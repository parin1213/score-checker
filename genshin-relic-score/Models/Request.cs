using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace genshin.relic.score.Models
{
    public class Request
    {
        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("httpMethod")]
        public string HttpMethod { get; set; }

        [JsonProperty("headers")]
        public JObject Headers { get; set; }

        [JsonProperty("multiValueHeaders")]
        public JObject MultiValueHeaders { get; set; }

        [JsonProperty("queryStringParameters")]
        public JObject QueryStringParameters { get; set; }

        [JsonProperty("multiValueQueryStringParameters")]
        public JObject MultiValueQueryStringParameters { get; set; }

        [JsonProperty("pathParameters")]
        public JObject PathParameters { get; set; }

        [JsonProperty("stageVariables")]
        public JObject StageVariables { get; set; }

        [JsonProperty("requestContext")]
        public JObject RequestContext { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("isBase64Encoded")]
        public Boolean IsBase64Encoded { get; set; }
    }
}
