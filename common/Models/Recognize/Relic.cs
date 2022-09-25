using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using genshin.relic.score.Extentions;
using genshin.relic.score.Models.ResponseData;
using Google.Cloud.Vision.V1;
using Newtonsoft.Json;

namespace genshin.relic.score.Models.Recognize
{
    public class Relic
    {
        const string scretPath = @"./gcp.json";
        public byte[] imageBinary;
        public string gcpRecogText { get => text?.ToString() ?? "{}"; }
        private RelicKind[] relicKind;
        private Character[] characters;
        private TextAnnotation text = null;
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
            characters = JsonConvert.DeserializeObject<Character[]>(File.ReadAllText("./character.json", Encoding.UTF8));
        }

        public void parseFromJson(string jsonString)
        {
            text = TextAnnotation.Parser.ParseJson(jsonString);
        }

        public IEnumerable<RelicWord> detectWords_fallback()
        {
            if (text == null)
            {
                //------------------------------
                // 画像のオブジェクトの生成
                //------------------------------
                var image = Google.Cloud.Vision.V1.Image.FromBytes(imageBinary);
                
                //------------------------------
                // GCPへOCRリクエスト
                //------------------------------
                ImageAnnotatorClient client = ImageAnnotatorClient.Create();
                text = client.DetectDocumentText(image);
            }

            return text.Text.Split('\n').Select(t => new RelicWord() { text = t, rect = default });
        }

        public async Task<IEnumerable<RelicWord>> detectWords()
        {
            //------------------------------
            // 画像のオブジェクトの生成
            //------------------------------
            var image = Google.Cloud.Vision.V1.Image.FromBytes(imageBinary);

            //------------------------------
            // GCPへOCRリクエスト
            //------------------------------
            if(text == null)
            {
                ImageAnnotatorClient client = ImageAnnotatorClient.Create();
                text = await client.DetectDocumentTextAsync(image);
            }
            var words = WordAnalyzeV2(text);

            return words.ToList();
            //return text.Text.Split('\n').ToList();
        }

        private IEnumerable<RelicWord> WordAnalyze(TextAnnotation text)
        {
            //foreach (var page in text.Pages)
            //{
            //    foreach (var block in page.Blocks)
            //    {
            //        string box = string.Join(" - ", block.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
            //        System.Diagnostics.Debug.WriteLine($"Block {block.BlockType} at {box}");
            //        foreach (var paragraph in block.Paragraphs)
            //        {
            //            box = string.Join(" - ", paragraph.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
            //            System.Diagnostics.Debug.WriteLine($"  Paragraph at {box}");
            //            foreach (var word in paragraph.Words)
            //            {
            //                box = string.Join(" - ", word.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
            //                System.Diagnostics.Debug.WriteLine($"    box: {box}, Word: {string.Join("", word.Symbols.Select(s => s.Text))}");
            //            }
            //        }
            //    }
            //}
            //System.Diagnostics.Debug.WriteLine($"----------");
            foreach (var page in text.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var paragraph in block.Paragraphs)
                    {
                        var groups = paragraph.Words
                                    .GroupBy(word => word.BoundingBox.Vertices.Min(v => v.Y))
                                    .OrderBy(group => group.Key);

                        var chunk_y = getChunkWords_y(groups);

                        var words = chunk_y;

                        foreach (var word in words)
                        {
                            var min = word.SelectMany(w => w.BoundingBox.Vertices).OrderBy(v => v.X * v.Y).FirstOrDefault();
                            var max = word.SelectMany(w => w.BoundingBox.Vertices).OrderByDescending(v => v.X * v.Y).FirstOrDefault();
                            var box = $"({min.X}, {min.Y}) - ({max.X}, {max.Y})";
                            var _text = string.Join("", word.Select(w => string.Join("", w.Symbols.Select(s => s.Text))));

                            //System.Diagnostics.Debug.WriteLine($"    box: {box}, Word: {_text}");
                            var rWord = new RelicWord()
                            {
                                text = _text,
                                rect = new Rectangle(
                                            new Point(min.X, min.Y),
                                            new Size(max.X - min.X, max.Y - min.Y)
                                        ),
                                chars = word.SelectMany(w => w.Symbols)
                                            .Select(s =>
                                                new RelicWord
                                                {
                                                    rect = s.BoundingBox.Vertices.ToRectangle(),
                                                    text = string.Join("", s.Text),
                                                }),
                            };

                            var doSplit = false;
                            do
                            {
                                RelicWord rWord1 = null;
                                RelicWord rWord2 = null;
                                try
                                {
                                    doSplit = splitWords(rWord, out rWord1, out rWord2);
                                }
                                catch (Exception ex)
                                {
                                    // エラー時のフェールセーフ対策
                                    rWord1 = rWord;
                                    rWord2 = null;
                                    doSplit = false;
                                }
                                yield return rWord1;
                                if (rWord2 != null)
                                {
                                    rWord = rWord2;
                                }
                            } while (doSplit);
                        }
                    }
                }
            }
        }

        private IEnumerable<RelicWord> WordAnalyzeV2(TextAnnotation text)
        {
            var words = text.Pages
                            .SelectMany(p => p.Blocks)
                            .SelectMany(b => b.Paragraphs)
                            .SelectMany(p => p.Words);

            var groups = words
                        .GroupBy(word => word.BoundingBox.Vertices.Min(v => v.Y))
                        .OrderBy(group => group.Key);

            var chunk_y = getChunkWords_y(groups);

            foreach (var word in chunk_y)
            {
                var min = word.SelectMany(w => w.BoundingBox.Vertices).OrderBy(v => v.X * v.Y).FirstOrDefault();
                var max = word.SelectMany(w => w.BoundingBox.Vertices).OrderByDescending(v => v.X * v.Y).FirstOrDefault();
                var box = $"({min.X}, {min.Y}) - ({max.X}, {max.Y})";
                var _text = string.Join("", word.Select(w => string.Join("", w.Symbols.Select(s => s.Text))));

                //System.Diagnostics.Debug.WriteLine($"    box: {box}, Word: {_text}");
                var rWord = new RelicWord()
                {
                    text = _text,
                    rect = new Rectangle(
                                new Point(min.X, min.Y),
                                new Size(max.X - min.X, max.Y - min.Y)
                            ),
                    chars = word.SelectMany(w => w.Symbols)
                                .Select(s =>
                                    new RelicWord
                                    {
                                        rect = s.BoundingBox.Vertices.ToRectangle(),
                                        text = string.Join("", s.Text),
                                    }),
                };

                var doSplit = false;
                do
                {
                    RelicWord rWord1 = null;
                    RelicWord rWord2 = null;
                    try
                    {
                        doSplit = splitWords(rWord, out rWord1, out rWord2);
                    }
                    catch (Exception ex)
                    {
                        // エラー時のフェールセーフ対策
                        rWord1 = rWord;
                        rWord2 = null;
                        doSplit = false;
                    }
                    yield return rWord1;
                    if (rWord2 != null)
                    {
                        rWord = rWord2;
                    }
                } while (doSplit);
            }
        }

        private bool splitWords(RelicWord rWord, out RelicWord rWord1, out RelicWord rWord2)
        {
            rWord1 = null;
            rWord2 = null;

            bool doSplit = false;
            for (int i = 0; i < rWord.chars.Count(); i++)
            {
                var prevC = rWord.chars.ElementAtOrDefault(i - 1);
                var c = rWord.chars.ElementAtOrDefault(i);
                var nextC = rWord.chars.ElementAtOrDefault(i + 1);
                int prevMargin = c?.rect.X - prevC?.rect.Right ?? rWord.chars.ElementAtOrDefault(i).rect.Width;
                int nextMargin = nextC?.rect.X - c?.rect.Right ?? 0;
                int diff = Math.Abs(prevMargin - nextMargin);
                if (0 < prevMargin &&
                    10 < diff &&
                    prevMargin * 5 < nextMargin)
                {
                    System.Diagnostics.Debug.WriteLine($"------");
                    System.Diagnostics.Debug.WriteLine($"text:{rWord.text}");
                    System.Diagnostics.Debug.WriteLine($"prev:{prevC}");
                    System.Diagnostics.Debug.WriteLine($"crnt:{c}");
                    System.Diagnostics.Debug.WriteLine($"next:{nextC}");
                    System.Diagnostics.Debug.WriteLine($"prevMargin:{prevMargin}, nextMargin:{nextMargin}");
                    System.Diagnostics.Debug.WriteLine($"------");
                    
                    var split1 = rWord.chars.Take(i + 1).Aggregate((w1, w2) => new RelicWord() { text = w1.text + w2.text, rect = Rectangle.Union(w1.rect, w2.rect), });
                    var split2 = rWord.chars.Skip(i + 1).Aggregate((w1, w2) => new RelicWord() { text = w1.text + w2.text, rect = Rectangle.Union(w1.rect, w2.rect), });

                    split1.chars = rWord.chars.Take(i + 1);
                    split2.chars = rWord.chars.Skip(i + 1);

                    rWord1 = split1;
                    rWord2 = split2;
                    doSplit = true;
                    break;
                }
            }

            if (doSplit == false)
            {
                rWord1 = rWord;
                rWord2 = null;
            }

            return doSplit;
        }

        public IEnumerable<IEnumerable<Status>> chunkRelicSubStatus(IEnumerable<Status> sub_status)
        {
            var list = new List<List<Status>>();

            if(sub_status.Count() <= 4)
            {
                list.Add(sub_status.ToList());
                return list;
            }

            var assignments = sub_status.OrderBy(s => Point.Empty.Distance(s.rect.Location)).ToList();

            while (assignments.Any())
            {
                var means = assignments.FirstOrDefault();
                if (means == null) { list.Add(assignments); break; }

                var min_x = means.rect.X - means.rect.Width;
                var max_x = means.rect.X + means.rect.Width;
                var min_y = means.rect.Y - means.rect.Width;
                var max_y = means.rect.Y + means.rect.Height * 5.5;// 聖遺物のサブステータス:4行+行間0.5行×3=5.5

                var tmpList = assignments
                                .Where(r =>
                                    (min_x <= r.rect.X && r.rect.X <= max_x) &&
                                    (min_y <= r.rect.Y && r.rect.Y <= max_y))
                                .ToList();
                
                // 1個のみのステータスはノイズデータとして無視する
                if (1 < tmpList.Count)
                {
                    list.Add(tmpList);
                }
                assignments = assignments.Except(tmpList).ToList();
            }

            return list;
        }

        private static IEnumerable<IEnumerable<Word>> getChunkWords_y(IOrderedEnumerable<IGrouping<int, Word>> groups)
        {
            for (int i = 0; i < groups.Count(); i++)
            {
                List<Word> _list = null;
                var word = groups.ElementAt(i);
                var word_y = word.Key;
                for (var j = i; j < groups.Count(); j++)
                {
                    var nextWord = groups.ElementAt(i);
                    var nextWord_y = nextWord.Key;
                    var diff = nextWord_y - word_y;
                    if (diff < 5)
                    {
                        _list = _list ?? new List<Word>();
                        _list.AddRange(nextWord);
                        i++;
                    }
                    else
                    {
                        _list = _list.OrderBy(w => w.BoundingBox.Vertices.Select(v => v.X).Min()).ToList();
                        yield return _list;
                        _list = null;
                        i--;
                        break;
                    }

                }

                if (_list != null) { yield return _list.OrderBy(w => w.BoundingBox.Vertices.Select(v => v.X).Min()).ToList(); }
            }
        }

        public List<Status> getRelicSubStatus(IEnumerable<RelicWord> lines)
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
            // オプション毎にステータスを記録
            //------------------------------
            var list = statusList
               .Select(
                    l =>
                    {
                        var t = l.text.Split("+");
                        var sub_status = new KeyValuePair<string, double>(
                            t.ElementAtOrDefault(0) + (t.ElementAtOrDefault(1).EndsWith("%") ? "%" : ""),
                            double.Parse(t.ElementAtOrDefault(1).Replace("%", "")));

                        return new Status()
                        {
                            pair = sub_status,
                            rect = l.rect,
                        };
                    })
               .Distinct()
               .ToList();

            var dic = filterOptions(list);

            return dic;
        }

        private IEnumerable<RelicWord> getStatus(IEnumerable<RelicWord> lines, string pattern, string numeric_pattern, bool needsMainStatus = false)
        {
            //------------------------------
            // ノイズ情報除去
            //------------------------------
            lines = lines.Where(text => text.text != "+20");

            //------------------------------
            // パターンに一致する文字列を抽出
            //------------------------------
            var regex = new Regex(pattern);
            var statusList = lines.Where(text => regex.IsMatch(text.text));

            //------------------------------
            // パターンに一致しない場合
            // （聖遺物強化画面のキャプチャの場合）
            //------------------------------
            bool isDetailPage = statusList.Any() == false;
            if (isDetailPage || needsMainStatus)
            {
                // 聖遺物強化画面の場合はOCR結果が「攻撃力\n5.7%」のような形で取得されるので
                // 数値っぽい値の場合前の行と合成してパターンを作る
                var _statusList = lines
                .Select((t, i) =>
                {
                    if (i < 1) { return t; }

                    if (Regex.IsMatch(t.text, numeric_pattern))
                    {
                        return lines.ElementAt(i - 1).MergeFrom(t);
                    }

                    return t;
                }).ToList();

                double pointDistance(Point point1, Point point2)
                {
                    double ret = Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2));
                    return ret;
                }
                var _statusListV2 = lines.Where(t => t.text != "+20")
                                        //.Where(t => "1234567890".Where(c => t.text.Contains(c)).Any() == false)
                                        .Select(t =>
                                        {
                                            var near = lines.Where(t2 => t2.text != t.text)
                                                            //.Where(t => "1234567890".Where(c => t.text.Contains(c)).Any())
                                                            .OrderBy(t2 => pointDistance(t.rect.Location, t2.rect.Location))
                                                            .FirstOrDefault();
                                            if (near != null) { t = t.MergeFrom(near); }
                                            return t;
                                        });

                var _statusListV3 = lines.Where(t => t.text != "+20")
                                        .Where(t => "1234567890".Where(c => t.text.Contains(c)).Any() == false)
                                        .Select(t =>
                                        {
                                            var x = t.rect.X;
                                            var min_y = t.rect.Y - t.rect.Height;
                                            var max_y = t.rect.Y + t.rect.Height;

                                            var nears = lines
                                                            .Where(t => "1234567890".Where(c => t.text.Contains(c)).Any())
                                                            .Where(t2 => (x < t2.rect.X) &&
                                                                         (min_y < t2.rect.Y && t2.rect.Y < max_y))
                                                            //.OrderBy(t2 => Math.Pow(t.rect.Top - t2.rect.Top, 2))
                                                            .OrderBy(t2 => Math.Pow(t.rect.Left - t2.rect.Left, 2)).ToList();
                                            var near = nears.FirstOrDefault();
                                            if (near != null) { t = t.MergeFrom(near); }
                                            return t;
                                        });
                _statusList.AddRange(statusList);
                _statusList.AddRange(_statusListV2);
                _statusList.AddRange(_statusListV3);
                statusList = _statusList;
            }

            //------------------------------
            // 最終フィルタ実施
            // (セット効果等をフィルタ実施)
            //------------------------------
            statusList = statusList.Select(text =>
            {
                return (text: text, match: Regex.Match(text.text, pattern));
            })
            .Where(m =>
            {
                // 記号は無視する
                var size = m.text.text
                            .Where(c => char.IsSymbol(c) == false)
                            .Where(c => char.IsPunctuation(c) == false)
                            .Where(c => char.IsWhiteSpace(c) == false)
                            .Count();
                // セット効果の説明文などをフィルタするために、ステータスらしき箇所だけ抽出する
                var hitRate = ((double)m.match.Value.Length / size);
                return 0.8 < hitRate;
            })
            .Select(m =>
            {
                m.text.text = $"{m.match.Groups["class"]}+{m.match.Groups["value"]}";

                return m.text.extraWords();
            })
            .ToArray();

            return statusList;
        }

        private List<Status> filterOptions(List<Status> _subStatusOption)
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
                {"HP",              ( 110d,  299  * 6)},
                {"HP%",             ( 2.5d,  5.8d * 6)},
                {"元素熟知",          (  10d,  23   * 6)},
                {"元素チャージ効率%",  ( 2.7d,  6.5  * 6)},
                {"会心率%",          ( 1.6d,  3.9  * 6)},
                {"会心ダメージ%",     ( 3.3d,  7.8  * 6)},
                {"会心率",           ( 0.0d,  0.0d)},
                {"会心ダメージ",      ( 0.0d,  0.0d)},
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
            var sameOptions = _subStatusOption.GroupBy(status => status.pair.Key)
                                              .Where(g => g.Count() != 1);
            foreach (var g in sameOptions)
            {
                var _removeList = new List<Status>();

                var effects = g.Where(s => ignoreSetEffects.Contains((s.pair.Key, s.pair.Value)));
                _removeList.AddRange(effects);


                // 削除
                foreach (var removeItem in _removeList)
                {
                    _subStatusOption.Remove(removeItem);
                }
            }

            // 中身をコピー
            var subStatusOption = new List<Status>(_subStatusOption);

            //------------------------------
            // あり得ない値（メイン効果等）の削除
            //------------------------------
            foreach ( var scoreRatePair in scoreRates)
            {
                var minValue = scoreRatePair.Value.minValue;
                var maxValue = scoreRatePair.Value.maxValue;

                // 無ければ次の検索へ
                var values = subStatusOption
                                .Where(s => s.pair.Key == scoreRatePair.Key)
                                .Where(value => value.pair.Value < minValue || maxValue < value.pair.Value).ToList();
                foreach(var value in values)
                {
                    subStatusOption.Remove(value);
                }
            }
            if (subStatusOption.Any())
            {
                var max = subStatusOption.Select(s => s.rect.Width * s.rect.Height).Max();
                if (max != 0)
                {
                     subStatusOption = subStatusOption
                        .Where(s => 0.1 < ((s.rect.Width * s.rect.Height) / (double)max))
                        .ToList();
                }
             }
            return subStatusOption;
        }

        public List<Status> getMainStatus(IEnumerable<RelicWord> lines, List<Status> relicSubStatusList)
        {
            //------------------------------
            // 正規表現パターン生成
            //------------------------------
            var status_classes = new[] { "攻撃力", "攻擊力", "防御力", "HP", "元素熟知", "元素チャージ効率", "会心率", "会心ダメージ", "与える治療効果", "物理ダメージ", "(炎|水|氷|雷|風|岩)元素ダメージ" };
            var status_class_pattern = string.Join("|", status_classes);
            var numeric_pattern = "(?<value>([1-9][\\d*|0|,]*)(\\.\\d+)?(%)?)";
            var pattern = $"(?<class>{status_class_pattern})(.*?)?" + "(\\+)?" + numeric_pattern;

            //------------------------------
            // サブステータスの場所に近い箇所を算出
            //------------------------------
            //if (relicSubStatusList.Any())
            //{
            //    var subRelicRect = relicSubStatusList.Select(s => s.rect)
            //                             .Aggregate((r1, r2) => Rectangle.Union(r1, r2));

            //    var min_x = subRelicRect.Left - subRelicRect.Width;
            //    var max_x = subRelicRect.Right * 1.5;
            //    lines = lines.Where(l => min_x <= l.rect.X && l.rect.X <= max_x);
            //}

            var statusList = getStatus(lines, pattern, numeric_pattern, needsMainStatus: true);

            var subStatusDic = getRelicSubStatus(lines).ToList();

            //------------------------------
            // オプション毎にステータスを記録
            //------------------------------
            var list = statusList
               .Select(
                    s =>
                    {
                        var t = s.text.Split("+");
                        var pair = new KeyValuePair<string, double>(
                            t.ElementAtOrDefault(0) + (t.ElementAtOrDefault(1).EndsWith("%") ? "%" : ""),
                            double.Parse(t.ElementAtOrDefault(1).Replace("%", "")));

                        return new Status()
                        {
                            pair = pair,
                            rect = s.rect,
                        };
                    })
               .Distinct()
               .ToList();

            var mainStatus = list.Except(subStatusDic);

            mainStatus = mainStatus
                .Where(m => 
                    subStatusDic.All(s => m.rect.IntersectsWith(s.rect) == false));

            return mainStatus.ToList();
        }

        public string getCategory(IEnumerable<RelicWord> lines, List<Status> relicSubStatusList)
        {
            var rect = Rectangle.Empty;
            if (relicSubStatusList.Any())
            {
                rect = relicSubStatusList
                            .Select(s => s.rect)
                            .Aggregate((r1, r2) => Rectangle.Union(r1, r2));
            }

            var categories = relicKind.Select(r => r.category).Distinct();
            var cList = lines
                .Select(s => (category: categories.Where(category => s.text.Contains(category)).FirstOrDefault(), line: s))
                .Where(t => t.category != null)
                .OrderBy(t => t.line.rect.Location.Distance(rect.Location));
            
            var category = cList.FirstOrDefault().category;

            if (category == null)
            {
                category = relicKind.Where(r => lines.Where(l => l.text.Contains(r.item)).Any())
                                    .Select(r => r.category)
                                    .FirstOrDefault();
            }

            return category ?? "";
        }

        public string getSetName(IEnumerable<RelicWord> lines, List<Status> relicSubStatusList)
        {
            var rect = Rectangle.Empty;
            if (relicSubStatusList.Any())
            {
                rect = relicSubStatusList
                            .Select(s => s.rect)
                            .Aggregate((r1, r2) => Rectangle.Union(r1, r2));
            }

            var sets = relicKind.Select(r => r.set).Distinct().Reverse();
            var sList = lines
                .Select(s => (set: sets.Where(set => s.text.Replace("沈論の心", "沈淪の心").Contains(set)).FirstOrDefault(), line: s))
                .Where(t => t.set != null)
                .OrderBy(t => t.line.rect.Location.Distance(rect.Location));

            var setName = sList.FirstOrDefault().set;

            if (setName == null)
            {
                setName = relicKind.Where(r => lines.Where(l => l.text.Contains(r.item)).Any())
                                .Select(r => r.set)
                                .FirstOrDefault();
            }

            return setName ?? "";
        }

        public string getCharacterName(IEnumerable<RelicWord> lines, List<Status> relicSubStatusList)
        {
            var rect = Rectangle.Empty;
            if (relicSubStatusList.Any())
            {
                rect = relicSubStatusList
                            .Select(s => s.rect)
                            .Aggregate((r1, r2) => Rectangle.Union(r1, r2));
            }

            var sets = characters.Select(c => c.character);
            var sList = lines
                .Select(s => (character: sets.Where(character => s.text.Contains(character)).FirstOrDefault(), line: s))
                .Where(t => t.character != null)
                .OrderBy(t => t.line.rect.Location.Distance(rect.Location));

            var characterName = sList.FirstOrDefault().character;

            return characterName ?? "";
        }

        public double calculateScore(List<Status> dic)
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

                var value = dic.Where(s => s.pair.Key == key).FirstOrDefault();
                if (value != null)
                {
                    score += value.pair.Value * rate;
                }
            }

            return score;
        }

        public Rectangle getCropHint(ResponseRelicData relic, bool hasMultipleRelic)
        {
            var rect = relic?.main_status?.rect ?? default;
            foreach (var status in relic.sub_status)
            {
                if (rect == default) { rect = status.rect; continue; }
                rect = Rectangle.Union(rect, status.rect);
            }

            if (hasMultipleRelic) { return rect; }
            //int min_x = rect.X - (orgimage.Width * 5 / 100);
            int min_x = rect.X;
            if (min_x < 0) { min_x = 0; }
            var wordsRect = relic.word_list.Select(w => w.rect).Where(w => min_x < w.X);
            foreach (var w in wordsRect)
            {
                rect = Rectangle.Union(rect, w);
            }

            return rect;
        }
    }
}
