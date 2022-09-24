using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace score.checker.bot.Models.DB
{
    [DynamoDBTable("ReplyFailedTweet")]
    class ReplyFailedTweet
    {
        [DynamoDBHashKey]
        public string IdStr { get; set; }

        [DynamoDBProperty]
        public string ScreenName { get; set; }
    }
}
