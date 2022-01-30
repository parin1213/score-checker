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
using genshin.relic.score.Models.Lambda;
using genshin.relic.score.Models.ResponseData;
using genshin.relic.score.Models.Recognize;
using System.Drawing;
using genshin.relic.score.Extentions;

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
        public async Task<LambdaResponse> FunctionHandler(Request req, ILambdaContext context)
        {
            //------------------------------
            // 応答電文の設定
            //------------------------------
            LambdaResponse response = createDefaultResponse();
            var body = new ResponseRelicData();
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
                var cacheExists = cached;
                if (cached)
                {
                    cacheExists &= await tryGetCacheData(hash, body);
                }

                //------------------------------
                // 画像認識
                //------------------------------
                var relic = new Relic(imageBinary);
                var word_list = body.word_list as IEnumerable<RelicWord>;
                if (word_list == null || cacheExists == false)
                {
#if WINFORMS
                    if (cached)
                    {
                        word_list = Enumerable.Empty<RelicWord>();
                        body.word_list = word_list.ToList();
                    }
                    else
                    {
                        word_list = await relic.detectWords();
                        body.word_list = word_list.ToList();

                    }
#else
                    word_list = await relic.detectWords();
                    body.word_list = word_list.ToList();
#endif
                }

                //------------------------------
                // S3へ保存(画像)
                //------------------------------
                await saveFIleForS3($"image/{hash}.png", imageBinary);

                //------------------------------
                // サブステータス抽出
                //------------------------------
                var relicSubStatusList = relic.getRelicSubStatus(word_list);

                var multipleRelicSubStatusList = relic.chunkRelicSubStatus(relicSubStatusList);
                relicSubStatusList = multipleRelicSubStatusList.FirstOrDefault()?.ToList();
                relicSubStatusList = relicSubStatusList ?? Enumerable.Empty<Status>().ToList();

                if (relicSubStatusList.Any() == false)
                {
#if WINFORMS
#else
                    word_list = relic.detectWords_fallback();
                    body.word_list = word_list.ToList();
                    relicSubStatusList = relic.getRelicSubStatus(word_list);
#endif
                }

                var hasMultipleRelic = multipleRelicSubStatusList.Skip(1).Any();
                setRelic(body, relic, word_list, relicSubStatusList, hasMultipleRelic);

                //------------------------------
                // ハッシュ値
                //------------------------------
                body.RelicMD5 = hash;

                // 2個以上聖遺物が検知された場合
                if (multipleRelicSubStatusList.Skip(1).Any())
                {
                    var extendRelic = new List<ResponseRelicData>();
                    foreach (var extendRelicSubStatusList in multipleRelicSubStatusList.Skip(1))
                    {
                        var extendBody = new ResponseRelicData();
                        extendBody.word_list = word_list.ToList();

                        setRelic(
                            extendBody, 
                            relic, 
                            word_list, 
                            extendRelicSubStatusList.ToList(), 
                            hasMultipleRelic);
                        extendRelic.Add(extendBody);
                    }

                    body.extendRelic = extendRelic;
                }

                //------------------------------
                // S3へ保存(応答データ)
                //------------------------------
                await saveFIleForS3($"image/{hash}_status.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));

            }
            catch (Exception ex)
            {
                response.statusCode = HttpStatusCode.InternalServerError;

                var dev_mode = req?.QueryStringParameters?["dev_mode"]?.ToString();
                if (dev_mode == "1")
                {
                    body.StackTrace = ex.ToString();
                    body.req = JsonConvert.SerializeObject(req);
                }



                body.ExceptionMessages = "Internal Server Error";
            }
            finally
            {
                var assembly = Assembly.GetExecutingAssembly().GetName();
                var ver = assembly.Version;

                // アセンブリのバージョン
                body.version = $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";

                response.statusCode = HttpStatusCode.OK;
                response.body = JsonConvert.SerializeObject(body);
            }
            return response;
        }

        private static void setRelic(
            ResponseRelicData body, 
            Relic relic, 
            IEnumerable<RelicWord> word_list, 
            List<Status> relicSubStatusList,
            bool hasMultipleRelic)
        {
            //------------------------------
            // スコア計算
            //------------------------------
            double score = relic.calculateScore(relicSubStatusList);

            //------------------------------
            // 応答設定
            //------------------------------
            body.score = $"{score:0.0#}";
            body.sub_status = relicSubStatusList;

            //------------------------------
            // メインステータス
            //------------------------------
            var mainStatus = relic.getMainStatus(word_list, relicSubStatusList);
            body.main_status = selectMainStatus(mainStatus, relicSubStatusList);

            //------------------------------
            // 部位
            //------------------------------
            var category = relic.getCategory(word_list, relicSubStatusList);
            body.category = category;

            //------------------------------
            // 聖遺物セット
            //------------------------------
            var set = relic.getSetName(word_list, relicSubStatusList);
            body.set = set;

            //------------------------------
            // 聖遺物セット
            //------------------------------
            var character = relic.getCharacterName(word_list, relicSubStatusList);
            body.character = character;

            //------------------------------
            // 切抜箇所
            //------------------------------
            var cropHint = relic.getCropHint(body, hasMultipleRelic);
            body.cropHint = cropHint;
        }

        private static Status selectMainStatus(List<Status> mainStatus, List<Status> relicSubStatusList)
        {
            if (mainStatus.Any() == false || relicSubStatusList.Any() == false) { return null; }

            var subRelicRect = relicSubStatusList.Select(s => s.rect)
                                                 .Aggregate((r1, r2) => Rectangle.Union(r1, r2));

            var min_x = subRelicRect.Location.X - subRelicRect.Width;
            var max_x = subRelicRect.Location.X + subRelicRect.Width;

            var mains = mainStatus.Where(m => m.rect.Top < subRelicRect.Top)
                                  .Where(m => min_x < m.rect.Location.X && m.rect.Location.X < max_x)
                                  .Where(m => m.rect.Right < max_x * 2)
                                  .OrderBy(m => m.rect.Location.Distance(subRelicRect.Location));

            var main = mains.FirstOrDefault() ?? mainStatus.FirstOrDefault();

            return main;
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

        private async Task saveFIleForS3(string filePath, byte[] bin)
        {
#if WINFORMS
            await Task.CompletedTask;
#elif DEBUG
            var path = @"C:\dev\aws\s3\relic-server-log_next\";
            path = Path.Combine(path, filePath);
            await File.WriteAllBytesAsync(path, bin);
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
                await client.PutObjectAsync(request);
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

        private async Task<bool> tryGetCacheData(string hash, ResponseRelicData dic)
        {
#if DEBUG || WINFORMS
            var path = @"C:\dev\aws\s3\relic-server-log_next\";
            path = Path.Combine(path, $"image/{hash}_status.json");
            ResponseRelicData relic = null;
            try
            {
                var relicString = await File.ReadAllTextAsync(path);
                relic = JsonConvert.DeserializeObject<ResponseRelicData>(relicString);
            }
            catch { }
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
            var response = await client.ListObjectsV2Async(request, CancellationToken.None);
            if (response.S3Objects.Count() != 3) { return false; }

            //------------------------------
            // キャッシュ取得
            //------------------------------
            var status = await client.GetObjectAsync("relic-server-log", $"image/{hash}_status.json");
            var statusStream = new StreamReader(status.ResponseStream);
            ResponseRelicData relic = null;
            try
            {
                var relicString = await statusStream.ReadToEndAsync();
                relic = JsonConvert.DeserializeObject<ResponseRelicData>(relicString);
            }
            catch { }
#endif
            //------------------------------
            // キャッシュ値格納
            //------------------------------
            dic.word_list    = relic?.word_list;
            dic.score        = relic?.score;
            dic.sub_status   = relic?.sub_status;
            dic.main_status  = relic?.main_status;
            dic.category     = relic?.category;
            dic.set          = relic?.set;

            return true;
        }

    }
}
