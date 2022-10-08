using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using genshin.relic.score.Extentions;
using genshin.relic.score.Models;
using genshin.relic.score.Models.Lambda;
using genshin.relic.score.Models.ResponseData;
using genshin_relic_score;
using Google.Cloud.Vision.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace genshin_relic
{
    public partial class Form1 : Form
    {
        Task _updateTask = null;
        List<DataSource> _list = new List<DataSource>();
        BindingList<DataSource> bindList = new BindingList<DataSource>();

        private int pageCount = 0;

        private int pageSize
        {
            get => chkOnlyDiffirent.Checked ? maxCount : 1000;
        }
 
        private RelicManager relicManager;

        private int maxCount { get => relicManager?.maxCount ?? 0; }

        private int maxPageSize
        {
            get => (int) Math.Ceiling(maxCount / (double)pageSize);
        }

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                column.SortMode = DataGridViewColumnSortMode.Automatic;
            }

            timer1.Enabled = true;
            lblPage.Text = $"{pageCount + 1}/{maxPageSize}";
            this.WindowState = FormWindowState.Maximized;

            _updateTask = updateList();
            await _updateTask;
        }

        //const string dir = @"C:\dev\aws\s3\relic-server-log_next\image";
        const string dir = @"C:\dev\aws\s3\relic-server-log_next\image";

        private async Task updateList()
        {
            var cached = chkCached.Checked.ToString();
            dataGridView1.DataSource = bindList;

            await Task.Run(async () =>
            {
                relicManager = new RelicManager(cached, dir, chkVerify.Checked);
                var bindListAsync = relicManager.updateList().ConfigureAwait(false);
                changeEnalbed();

                await foreach (var datasource in bindListAsync)
                {
                    _list.Add(datasource);
                }
            }).ConfigureAwait(false);
        }


        private async void button1_Click(object sender, EventArgs e)
        {
            _updateTask = updateList();
            await _updateTask;
        }

        private IEnumerable<DataSource> extractRelicData(DataSource dataSource)
        {
            var dataSourceList = new List<DataSource>();
            dataSourceList.Add(dataSource);

            if (dataSource.relic.extendRelic != null)
            {
                foreach (var exRelic in dataSource.relic.extendRelic)
                {
                    var dataSourceEx = DataSource.Create(Path.Combine(dir, dataSource.FileName), exRelic);
                    exRelic.RelicMD5 = dataSource.relic.RelicMD5;
                    dataSourceEx.index = dataSource.index;
                    dataSourceList.Add(dataSourceEx);
                }
            }

            return dataSourceList;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            var selected = dataGridView1.SelectedRows;
            if (selected.Count <= 0) { return; }

            var item = selected[0];
            var index = item.Index;
            var relic = ((BindingList<DataSource>)dataGridView1.DataSource)[index].relic;
            var fileName = selected[0].Cells?[1]?.Value?.ToString() ?? "";
            var filePath = Path.Combine(dir, fileName);

            if (File.Exists(filePath))
            {
                txtUrl.Text = filePath;
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                //using var image = System.Drawing.Image.FromStream(stream);
                //changeImage(originalImage, image);

                //// 描画範囲を決める
                //Rectangle rect = getCropRange(originalImage.Image, relic);

                //// 画像を切り抜く
                //using var orgimage = new Bitmap(originalImage.Image);
                //var dstImg = createCropImage(orgimage, rect, relic);
                //changeImage(cropImage, dstImg);
                try
                {
                    using var skBitmap = SKBitmap.Decode(stream);
                    changeImage(originalImage, skBitmap.ToBitmap());
                    using var skCropBitmap = createCropImage(skBitmap, relic);
                    changeImage(cropImage, skCropBitmap.ToBitmap());
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private SKBitmap createCropImage(SKBitmap orgimage, ResponseRelicData relic)
        {
            var cropHint = relic.cropHint;
            var crops = relic.sub_status?.Select(r => r.rect) ?? Enumerable.Empty<Rectangle>();
            foreach (var c in crops)
            {
                cropHint = Rectangle.Union(cropHint, c);
            }
            using var canvas = new SKCanvas(orgimage);
            var rect = cropHint.ToSKRect();

            var paint = new SKPaint
            {
                Color = SKColors.LightGreen,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (rect.Width + rect.Height) / 2 * 1 / 100,
            };

            var _rList = new List<ResponseRelicData>();
            _rList.Add(relic);
            //配下のものはひとまずやらない
            //if (relic.extendRelic != null)
            //{
            //    _rList.AddRange(relic.extendRelic);
            //}

            foreach (var r in _rList)
            {
                if (r.main_status != null)
                {
                    var preRect = r.main_status.rect;
                    var mainRect = preRect.ToSKRect();
                    canvas.DrawRect(mainRect, paint);
                }

                if (r.sub_status.Any())
                {
                    var subRects = r.sub_status.Select(s =>
                    {
                        var subRect = s.rect.ToSKRect();
                        return subRect;
                    }).ToArray();

                    foreach (var subRect in subRects)
                    {
                        canvas.DrawRect(subRect, paint);
                    }

                    var paint2 = new SKPaint
                    {
                        Color = SKColors.HotPink,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = (rect.Width + rect.Height) / 2 * 1 / 100,
                    };

                    var drawRect = subRects.Aggregate((r1, r2) => SKRectI.Union(r1, r2));
                    canvas.DrawRect(drawRect, paint2);
                }
            }


            rect.Left -= orgimage.Width * 5 / 100;
            rect.Top -= orgimage.Height * 5 / 100;
            rect.Right += (orgimage.Width * 5 / 100);
            rect.Bottom += (orgimage.Height * 5 / 100);
            var dstImg = new SKBitmap(rect.Width, rect.Height);
            orgimage.ExtractSubset(dstImg, rect);
            return dstImg;
        }

        private void changeImage(PictureBox pic, System.Drawing.Image bmp)
        {
            var oldImage = pic.Image;
            pic.Image = (Bitmap)bmp.Clone();
            if (oldImage != null)
            {
                oldImage.Dispose();
            }
        }

        private async void btnAnalyze_Click(object sender, EventArgs e)
        {
            //btnAnalyze.Enabled = false;
            //var start = DateTime.Now;
            //var relic = await relic_analyze(txtUrl.Text);
            //var end = DateTime.Now;

            //var srcList = list.Where(d => d.FileName == Path.GetFileName(txtUrl.Text));

            //var flat = relic.extendRelic ?? new List<ResponseRelicData>();
            //flat.Add(relic);
            //foreach (var r in flat)
            //{
            //    var p = srcList.Where(p => p.set == r.set).FirstOrDefault();
            //    if (p == null) continue;

            //    p.set = r.set;
            //    p.category = r.category;
            //    p.main_status = r.main_status?.ToString();
            //    p.sub_status1 = r.sub_status?.ElementAtOrDefault(0)?.ToString();
            //    p.sub_status2 = r.sub_status?.ElementAtOrDefault(1)?.ToString();
            //    p.sub_status3 = r.sub_status?.ElementAtOrDefault(2)?.ToString();
            //    p.sub_status4 = r.sub_status?.ElementAtOrDefault(3)?.ToString();
            //    p.score = r.score;
            //    p.character = r.character;

            //    p.LastUpdate = new FileInfo(txtUrl.Text).LastWriteTime;
            //    p.ProcessTime = (end - start);

            //    r.RelicMD5 = relic.RelicMD5;
            //    p.relic = r;
            //    btnAnalyze.Enabled = true;
            //}

            //foreach (DataGridViewCell cell in dataGridView1.SelectedCells)
            //{
            //    dataGridView1.UpdateCellValue(cell.ColumnIndex, cell.RowIndex);
            //}

            //var selected = dataGridView1.SelectedRows;
            //if (selected.Count <= 0) { return; }

            //var item = selected[0];
            //dataGridView1_RowsAdded(sender, new DataGridViewRowsAddedEventArgs(item.Index, item.Cells.Count));
        }

        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            var index = e.RowIndex;

            var gridRow = dataGridView1.Rows.Cast<DataGridViewRow>().ElementAtOrDefault(index);
            var dataSourceList = ((BindingList<DataSource>)dataGridView1.DataSource);
            var row = dataSourceList.ElementAtOrDefault(gridRow.Index);
            if (row == null) { return; }

            gridRow.DefaultCellStyle.BackColor = Color.White;
            if (row.relic.extendRelic?.Any() ?? false)
            {
                gridRow.DefaultCellStyle.BackColor = Color.LightBlue;
            }

            if (0 < index)
            {
                var prevRow = dataGridView1.Rows.Cast<DataGridViewRow>().ElementAtOrDefault(index - 1);
                if(prevRow.Cells[1].Value.ToString() == row.FileName)
                {
                    gridRow.DefaultCellStyle.BackColor = Color.LightBlue;
                }
            }
            if (chkOnlyDiffirent.Checked)
            {
                gridRow.Visible = false;
            }
            if (chkMultiRelic.Checked && gridRow.DefaultCellStyle.BackColor != Color.LightBlue)
            {
                gridRow.Visible = false;
            }

            //gridRow.Visible = false;
            try
            {

                var guild = row.relic.RelicMD5;
                var imageFilePath = Path.Combine(dir, $"{guild}.png");
                var jsonFilePath = Path.Combine(dir, $"{guild}_status.json");
                var relic = JsonConvert.DeserializeObject<ResponseRelicData>(File.ReadAllText(jsonFilePath));
                var dataSource = DataSource.Create(imageFilePath, relic);

                if(relic.extendRelic?.Any() ?? false)
                {
                    var r = relic.extendRelic.Append(relic).FirstOrDefault(r => $"{r.category}/{r.set}" == $"{row.category}/{row.set}") ?? relic;
                    r.RelicMD5 = relic.RelicMD5;
                    dataSource = DataSource.Create(imageFilePath, r);
                }

                var t = typeof(DisplayNameAttribute);
                var props = row.GetType()
                                .GetProperties()
                                .Select(p =>
                                {
                                    var attr = (DisplayNameAttribute)p.GetCustomAttributes(t, false).FirstOrDefault();
                                    var displaName = attr.DisplayName;

                                    var value1 = p.GetValue(row)?.ToString();
                                    var value2 = p.GetValue(dataSource)?.ToString();

                                    return (displaName, value1, value2);
                                });

                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    if (column.HeaderText == "処理時間") { continue; }
                    if (column.HeaderText == "col") { continue; }

                    var prop = props.Where(p => p.displaName == column.HeaderText)
                                    .FirstOrDefault();

                    var cell = gridRow.Cells
                                        .Cast<DataGridViewCell>()
                                        .Where(c => c.OwningColumn.HeaderText == prop.displaName)
                                        .FirstOrDefault();

                    if (prop.value1 != prop.value2 && prop.displaName != "装備キャラクター")
                    {
                        if (chkOnlyDiffirent.Checked)
                        {
                            gridRow.Visible = true;
                        }
                        cell.Style.BackColor = Color.Red;
                    }
                }
            }
            catch(Exception e2)
            {
                gridRow.DefaultCellStyle.BackColor = Color.Gray;
                //gridRow.Visible = false;
                return;
            }
        }

        private void イメージを開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = dataGridView1.SelectedRows;
            if (selected.Count <= 0) { return; }

            var item = selected[0];
            var relic = ((BindingList<DataSource>)dataGridView1.DataSource)[item.Index].relic;
            var fileName = item?.Cells?[1]?.Value?.ToString() ?? "";
            var filePath = Path.Combine(dir, fileName);

            var p = new Process();
            p.StartInfo = new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void ログファイルjsonを開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = dataGridView1.SelectedRows;
            if (selected.Count <= 0) { return; }

            var item = selected[0];
            var relic = ((BindingList<DataSource>)dataGridView1.DataSource)[item.Index].relic;
            var fileName = $"{relic?.RelicMD5}_status.json";
            var filePath = Path.Combine(dir, fileName);

            var p = new Process();
            p.StartInfo = new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if(0 < pageCount) { pageCount--; }

            lblPage.Text = $"{pageCount + 1}/{maxPageSize}";
            changeEnalbed();

            var min = pageCount * pageSize;
            bindList = new BindingList<DataSource>(_list.Skip(min).Take(pageSize).ToList());
            dataGridView1.DataSource = bindList;
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (pageCount < maxPageSize) { pageCount++; }

            lblPage.Text = $"{pageCount+1}/{maxPageSize}";
            changeEnalbed();

            var min = pageCount * pageSize;
            bindList = new BindingList<DataSource>(_list.Skip(min).Take(pageSize).ToList());
            dataGridView1.DataSource = bindList;
        }

        private void changeEnalbed()
        {
            if (0 < pageCount) { btnPrev.Enabled = true; }
            if (0 == pageCount) { btnPrev.Enabled = false; }

            if (pageCount < maxPageSize) { btnNext.Enabled = true; }
            if (maxPageSize-1 == pageCount) { btnNext.Enabled = false; }
        }

        private void btnWordsShow_Click(object sender, EventArgs e)
        {
            var selected = dataGridView1.SelectedRows;
            if (selected.Count <= 0) { return; }

            var item = selected[0];
            var index = item.Index;
            var relic = ((BindingList<DataSource>)dataGridView1.DataSource)[index].relic;
            var fileName = selected[0].Cells?[1]?.Value?.ToString();
            var filePath = Path.Combine(dir, fileName);

            if (File.Exists(filePath) == false) { return; }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            try
            {
                using var skBitmap = SKBitmap.Decode(stream);
                using var canvas = new SKCanvas(skBitmap);

                var paint = new SKPaint
                {
                    Color = SKColors.HotPink,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = (skBitmap.Width + skBitmap.Height) / 10 * 1 / 100,
                };
                foreach (var word in relic.word_list.OrderBy(w => w.rect.X).ThenBy(w => w.rect.Y))
                {
                    var paint2 = new SKPaint
                    {
                        Color = SKColors.Black,
                        StrokeWidth = (skBitmap.Width + skBitmap.Height) / 5 * 1 / 100,
                        TextSize = (int)(word.rect.Height * 0.8),
                        Typeface = SKTypeface.FromFamilyName(Font.FontFamily.Name),
                    };
                    canvas.DrawRect(word.rect.ToSKRect(), paint);
                    canvas.DrawRect(word.rect.ToSKRect(), new SKPaint() { Color = SKColors.BlanchedAlmond, Style = SKPaintStyle.Fill,});
                    canvas.DrawText(word.text, word.rect.Left, word.rect.Top + word.rect.Height, paint2);

                }
                changeImage(originalImage, skBitmap.ToBitmap());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void btnEdge_Click(object sender, EventArgs e)
        {
            //画像をグレースケールとして読み込み、平滑化する  
            Mat src = new Mat(txtUrl.Text, ImreadModes.Grayscale);

            Mat dst = new Mat();
            Mat hold = new Mat();
            Cv2.CvtColor(src, dst, ColorConversionCodes.GRAY2BGR);
            //Cv2.CvtColor(dst, dst, ColorConversionCodes.BGRA2RGB);

            //Cv2.Threshold(dst, hold, 127, 255, ThresholdTypes.Binary);
            //Cv2.BitwiseNot(hold, hold);
            //Cv2.BitwiseAnd(src, src, src, hold);
            //Cv2.ImShow("hold", hold);
            //Cv2.ImShow("src", src);


            //②輪郭抽出を行う  
            Mat edge = new Mat();
            Cv2.Canny(src, edge, 1, 20);
            Cv2.ImShow("edge", edge);

            //③標準Hough変換を行い、(ρ,θ)を取得
            double rhoBunkai = 1;//ρθ平面におけるρ方向の分解能(今回は1ピクセル)  
            double thetaBunkai = Math.PI / 360;//ρθ平面におけるθ方向の分解能
            int thresh = 50;//最小のライン交差数  
            var lines = Cv2.HoughLinesP(edge, rhoBunkai, thetaBunkai, thresh, 400, 25);


            //④直線の描画  
            for (int i = 0; i < lines.Length; i++)
            {
                if ((lines[i].P1.Y - lines[i].P2.Y) != 0 &&
                    (lines[i].P1.X - lines[i].P2.X) != 0) continue;

                //if ((lines[i].P2.X - lines[i].P1.X) < (src.Width / 2) &&
                //    (lines[i].P2.Y - lines[i].P1.Y) < (src.Height / 2)) continue;

                dst.Line(lines[i].P1, lines[i].P2, Scalar.Red, 1, LineTypes.AntiAlias);
            }
            Cv2.ImShow("dst", dst);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var progressCount = _list.Count;
            lblPage.Text = $"{pageCount + 1:#,0}/{maxPageSize:#,0}";
            lblSum.Text = $"{progressCount:#,0}/{relicManager.maxCount:#,0}";

            var min = pageCount * pageSize;
            var max = min + pageSize;
            if (bindList.Count() == 0 && max < progressCount)
            {
                bindList.AddRange(_list.Skip(min).Take(pageSize));
            }

            if (_updateTask.IsCompleted && progressCount == relicManager.maxCount)
            {
                timer1.Enabled = false;
            }
        }
    }
}
