using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using genshin.relic.score.Models.Lambda;
using genshin.relic.score.Models.ResponseData;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using score.checker.bot.Models.DB;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
using Tweetinvi.Parameters;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace score_checker_bot
{
    public class Function
    {
        private static AmazonDynamoDBClient Client = new AmazonDynamoDBClient(RegionEndpoint.APNortheast1);

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<LambdaResponse> FunctionHandler(Request req, ILambdaContext context)
        {
            const int nextStepMinuts = 10;
            var success = true;

            var executeLog = await getDynamoDBObjects<TweetAuditLog>();
            executeLog = executeLog.OrderBy(log => log.executeTime);
            int failedCounter = 0;
            foreach (var e in executeLog.Reverse())
            {
                if (e.success) break;
                failedCounter++;
            }
            // 2^(失敗回数) * 10分待機するように計算する
            var nextInterval = TimeSpan.FromMinutes(Math.Pow(2, failedCounter) * nextStepMinuts);
            var nextTime = executeLog.Select(log => log.executeTime).DefaultIfEmpty().Max() + nextInterval;
            if (failedCounter != 0 && DateTime.Now < nextTime) return createDefaultResponse();

            try
            {
                TwitterClient userClient = createTwitterClient();

                var user = await userClient.Users.GetUserAsync("score_checker");

                var directMessages = await userClient.Messages.GetMessagesAsync();
                var directMentions = directMessages.Where(m => m.SenderId == user.Id);
                directMessages = directMessages
                                            .Where(m => m.SenderId != user.Id)
                                            .Where(m => 
                                                        directMentions
                                                                .Where(m2 => m2.RecipientId == m.SenderId)
                                                                .All(m2 => m2.CreatedAt < m.CreatedAt))
                                            .ToArray();

                var tray = directMessages.GroupBy(m => m.SenderId);
                foreach (var messages in tray)
                {
                    foreach (var message in messages)
                    {
                        var mediaList = await getMedia(userClient, message.Entities.Medias);
                        var relicList = await getRelic(mediaList);
                        var textInTweets = createTweetMessage(relicList);
                        foreach (var text in textInTweets)
                        {
                            var publishedMessage = await userClient.Messages.PublishMessageAsync(text, message.SenderId);
                        }
                    }
                }


                var allTweets = await user.GetUserTimelineAsync();
                var replied = allTweets.Where(t => t.InReplyToStatusId.HasValue).Select(t => t.InReplyToStatusId ?? 0).ToList();

                var tweets = await userClient.Timelines.GetMentionsTimelineAsync();
                var _failedList = await getDynamoDBObjects<ReplyFailedTweet>();
                var failedIDList = _failedList.Select(t => t.IdStr).ToList();


                tweets = tweets
                            .Where(tweet => tweet.InReplyToUserId == user.Id)
                            .Where(tweet => tweet.Entities.UserMentions.Where(t => t.ScreenName == user.ScreenName).Any())
                            .Where(tweet => !replied.Contains(tweet.Id))
                            .Where(tweet => !failedIDList.Contains(tweet.IdStr))
                            .Where(tweet => tweet.Media.Any(m => m.MediaType == "photo"))
                            .Where((tweet)=> 
                            {
                                var lastReply = allTweets
                                                    .Where(t => t.InReplyToStatusId.HasValue)
                                                    .OrderByDescending(f => f.CreatedAt)
                                                    .First();
                                return tweet.CreatedAt > lastReply.CreatedAt;
                            })
                            .ToArray();
                foreach (var tweet in tweets)
                {
                    try
                    {
                        Debug.WriteLine(tweet.Text);
                        var mediaList = await getMedia(userClient, tweet.Media);
                        var relicList = await getRelic(mediaList);
                        var textInTweets = createTweetMessage(relicList);

                        foreach (var text in textInTweets)
                        {
                            var reply = await userClient.Tweets.PublishTweetAsync(
                            new PublishTweetParameters("@" + tweet.CreatedBy.ScreenName + $" {text}")
                            {
                                InReplyToTweet = tweet,
                            });
                        }

                    }
                    catch (Exception ex)
                    {
                        success = false;
                        var failed = new ReplyFailedTweet()
                        {
                            IdStr = tweet.IdStr,
                            ScreenName = tweet.CreatedBy.ScreenName,

                        };
                        await setDynamoDBObject(failed);
                        context.Logger.Log(ex.ToString());

                        return createDefaultResponse();
                    }
                }
            }
            finally
            {
                if (success == false)
                {
                    var log = new TweetAuditLog()
                    {
                        executeTime = DateTime.Now,
                        success = success,
                    };
                    await setDynamoDBObject(log);
                }
            }


            return createDefaultResponse();
        }

        private IEnumerable<string> createTweetMessage(IEnumerable<ResponseRelicData> relicList)
        {
            List<string> tweets = new List<string>();
            var orgRelicList = relicList.Where(r => r.RelicMD5 != null);
            if (2 <= orgRelicList.Count() && orgRelicList.All(r => r.extendRelic != null))
            {
                var _text = relicDiffScore(orgRelicList);
                tweets.Add(_text);
                foreach (var r in orgRelicList)
                {
                    _text = relicSumScore(r.extendRelic.Prepend(r));
                    tweets.Add(_text);
                }
            }
            else
            {
                var _text = relicSumScore(relicList);
                tweets.Add(_text);
            }

            tweets = tweets.Select(text =>
            {
                if (140 < text.Length)
                {
                    text = text.Substring(0, 140);
                }

                return text;
            }).ToList();

            return tweets;
        }

        private string relicDiffScore(IEnumerable<ResponseRelicData> relicList)
        {
            string text = "";
            foreach (var r in relicList)
            {
                var flat = r.extendRelic?.Append(r) ?? default;
                var count = flat?.Count() ?? 0;
                if (1 < count)
                {
                    var sum = flat.Sum(r =>
                    {
                        double.TryParse(r.score, out var _score);
                        return _score;
                    });
                    var crit = flat.SelectMany(_r => _r.sub_status).Where(s => s.Key == "会心率%").Sum(s => s.Value);
                    var crit_dmg = flat.SelectMany(_r => _r.sub_status).Where(s => s.Key == "会心ダメージ%").Sum(s => s.Value);
                    var atk = flat.SelectMany(_r => _r.sub_status).Where(s => s.Key == "攻撃力%" || s.Key == "攻擊力%").Sum(s => s.Value);
                    text += $"スコア: {sum:0.0#}(CRT：{crit:0.0#}%, CRT DMG：{crit_dmg:0.0#}%, ATK：{atk:0.0#})" + Environment.NewLine; ;
                }
            }
            return text;
        }

        private static string relicSumScore(IEnumerable<ResponseRelicData> relicList)
        {
            string text = "";
            foreach (var r in relicList)
            {
                text += $"スコア:{r.score}({r.set}/{r.category})" + Environment.NewLine;
            }

            var count = relicList.Count();
            if (1 < count)
            {
                var sum = relicList.Sum(r =>
                {
                    double.TryParse(r.score, out var _score);
                    return _score;
                });
                text += $"合計スコア: {sum:0.0#}";
            }

            return text;
        }

        private async Task<IEnumerable<(string src, string mediaType)>> getMedia(TwitterClient userClient, IEnumerable<IMediaEntity> mediaEntity)
        {
            List<(string src, string mediaType)> list = new List<(string src, string mediaType)>();
            foreach (var media in mediaEntity)
            {
                var result = await userClient.Execute.RequestAsync(request =>
                {
                    request.Url = media.MediaURL;
                    request.HttpMethod = Tweetinvi.Models.HttpMethod.GET;
                });

                var imageBinary = result.Response.Binary;
                var mediaType = GetMediaType(media.MediaURL);

                var base64 = Convert.ToBase64String(imageBinary);
                var src = "data:" + mediaType + ";base64," + base64;
                list.Add(( src, mediaType ));
            }

            return list;
        }
        public string GetMediaType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings.Add(".dnct", "application/dotnetcoretutorials");
            string contentType;
            if (!provider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }

        private async Task<IEnumerable<ResponseRelicData>> getRelic(IEnumerable<(string src, string mediaType)> relicList)
        {
            List<ResponseRelicData> list = new List<ResponseRelicData>();
            foreach (var src in relicList)
            {
                var relic = await calclateScore(src.src, src.mediaType);

                list.Add(relic);
                if (relic.extendRelic != null)
                {
                    foreach (var r in relic.extendRelic)
                    {
                        list.Add(r);
                    }
                }
            }

            return list;
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

        private TwitterClient createTwitterClient()
        {
            var jsonString = File.ReadAllText("./score_checker_scret.json");
            var screts = JObject.Parse(jsonString);

            string CONSUMER_KEY = screts.Value<string>("CONSUMER_KEY");
            string CONSUMER_SECRET = screts.Value<string>("CONSUMER_SECRET");
            string ACCESS_TOKEN = screts.Value<string>("ACCESS_TOKEN");
            string ACCESS_TOKEN_SECRET = screts.Value<string>("ACCESS_TOKEN_SECRET");
            string BEARER_TOKEN = screts.Value<string>("BEARER_TOKEN");

            //var appCredentials = new ConsumerOnlyCredentials(CONSUMER_KEY, CONSUMER_SECRET)
            //{
            //    BearerToken = BEARER_TOKEN // bearer token is optional in some cases
            //};

            //var userClient = new TwitterClient(appCredentials);


            var userCredentials = new TwitterCredentials(CONSUMER_KEY, CONSUMER_SECRET, ACCESS_TOKEN, ACCESS_TOKEN_SECRET);
            var userClient = new TwitterClient(userCredentials);


            return userClient;
        }

        async Task<ResponseRelicData> calclateScore(string imageData, string contentType)
        {
            const string url = "https://api.genshin.parin1213.com/genshin-relic-score?dev_mode=1&cached=true";
            if (string.IsNullOrEmpty(contentType)) { contentType = "text/plain"; }

            var content = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = new System.Net.Http.HttpMethod("POST"),
                Content = new StringContent(imageData, Encoding.UTF8, contentType),
            };
            var Http = HttpClientFactory.Create();
            var bodyTask = Http.SendAsync(content);
            var body = await bodyTask;

            string rawResponse = await body.Content.ReadAsStringAsync();
            ResponseRelicData _relic = null;
            _relic = JsonConvert.DeserializeObject<ResponseRelicData>(rawResponse, new JsonSerializerSettings()
            {
            });


            if (body.IsSuccessStatusCode == false)
            {
                string NewLine = Environment.NewLine;
                var messages = $"{NewLine}Server StackTrace:{NewLine}" +
                                $"{_relic?.StackTrace}{NewLine}" +
                                $"Error Message:{_relic?.ExceptionMessages}";

                throw new HttpRequestException(messages);
            }

            return _relic;
        }

        private async Task<IEnumerable<T>> getDynamoDBObjects<T>()
        {
            using var dbContext = new DynamoDBContext(Client);
            var dbObjects = await dbContext.ScanAsync<T>(null).GetRemainingAsync();
            return dbObjects;
        }

        private async Task setDynamoDBObject<T>(T dbObjects)
        {
            using var dbContext = new DynamoDBContext(Client);
            await dbContext.SaveAsync(dbObjects);
        }

        private async Task removeDynamoDBObject<T>(string hashKey, string rangeKey)
        {
            using var dbContext = new DynamoDBContext(Client);
            await dbContext.DeleteAsync<T>(hashKey: hashKey, rangeKey: rangeKey);
        }


    }
}
