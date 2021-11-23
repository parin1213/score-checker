using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace genshin.relic.score.Models.Lambda
{
    public class LambdaResponse
    {
        [JsonProperty(PropertyName = "isBase64Encoded")]
        public bool isBase64Encoded;

        [JsonProperty(PropertyName = "statusCode")]
        public HttpStatusCode statusCode;

        [JsonProperty(PropertyName = "headers")]
        public Dictionary<string, string> headers;

        [JsonProperty(PropertyName = "body")]
        public string body;
    }
}
