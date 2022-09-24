using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace score.checker.bot.Models.DB
{
    [DynamoDBTable("TweetAuditLog")]
    class TweetAuditLog
    {
        [DynamoDBHashKey]
        public DateTime executeTime;

        [DynamoDBRangeKey]
        public bool success;
    }
}
