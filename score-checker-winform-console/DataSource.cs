using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using genshin.relic.score.Models.ResponseData;

namespace genshin_relic
{
    public class DataSource
    {
        [DisplayName("col")]
        public int index { get; set; }

        [DisplayName("ファイル名")]
        public string FileName { get; set; }

        [DisplayName("聖遺物セット")]
        public string set { get; set; }

        [DisplayName("部位")]
        public string category { get; set; }

        [DisplayName("メインステータス")]
        public string main_status { get; set; }

        [DisplayName("サブステータス①")]
        public string sub_status1 { get; set; }

        [DisplayName("サブステータス②")]
        public string sub_status2 { get; set; }

        [DisplayName("サブステータス③")]
        public string sub_status3 { get; set; }

        [DisplayName("サブステータス④")]
        public string sub_status4 { get; set; }

        [DisplayName("スコア")]
        public string score { get; set; }

        [DisplayName("装備キャラクター")]
        public string character { get; set; }

        [DisplayName("最終更新日付")]
        public DateTime LastUpdate { get; set; }

        [DisplayName("処理時間")]
        public TimeSpan ProcessTime { get; set; }

        [Browsable(false)]
        public ResponseRelicData relic { get; set; }

        public static DataSource Create(string filePath, ResponseRelicData relic)
        {
            var sub_status1 = relic?.sub_status?.ElementAtOrDefault(0);
            var sub_status2 = relic?.sub_status?.ElementAtOrDefault(1);
            var sub_status3 = relic?.sub_status?.ElementAtOrDefault(2);
            var sub_status4 = relic?.sub_status?.ElementAtOrDefault(3);

            var dataSource = new DataSource
            {
                FileName = Path.GetFileName(filePath),
                set = relic?.set ?? "",
                category = relic?.category ?? "",
                main_status = relic?.main_status?.ToString() ?? "",
                sub_status1 = sub_status1?.ToString() ?? "",
                sub_status2 = sub_status2?.ToString() ?? "",
                sub_status3 = sub_status3?.ToString() ?? "",
                sub_status4 = sub_status4?.ToString() ?? "",
                score = relic?.score ?? "",
                character = relic?.character ?? "",
                LastUpdate = new FileInfo(filePath).LastWriteTime,
                relic = relic,
            };
            return dataSource;
        }

    }
}
