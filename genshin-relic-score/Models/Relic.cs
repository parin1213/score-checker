using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Google.Cloud.Vision.V1;

namespace genshin.relic.score.Models
{
    public class Relic
    {
        const string scretPath = @"./gcp.json";
        public byte[] imageBinary;

        public Relic(byte[] imageBinary)
        {
            //------------------------------
            // 引数設定
            //------------------------------
            this.imageBinary = imageBinary;

            //------------------------------
            // GCPアカウントの設定
            //------------------------------
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", scretPath);
        }

        public List<string> detectWords()
        {
            //------------------------------
            // 画像のオブジェクトの生成
            //------------------------------
            var image = Image.FromBytes(imageBinary);

            //------------------------------
            // GCPへOCRリクエスト
            //------------------------------
            ImageAnnotatorClient client = ImageAnnotatorClient.Create();
            TextAnnotation text = client.DetectDocumentText(image);

            return text.Text.Split('\n').ToList();
        }

        public Dictionary<string, double> getRelicSubStatus(List<string> lines)
        {
            //------------------------------
            // 正規表現パターン生成
            //------------------------------
            var status_classes = new[] { "攻撃力", "攻擊力", "防御力", "HP", "元素熟知", "元素チャージ効率", "会心率", "会心ダメージ" };
            var status_class_pattern = string.Join("|", status_classes);
            var numeric_pattern = "(?<value>([1-9]\\d*|0)(\\.\\d+)?(%)?)";
            var pattern = $"(?<class>{status_class_pattern})(.*?)?" + "(\\+)?" + numeric_pattern;

            //------------------------------
            // パターンに一致する文字列を抽出
            //------------------------------
            var statusList = lines.Where(text => Regex.IsMatch(text, pattern));


            //------------------------------
            // パターンに一致しない場合
            // （聖遺物強化画面のキャプチャの場合）
            //------------------------------
            bool isDetailPage = statusList.Any() == false;
            if (isDetailPage)
            {
                // 聖遺物強化画面の場合はOCR結果が「攻撃力\n5.7%」のような形で取得されるので
                // 数値っぽい値の場合前の行と合成してパターンを作る
                statusList = lines.Select((t, i) =>
                {
                    if (i < 1) { return t; }

                    if (Regex.IsMatch(t, numeric_pattern))
                    {
                        return statusList.ElementAt(i - 1) + "+" + t;
                    }

                    return t;
                });
            }

            //------------------------------
            // 最終フィルタ実施
            // (セット効果等をフィルタ実施)
            //------------------------------
            statusList = statusList.Select(text =>
                                    {
                                        return (text: text, match: Regex.Match(text, pattern));
                                    })
                                    .Where(m =>
                                    {
                                        // 記号は無視する
                                        var size = m.text
                                                    .Where(c => char.IsSymbol(c) == false)
                                                    .Where(c => char.IsPunctuation(c) == false)
                                                    .Where(c => char.IsWhiteSpace(c) == false)
                                                    .Count();
                                        // セット効果の説明文などをフィルタするために、ステータスらしき箇所だけ抽出する
                                        return ((double)m.match.Value.Length / size) > 0.8;
                                    })
                                    .Select(m => $"{m.match.Groups["class"]}+{m.match.Groups["value"]}")
                                    .ToArray();

            //------------------------------
            // メイン効果を削除
            //------------------------------
            if (isDetailPage && statusList.Count() > 4)
            {
                statusList = statusList.TakeLast(4);
            }

            //------------------------------
            // オプション毎にステータスを記録
            //------------------------------
            var dic = statusList.Distinct()
               .Select(text => text.Split("+"))
               .ToDictionary(
                    t => t.ElementAtOrDefault(0) + (t.ElementAtOrDefault(1).EndsWith("%") ? "%" : ""),
                    t => double.Parse(t.ElementAtOrDefault(1).Replace("%", "")));

            dic = filterOptions(dic);

            return dic;
        }

        private Dictionary<string, double> filterOptions(Dictionary<string, double> subStatusOption)
        {
            var scoreRates = new Dictionary<string, (double minValue, double maxValue)>()
            {
                // key              maxValue,  minValue
                {"攻撃力",           ( 7.0d,  19d  * 6)},
                {"攻撃力%",          ( 2.5d,  5.8d * 6)},
                {"攻擊力%",          ( 2.5d,  5.8d * 6)},
                {"防御力",           ( 8.0d,  23d  * 6)},
                {"防御力%",          ( 3.1d,  7.3d * 6)},
                {"HP",               ( 110d,  299  * 6)},
                {"HP%",              ( 2.5d,  5.8d * 6)},
                {"元素熟知",         (  10d,  23   * 6)},
                {"元素チャージ効率", ( 2.7d,  6.5  * 6)},
                {"会心率%",          ( 1.6d,  3.9  * 6)},
                {"会心ダメージ%",    ( 3.3d,  7.8  * 6)},
            };

            // 中身をコピー
            subStatusOption = new Dictionary<string, double>(subStatusOption);

            foreach (var scoreRatePair in scoreRates)
            {
                var minValue = scoreRatePair.Value.minValue;
                var maxValue = scoreRatePair.Value.maxValue;

                // 無ければ次の検索へ
                if (subStatusOption.TryGetValue(scoreRatePair.Key, out var value) == false) { continue; }

                // 最低値以上、最大値未満でないものは削除する
                if (value < minValue || maxValue < value)
                {
                    subStatusOption.Remove(scoreRatePair.Key);
                }
            }

            return subStatusOption;
        }

        public double calculateScore(Dictionary<string, double> dic)
        {
            double score = 0;

            var scoreRates = new Dictionary<string, double>()
            {
                {"攻撃力%",         1.0d},
                {"攻擊力%",         1.0d},
                {"会心率%",         2.0d},
                {"会心ダメージ%",   1.0d},
            };

            foreach (var pair in scoreRates)
            {
                var key = pair.Key;
                var rate = pair.Value;

                if (dic.TryGetValue(key, out var strValue))
                {
                    score += strValue * rate;
                }
            }

            return score;
        }

    }
}
