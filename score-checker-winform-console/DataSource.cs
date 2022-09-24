using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    }
}
