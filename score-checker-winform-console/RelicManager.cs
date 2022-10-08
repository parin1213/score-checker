using genshin.relic.score.Extentions;
using genshin.relic.score.Models.Lambda;
using genshin.relic.score.Models.ResponseData;
using genshin_relic_score;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace genshin_relic
{
    internal class RelicManager
    {
        private IAsyncEnumerable<DataSource> tasks;


        public string cached = false.ToString();
        public bool chkVerify = false;
        public string dir = "";
        private Task _task;

        public int maxCount { get; private set; }

        public RelicManager(string cached, string dir, bool chkVerify)
        {
            this.cached = cached;
            this.dir = dir;
            this.chkVerify = chkVerify;
        }

        public async IAsyncEnumerable<DataSource> updateList()
        {
            var dataSourceList = new BindingList<DataSource>();

            var files = Directory.EnumerateFiles(dir, "*.png", SearchOption.AllDirectories)
                                    .OrderByDescending(f => new FileInfo(f).LastWriteTime);
            maxCount = files.Count();

            foreach((var file, var index) in files.Select((f, i)=> (f, i)))
            {
                yield return await getRelic(index, file, chkVerify).ConfigureAwait(false);
            }
        }

        private async Task<DataSource> getRelic(int index, string filePath, bool force = false)
        {
            Debug.WriteLine($"getRelic Start:{index}");
            var start = DateTime.Now;
            ResponseRelicData relic;
            if (force)
            {
                relic = await relic_analyze(filePath).ConfigureAwait(false);
            }
            else
            {
                try
                {
                    relic = JsonConvert.DeserializeObject<ResponseRelicData>(File.ReadAllText(filePath.Replace(".png", "_status.json")));
                }
                catch (Exception ex)
                {
                    relic = await relic_analyze(filePath).ConfigureAwait(false);
                }
            }
            var end = DateTime.Now;

            var dataSource = DataSource.Create(filePath, relic);
            dataSource.index = index;
            dataSource.ProcessTime = (end - start);

            Debug.WriteLine($"getRelic End:{index}");
            return dataSource;
        }

        private async Task<ResponseRelicData> relic_analyze(string filePath)
        {
            var content = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
            var request = new Request()
            {
                IsBase64Encoded = true,
                Body = Convert.ToBase64String(content),
                QueryStringParameters = JObject.FromObject(new Dictionary<string, string>()
                {
                    { "dev_mode", "1"  },
                    { "cached",  cached},
                }),

            };
            var lambda = new Function();
            var res = await lambda.FunctionHandler(request, null).ConfigureAwait(false);
            var relic = JsonConvert.DeserializeObject<ResponseRelicData>(res.body);

            return relic;
        }
    }
}
