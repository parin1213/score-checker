using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using genshin.relic.score.Models;
using Google.Cloud.Vision.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using System.Security.Cryptography;
using System.Threading;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace genshin_relic_score
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public LambdaResponse FunctionHandler(Request req, ILambdaContext context)
        {
            //------------------------------
            // 応答電文の設定
            //------------------------------
            LambdaResponse response = createDefaultResponse();
            var body = new Dictionary<string, object>();


            try
            {
                //------------------------------
                // 送信された画像を復号化
                //------------------------------
                var imageBinary = decryptBase64String(req.Body);
                //var imageBinary = File.ReadAllBytes(@"C:\Users\schwa\Videos\Captures\relic\DDD844CF49F78ADBAEB064005E5BED52.png");

                //------------------------------
                // 応答固有のID(画像のハッシュ値)の生成
                //------------------------------
                using var hashProvider = MD5.Create();
                var hash = string.Join("", hashProvider.ComputeHash(imageBinary).Select(b => b.ToString("X2")));

                //------------------------------
                // キャッシュ取得
                //------------------------------
                var cacheExists = tryGetCacheData(hash, body);
                if (cacheExists) { return response; }

                //------------------------------
                // S3へ保存(画像)
                //------------------------------
                saveFIleForS3($"image/{hash}.png", imageBinary);

                //------------------------------
                // 画像認識
                //------------------------------
                var relic = new Relic(imageBinary);
                var word_list = relic.detectWords();

                //------------------------------
                // サブステータス抽出
                //------------------------------
                var relicSubStatusList = relic.getRelicSubStatus(word_list);

                //------------------------------
                // S3へ保存(サブステータス)
                //------------------------------
                saveFIleForS3($"image/{hash}_status.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(relicSubStatusList)));

                //------------------------------
                // スコア計算
                //------------------------------
                double score = relic.calculateScore(relicSubStatusList);

                //------------------------------
                // S3へ保存(スコア)
                //------------------------------
                saveFIleForS3($"image/{hash}_score.json",
                    Encoding.UTF8.GetBytes(
                        JsonConvert.SerializeObject(
                            new Dictionary<string, string>()
                            {
                            { "score", $"{score:0.0#}"}
                            })
                        ));

                //------------------------------
                // 応答設定
                //------------------------------
                body.Add("score", $"{score:0.0#}");
                body.Add("sub_status", relicSubStatusList);
            }
            catch (Exception ex)
            {
                var dev_mode = req.QueryStringParameters["dev_mode"]?.ToString();
                if (dev_mode == "1")
                {
                    body["body"] = req.Body;
                    body["StackTrace"] = ex.ToString();
                }
            }
            finally
            {
                response.body = JsonConvert.SerializeObject(body);
            }
            return response;
        }

        public byte[] decryptBase64String(string base64String)
        {
            var data = Convert.FromBase64String(base64String);
            var dataString = Encoding.UTF8.GetString(data);
            if (dataString.StartsWith("data:"))
            {
                // ブラウザから送信されるデータは"data:image/xxx, (base64文字列) "形式なので変換する
                base64String = dataString.Split(",").ElementAtOrDefault(1)?.Replace("-", "+")?.Replace("_", "/");
                data = Convert.FromBase64String(base64String);
            }

            return data;
        }

        private void saveFIleForS3(string filePath, byte[] bin)
        {
            var client = new AmazonS3Client(RegionEndpoint.APNortheast1);

            PutObjectRequest request = new PutObjectRequest()
            {

                BucketName = "relic-server-log",
                Key = filePath,

                StorageClass = S3StorageClass.StandardInfrequentAccess,
                CannedACL = S3CannedACL.Private,
                InputStream = new MemoryStream(bin),
            };
            request?.InputStream?.Seek(0, SeekOrigin.Begin);

            try
            {
                client.PutObjectAsync(request).Wait();
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private LambdaResponse createDefaultResponse()
        {
            var response = new LambdaResponse()
            {
                isBase64Encoded = false,
                statusCode = HttpStatusCode.OK,
                headers = new Dictionary<string, string>()
                {
                    { "Access-Control-Allow-Origin",  "*" },
                    { "Access-Control-Allow-Headers", "*" },
                    { "Access-Control-Allow-Methods", "*" },
                },
            };

            return response;
        }

        private bool tryGetCacheData(string hash, Dictionary<string, object> dic)
        {
            var client = new AmazonS3Client(RegionEndpoint.APNortheast1);

            //------------------------------
            // キャッシュ存在チェック
            //------------------------------
            var request = new ListObjectsV2Request
            {
                BucketName = "relic-server-log",
                Prefix = $"image/{hash}",
                MaxKeys = 3
            };
            var response = client.ListObjectsV2Async(request, CancellationToken.None).Result;
            if (response.S3Objects.Count() != 3) { return false; }

            //------------------------------
            // キャッシュ取得
            //------------------------------
            var status = client.GetObjectAsync("relic-server-log", $"image/{hash}_status.json").Result;
            var statusStream = new StreamReader(status.ResponseStream);
            var statusDic = JsonConvert.DeserializeObject<Dictionary<string, double>>(statusStream.ReadToEnd());

            var score = client.GetObjectAsync("relic-server-log", $"image/{hash}_score.json").Result;
            var scoreStream = new StreamReader(score.ResponseStream);
            var scoreDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(scoreStream.ReadToEnd());

            //------------------------------
            // キャッシュ値格納
            //------------------------------
            dic["sub_status"] = statusDic;

            foreach (var pair in scoreDic)
            {
                dic[pair.Key] = pair.Value;
            }

            return true;
        }

    }
}
