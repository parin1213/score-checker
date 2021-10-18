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
using System.Reflection;
using score_checker.Models;

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
            byte[] imageBinary = null;

            try
            {
                //------------------------------
                // 送信された画像を復号化
                //------------------------------
                try
                {
                    imageBinary = decryptBase64String(req.Body, req.IsBase64Encoded);
                    //imageBinary = File.ReadAllBytes(@"C:\Users\schwa\Videos\Captures\relic\DDD844CF49F78ADBAEB064005E5BED52.png");
                }
                catch
                {
                    response.statusCode = HttpStatusCode.BadRequest;
                    throw;
                }

                //------------------------------
                // 応答固有のID(画像のハッシュ値)の生成
                //------------------------------
                using var hashProvider = MD5.Create();
                var hash = string.Join("", hashProvider.ComputeHash(imageBinary).Select(b => b.ToString("X2")));

                //------------------------------
                // キャッシュ取得
                //------------------------------
                var strCached = req?.QueryStringParameters?["cached"]?.ToString() ?? "";
                bool.TryParse(strCached, out var cached);
                var cacheExists = cached && tryGetCacheData(hash, body);

                //------------------------------
                // 画像認識
                //------------------------------
                var relic = new Relic(imageBinary);
                body.TryGetValue("word_list", out var objWordList);
                var word_list = objWordList as List<string>;
                if (word_list == null || cacheExists == false)
                {
                    word_list = relic.detectWords();
                    body.Add("word_list", word_list);

                }

                //------------------------------
                // S3へ保存(画像)
                //------------------------------
                saveFIleForS3($"image/{hash}.png", imageBinary);

                //------------------------------
                // サブステータス抽出
                //------------------------------
                var relicSubStatusList = relic.getRelicSubStatus(word_list);

                //------------------------------
                // スコア計算
                //------------------------------
                double score = relic.calculateScore(relicSubStatusList);

                //------------------------------
                // 応答設定
                //------------------------------
                body["score"] = $"{score:0.0#}";
                body["sub_status"] = relicSubStatusList;

                //------------------------------
                // 応答設定
                //------------------------------
                var mainStatus = relic.getMainStatus(word_list);
                body["main_status"] = mainStatus.FirstOrDefault();

                //------------------------------
                // 応答設定
                //------------------------------
                var category = relic.getCategory(word_list);
                body["category"] = category;

                //------------------------------
                // 応答設定
                //------------------------------
                var set = relic.getSetName(word_list);
                body["set"] = set;

                //------------------------------
                // S3へ保存(応答データ)
                //------------------------------
                saveFIleForS3($"image/{hash}_status.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));

            }
            catch (Exception ex)
            {
                response.statusCode = HttpStatusCode.InternalServerError;

                var dev_mode = req?.QueryStringParameters?["dev_mode"]?.ToString();
                if (dev_mode == "1")
                {
                    body["body"] = req?.Body;
                    body["StackTrace"] = ex.ToString();
                }

                body["ExceptionMessages"] = "Internal Server Error";
            }
            finally
            {
                var assembly = Assembly.GetExecutingAssembly().GetName();
                var ver = assembly.Version;

                // アセンブリのバージョン
                body["version"] = $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";

                response.statusCode = HttpStatusCode.OK;
                response.body = JsonConvert.SerializeObject(body);
            }
            return response;
        }

        public byte[] decryptBase64String(string base64String, bool IsBase64Encoded)
        {
            var dataString = base64String;
            byte[] data = Encoding.ASCII.GetBytes(dataString);

            if (IsBase64Encoded)
            {
                data = Convert.FromBase64String(base64String);
                dataString = Encoding.UTF8.GetString(data);
            }
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
#if DEBUG
            var path = @"C:\dev\aws\s3\relic-server-log\";
            path = Path.Combine(path, filePath);
            File.WriteAllBytes(path, bin);
#else
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
#endif
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
#if DEBUG
            var path = @"C:\dev\aws\s3\relic-server-log\";
            path = Path.Combine(path, $"image/{hash}_status.json");
            var relic = JsonConvert.DeserializeObject<RelicScore>(File.ReadAllText(path));
#else
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
            var relic = JsonConvert.DeserializeObject<RelicScore>(statusStream.ReadToEnd());
#endif
            //------------------------------
            // キャッシュ値格納
            //------------------------------
            dic["word_list"]    = relic.word_list;
            dic["score"]        = relic.score;
            dic["sub_status"]   = relic.sub_status;
            dic["main_status"]  = relic.main_status;
            dic["category"]     = relic.category;
            dic["set"]          = relic.set;

            return true;
        }

    }
}
