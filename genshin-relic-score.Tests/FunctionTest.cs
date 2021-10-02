using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using genshin_relic_score;
using static genshin_relic_score.Function;
using Newtonsoft.Json.Linq;
using genshin.relic.score.Models;

namespace genshin_relic_score.Tests
{
    public class FunctionTest
    {
        [Fact]
        public void TestToUpperFunction()
        {

            // Invoke the lambda function and confirm the string was upper cased.
            var function = new Function();
            var context = new TestLambdaContext();
            var req = new Request { QueryStringParameters = JObject.Parse("{'ImageBase64': 'abc'}") };
            var upperCase = function.FunctionHandler(req, context);

        }
    }
}
