using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Google.Cloud.Vision.V1;
using Newtonsoft.Json;

namespace genshin.relic.score.Models
{
    public class Relic
    {
        const string scretPath = @"./gcp.json";
        public byte[] imageBinary;
        private RelicKind[] relicKind;

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

            relicKind = JsonConvert.DeserializeObject<RelicKind[]>(File.ReadAllText("./relic.json", Encoding.UTF8));
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
            var numeric_pattern = "(?<value>([1-9][\\d*|0|,]*)(\\.\\d+)?(%)?)";
            var pattern = $"(?<class>{status_class_pattern})(.*?)?" + "(\\+)?" + numeric_pattern;

            //------------------------------
            // ステータス抽出
            //------------------------------
            var statusList = getStatus(lines, pattern, numeric_pattern);

            //------------------------------
            // メイン効果を削除
            //------------------------------
            if (statusList.Count() > 4)
            {
                statusList = statusList.TakeLast(4);
            }

            //------------------------------
            // オプション毎にステータスを記録
            //------------------------------
            var list = statusList.Distinct()
               .Select(text => text.Split("+"))
               .Select(
                    t => new KeyValuePair<string, double>(
                        t.ElementAtOrDefault(0) + (t.ElementAtOrDefault(1).EndsWith("%") ? "%" : ""),
                        double.Parse(t.ElementAtOrDefault(1).Replace("%", ""))))
               .ToList();

            var dic = filterOptions(list);

            return dic;
        }

        private IEnumerable<string> getStatus(List<string> lines, string pattern, string numeric_pattern, bool needsMainStatus = false)
        {
            //------------------------------
            // パターンに一致する文字列を抽出
            //------------------------------
            var regex = new Regex(pattern);
            var statusList = lines.Where(text => regex.IsMatch(text));


            //------------------------------
            // パターンに一致しない場合
            // （聖遺物強化画面のキャプチャの場合）
            //------------------------------
            bool isDetailPage = statusList.Any() == false;
            if (isDetailPage || needsMainStatus)
            {
                // 聖遺物強化画面の場合はOCR結果が「攻撃力\n5.7%」のような形で取得されるので
                // 数値っぽい値の場合前の行と合成してパターンを作る
                statusList = lines.Select((t, i) =>
                {
                    if (i < 1) { return t; }

                    if (Regex.IsMatch(t, numeric_pattern))
                    {
                        return lines.ElementAt(i - 1) + "+" + t;
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

            return statusList;
        }

        private Dictionary<string, double> filterOptions(List<KeyValuePair<string, double>> _subStatusOption)
        {
            var scoreRates = new Dictionary<string, (double minValue, double maxValue)>()
            {
                // key              maxValue,  minValue
                {"攻撃力",           ( 7.0d,  19d  * 6)},
                {"攻擊力",           ( 7.0d,  19d  * 6)},
                {"攻撃力%",          ( 2.5d,  5.8d * 6)},
                {"攻擊力%",          ( 2.5d,  5.8d * 6)},
                {"防御力",           ( 8.0d,  23d  * 6)},
                {"防御力%",          ( 3.1d,  7.3d * 6)},
                {"HP",               ( 110d,  299  * 6)},
                {"HP%",              ( 2.5d,  5.8d * 6)},
                {"元素熟知",         (  10d,  23   * 6)},
                {"元素チャージ効率%",( 2.7d,  6.5  * 6)},
                {"会心率%",          ( 1.6d,  3.9  * 6)},
                {"会心ダメージ%",    ( 3.3d,  7.8  * 6)},
            };

            var ignoreSetEffects = new List<(string, double)>()
            {
                ("会心率%",              12d), // 狂戦士2セット
                ("攻撃力%",              18d), // 旅人の心2セット,剣闘士2セット,しめ縄2セット
                ("攻擊力%",              18d), // 同上
                ("元素熟知",             18d), // 教官2セット,楽団2セット
                ("元素チャージ効率%",    20d), // 学者2セット,亡命者2セット,絶縁2セット
                ("防御力",              100d), // 幸運2セット
                ("防御力%",              30d), // 守護2セット
                ("HP%",                  20d), // 仙岩2セット
            };

            //------------------------------
            // セット効果のようなステータスを削除
            //------------------------------
            var sameOptions = _subStatusOption.GroupBy(pair => pair.Key).Where(g => g.Count() != 1);
            foreach(var g in sameOptions)
            {
                var _removeList = new List<KeyValuePair<string, double>>();

                var effects = g.Where(p => ignoreSetEffects.Contains((p.Key, p.Value)));
                _removeList.AddRange(effects);


                // 削除
                foreach(var removeItem in _removeList)
                {
                    _subStatusOption.Remove(removeItem);
                }
            }

            // 中身をコピー
            var subStatusOption = new Dictionary<string, double>(_subStatusOption);

            //------------------------------
            // あり得ない値（メイン効果等）の削除
            //------------------------------
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

        public Dictionary<string, double> getMainStatus(List<string> lines)
        {
            //------------------------------
            // 正規表現パターン生成
            //------------------------------
            var status_classes = new[] { "攻撃力", "攻擊力", "防御力", "HP", "元素熟知", "元素チャージ効率", "会心率", "会心ダメージ", "与える治療効果", "物理ダメージ", "(炎|水|氷|雷|風|岩)元素ダメージ" };
            var status_class_pattern = string.Join("|", status_classes);
            var numeric_pattern = "(?<value>([1-9][\\d*|0|,]*)(\\.\\d+)?(%)?)";
            var pattern = $"(?<class>{status_class_pattern})(.*?)?" + "(\\+)?" + numeric_pattern;

            var statusList = getStatus(lines, pattern, numeric_pattern, needsMainStatus: true);

            var subStatusDic = getRelicSubStatus(lines).ToList();

            //------------------------------
            // オプション毎にステータスを記録
            //------------------------------
            var list = statusList.Distinct()
               .Select(text => text.Split("+"))
               .Select(
                    t => new KeyValuePair<string, double>(
                        t.ElementAtOrDefault(0) + (t.ElementAtOrDefault(1).EndsWith("%") ? "%" : ""),
                        double.Parse(t.ElementAtOrDefault(1).Replace("%", ""))))
               .ToList();

            var mainStatus = list.Except(subStatusDic);

            return new Dictionary<string, double>(mainStatus);
        }

        public string getCategory(List<string> lines)
        {
            var categories = relicKind.Select(r => r.category).Distinct();
            var category = categories.Where(c => lines.Where(l => l.Contains(c)).Any()).FirstOrDefault();

            if(category == null)
            {
                category = relicKind.Where(r => lines.Where(l => l.Contains(r.item)).Any())
                                    .Select(r => r.category)
                                    .FirstOrDefault();
            }

            return category ?? "";
        }

        public string getSetName(List<string> lines)
        {

            var setName = relicKind.Where(r => lines.Where(l => l.Contains(r.item)).Any())
                                .Select(r => r.set)
                                .FirstOrDefault();

            if(setName == null)
            {
                setName = relicKind.Where(r => lines.Where(l => l.Contains(r.set)).Any())
                                   .Select(r => r.set)
                                   .FirstOrDefault();
            }

            return setName ?? "";
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
