using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExcelApp = Microsoft.Office.Interop.Excel.Application;
using CADApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HL.CAD;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.GraphicsInterface;
using HL.Utilities;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Diagnostics.Eventing.Reader;


namespace HL.ExcelToCAD
{
    /// <summary>
    /// ExcelToCAD2 COM组件方式  速度有点慢
    /// </summary>
    internal class ExcelToCAD2
    {
        /// <summary>
        /// 获取Windows进程的Id
        /// </summary>
        /// <param name="hwnd">应用程序窗口句柄</param>
        /// <param name="iD">out参，执行后返回的进程ID</param>
        /// <returns>线程号</returns>
        [DllImport("User32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hwnd, out int iD);

        /// <summary>
        /// 打开的Excel进程ID
        /// </summary>
        private int processId = 0;

        /// <summary>
        /// 表格行高
        /// </summary>
        private double table_RowHeight = 50;

        /// <summary>
        /// 文字大小
        /// </summary>
        private double font_Size = 35;

        /// <summary>
        /// 上一个表格的Y坐标
        /// </summary>
        private double PreTableY = 0;

        /// <summary>
        /// 文字样式-常规
        /// </summary>
        private ObjectId textStyleId_Normal = ObjectId.Null;

        /// <summary>
        /// 文字样式-加粗
        /// </summary>
        private ObjectId textStyleId_Bold = ObjectId.Null;

        /// <summary>
        /// 文字样式-斜体
        /// </summary>
        private ObjectId textStyleId_Italic = ObjectId.Null;

        /// <summary>
        /// 文字样式-粗斜体
        /// </summary>
        private ObjectId textStyleId_BoldItalic = ObjectId.Null;

        /// <summary>
        /// 转换设置
        /// </summary>
        public ConvertSetting setting = null;

        /// <summary>
        /// 是否为快速转换
        /// </summary>
        public bool isFastConvert = false;

        /// <summary>
        /// 是否全部转换成功
        /// </summary>
        public bool isSucess = true;

        /// <summary>
        /// 执行转换
        /// </summary>
        /// <param name="excelPath">Excel文件路径</param>
        /// <param name="convertSetting">转换项设置</param>
        /// <param name="fastConvert">当设置转换项时，是否为快速转换（true：单元格显示文字样式与双击单元格选中文字后显示样式不同时无法解析、转换，但速度较快；false:全格式支持，但速度较慢）</param>
        public void Run(string excelPath, ConvertSetting convertSetting, bool fastConvert = false)
        {
            setting = convertSetting;
            isFastConvert = fastConvert;

            //导入Excel
            ExcelApp excelApp = null;
            Workbook workbook = null;

            HLProgressBar progressBar = new HLProgressBar(title: "转换中...", isShow: false);

            try
            {
                //打开Excel并获取进程ID
                excelApp = new ExcelApp();
                GetWindowThreadProcessId((IntPtr)excelApp.Hwnd, out processId);

                workbook = excelApp.Workbooks.Open(excelPath);

                List<string> sheetNames = new List<string>();
                foreach (Worksheet item in workbook.Sheets)
                {
                    sheetNames.Add(item.Name);
                }

                //选择Sheet
                FormSelectSheet formSelectSheet = new FormSelectSheet(sheetNames);
                if (CADApp.ShowModalDialog(CADApp.MainWindow.Handle, formSelectSheet, false) == DialogResult.OK)
                {
                    var selectedSheets = formSelectSheet.SelectedSheets;
                    if (selectedSheets.Count == 0)
                        return;

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    CADApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"开始转换\n");

                    BaseTool.Clear();
                    progressBar.Show();
                    isSucess = true;

                    textStyleId_Normal = CreateEntity.CreateTextStyle("HLSTD", "Arial", "", font_Size, 0.7);
                    if (setting.IsFontStyle)
                    {
                        textStyleId_Bold = CreateEntity.CreateTextStyle("HLSTD_B", "Arial", "", font_Size, 0.7, true);
                        textStyleId_Italic = CreateEntity.CreateTextStyle("HLSTD_I", "Arial", "", font_Size, 0.7, false, true);
                        textStyleId_BoldItalic = CreateEntity.CreateTextStyle("HLSTD_BI", "Arial", "", font_Size, 0.7, true, true);
                    }

                    double proportion = 100d / selectedSheets.Count;  //每个Sheet的进度百分比
                    int index = 0;
                    foreach (var item in selectedSheets)
                    {
                        progressBar.SetTitle($"Sheet“{item}”转换中...");

                        Worksheet worksheet = workbook.Sheets[item];
                        DrawTable(worksheet, progressBar, proportion, index++ * proportion);
                    }

                    progressBar?.Exit();

                    BaseTool.ZoomAll();

                    stopwatch.Stop();
                    CADApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"转换完成，耗时{stopwatch.Elapsed.TotalSeconds:F2}秒！\n");

                    if (isSucess == false)
                        CADApp.ShowAlertDialog("部分表格未转换成功，详情请查看下方命令栏");
                }
            }
            catch (Exception ex)
            {
                progressBar?.Exit();
                CADApp.ShowAlertDialog(ex.Message);
                Log.WriteErrorLogAsync(ex);
            }
            finally
            {
                workbook?.Close(SaveChanges: false);
                excelApp?.Quit();

                //Excel进程不能及时关闭，用这种方式快速关闭
                if (processId != 0)
                {
                    Process processe = Process.GetProcessById(processId);
                    if (processe != null && !processe.HasExited)   //说明该进程还未关闭
                    {
                        processe.Kill();
                        processId = 0;
                    }
                }

                GC.Collect();
            }
        }

        /// <summary>
        /// 绘制表格
        /// </summary>
        /// <param name="worksheet">Excel工作表</param>
        /// <param name="progressBar">进度条</param>
        /// <param name="proportion">每个Sheet的进度占比</param>
        /// <param name="preProgress">前面进度条占比</param>
        private void DrawTable(Worksheet worksheet, HLProgressBar progressBar, double proportion, double preProgress)
        {
            object[,] excelData = worksheet.UsedRange.Value2 as object[,];

            if (excelData == null || excelData.GetLength(0) == 0 || excelData.GetLength(1) == 0)
            {
                CADApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nSheet“{worksheet.Name}”没有数据！\n");
                return;
            }

            //去掉空白数据，获取实际数据行列
            List<int> dataRows = new List<int>();
            List<int> dataCols = new List<int>();
            for (int i = 1; i <= excelData.GetLength(0); i++)
            {
                for (int j = 1; j <= excelData.GetLength(1); j++)
                {
                    if (excelData[i, j] != null && excelData[i, j].ToString().Trim() != "")
                    {
                        dataRows.Add(i);
                        dataCols.Add(j);
                    }
                }
            }

            if (dataRows.Count == 0)
            {
                CADApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Sheet“{worksheet.Name}”没有数据！\n");
                return;
            }

            //数据范围
            int startRow = dataRows.Min();
            int startCol = dataCols.Min();
            int endRow = dataRows.Max();
            int endCol = dataCols.Max();

            Table tb = new Table();
            tb.Direction = new Vector3d(1, 0, 0);  //调整表格方向，X横向排列，Y纵向排列
            tb.SetSize(endRow - startRow + 1, endCol - startCol + 1);
            tb.Position = new Point3d(0, PreTableY - table_RowHeight * 10, 0);

            //设置行高列宽
            SettingRowColumnSize(tb, worksheet, startRow, startCol, endRow, endCol, out List<int> hideRows, out List<int> hideCols);

            //添加实体
            CreateEntity.AddEntity(tb);

            int progressValue = (int)(proportion * (setting.IsFormatSetting ? 0.05 : 0.1) + preProgress); //此处进度占比：有转换项时为5%，否则为10%
            progressBar.SetProcess(progressValue);

            //默认情况下，表格第一行为标题行，整行合并单元格，且行高较高，若需设置需要在添加实体后，从新打开进行编辑否则报错“eNotApplicable”
            //标题：_TITLE；表头：_HEADER；数据：_DATA
            using (DocumentLock doclock = CADApp.DocumentManager.MdiActiveDocument.LockDocument())
            {
                using (Database db = CADApp.DocumentManager.MdiActiveDocument.Database)
                {
                    using (Transaction trans1 = db.TransactionManager.StartTransaction())
                    {
                        Entity entity = (Entity)trans1.GetObject(tb.ObjectId, OpenMode.ForWrite);
                        tb.Rows[0].Style = "_DATA";
                        tb.Rows[0].Height = table_RowHeight;

                        //绘制表格内容
                        DrawTableContent(tb, worksheet, excelData, startRow, startCol, endRow, endCol, progressBar, (proportion + preProgress - progressValue), progressValue);

                        //如果存在隐藏行列，删除隐藏行列,要从后往前删除，否则会影响行列索引
                        foreach (int row in hideRows.OrderByDescending(x => x))
                        {
                            tb.DeleteRows(row, 1);
                        }

                        foreach (int col in hideCols.OrderByDescending(x => x))
                        {
                            tb.DeleteColumns(col, 1);
                        }
                        trans1.Commit();
                    }
                }
            }

            //绘制Sheet名称 ，计算Table末尾Y
            CreateEntity.CreateDBText(new Point3d(tb.Position.X - 20, tb.Position.Y - 5, 0), worksheet.Name, table_RowHeight * 5, horizontalMode: TextHorizontalMode.TextRight, verticalMode: TextVerticalMode.TextTop, textStyleId: textStyleId_Normal);
            PreTableY = tb.Position.Y - tb.Height;

            //爆破表格
            if (setting.IsExplode)
                BaseTool.Explode(tb.ObjectId);
        }

        /// <summary>
        /// 设置CAD表格行高列宽
        /// </summary>
        /// <param name="tb">CAD表格</param>
        /// <param name="worksheet">Excel工作表</param>
        /// <param name="startRow">Excel数据起始行索引</param>
        /// <param name="startCol">Excel数据起始列索引</param>
        /// <param name="endRow">Excel数据结束行索引</param>
        /// <param name="endCol">Excel数据结束列索引</param>
        /// <param name="hideRows">隐藏行索引</param>
        /// <param name="hideCols">隐藏列索引</param>
        private void SettingRowColumnSize(Table tb, Worksheet worksheet, int startRow, int startCol, int endRow, int endCol, out List<int> hideRows, out List<int> hideCols)
        {
            hideRows = new List<int>();
            hideCols = new List<int>();

            if (setting.IsRowColumnSize)
            {
                //获取数据行列的每行行高，每列列宽，字体大小
                //Excel行高转厘米约为 /28.5，列宽转厘米约为 /4.6（行高列宽改变该比例改变，此处只取默认行高列宽的比例，好像是边框固定占据一定的长宽，好像跟像素也有关系）
                for (int i = startRow; i <= endRow; i++)
                {
                    double rowHeight = worksheet.Rows[i].RowHeight / 28.5 * 100;

                    if (rowHeight > 0)
                        tb.Rows[i - startRow].Height = rowHeight;
                    else
                        hideRows.Add(i - startRow);
                }
                for (int j = startCol; j <= endCol; j++)
                {
                    double colWidth = worksheet.Columns[j].ColumnWidth / 4.6 * 100;

                    if (colWidth > 0)
                        tb.Columns[j - startCol].Width = colWidth;
                    else
                        hideCols.Add(j - startCol);
                }
            }
            else
            {
                tb.SetRowHeight(table_RowHeight);
                tb.SetColumnWidth(table_RowHeight * 8);  //默认按照8倍行高来设置列宽
            }
        }

        /// <summary>
        /// 绘制表格内容
        /// </summary>
        /// <param name="tb">CAD表格</param>
        /// <param name="worksheet">Excel工作表</param>
        /// <param name="excelData">Excel格式化数据</param>
        /// <param name="startRow">Excel数据起始行索引</param>
        /// <param name="startCol">Excel数据起始列索引</param>
        /// <param name="endRow">Excel数据结束行索引</param>
        /// <param name="endCol">Excel数据结束列索引</param>
        /// <param name="progressBar">进度条</param>
        /// <param name="proportion">当前进度条占比</param>
        /// <param name="preProgress">前面进度条占比</param>
        private void DrawTableContent(Table tb, Worksheet worksheet, object[,] excelData, int startRow, int startCol, int endRow, int endCol, HLProgressBar progressBar, double proportion, double preProgress)
        {
            List<ExcelData> excelDatas = null;
            double proportion2 = 0;
            if (setting.IsFormatSetting)
            {
                //此处进度占比：有转换项时为85%
                proportion2 = proportion * 0.8;

                try
                {
                    excelDatas = GetExcelFormatData(tb, worksheet, startRow, startCol, endRow, endCol, progressBar, proportion2, preProgress);
                }
                catch
                {
                    isSucess = false;
                    CADApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Sheet“{worksheet.Name}”数据转换失败。原因：Excel格式解析失败，可能存在单元格样式与文字样式不同，请尝试点击“转换1”按钮进行转换，若问题依然存在请联系开发商\n");
                    CADApp.DocumentManager.MdiActiveDocument.Editor.Regen();

                    progressBar.SetProcess((int)(proportion + preProgress));
                    return;
                }

                preProgress += proportion2;
            }

            //写入数据
            double increment = (proportion - proportion2) / ((endRow - startRow + 1) * (endCol - startCol + 1)); // 每笔数据进度条增量
            int count = 0;
            for (int i = startRow; i <= endRow; i++)
            {
                for (int j = startCol; j <= endCol; j++)
                {
                    object value = excelData[i, j];
                    var range = worksheet.Cells[i, j];

                    if (setting.IsFormatSetting)
                    {
                        if (value != null || setting.IsEmptyCell)
                        {
                            ExcelData ed = excelDatas.First(x => x.RowIndex == i && x.ColumnIndex == j);
                            DrawTableCell(tb.Cells[i - startRow, j - startCol], ed);

                            //表格单元内垂直水平边距。为了向下兼容，引用的14版dll，没有Cell.Top等属性或方法，只能用过期方法
                            tb.SetMargin(i - startRow, j - startCol, CellMargins.Top, 5);
                            tb.SetMargin(i - startRow, j - startCol, CellMargins.Left, 5);
                        }
                    }
                    else if (value != null)
                    {
                        var cell = tb.Cells[i - startRow, j - startCol];
                        cell.TextString = value.ToString();
                        cell.TextHeight = font_Size;
                        cell.TextStyleId = textStyleId_Normal;
                        cell.Alignment = CellAlignment.MiddleCenter;

                        tb.SetMargin(i - startRow, j - startCol, CellMargins.Top, 3);
                        tb.SetMargin(i - startRow, j - startCol, CellMargins.Left, 3);
                    }

                    progressBar.SetProcess((int)(++count * increment + preProgress));
                }
            }

        }

        /// <summary>
        /// 绘制表格单元格
        /// </summary>
        /// <param name="cell">CAD单元格</param>
        /// <param name="excelData">Excel格式化数据</param>
        private void DrawTableCell(Cell cell, ExcelData excelData)
        {
            //字体大小。字体高度超出行高或长度超出列宽，会自动调整行高。 -1说明解析Excel字号失败
            cell.TextHeight = excelData.FontSize == -1 ? font_Size : excelData.FontSize;

            //字体颜色
            if (excelData.FontColor > 0)   // 小于0为DBNull，0为默认黑色 
            {
                System.Drawing.Color color = System.Drawing.ColorTranslator.FromOle(excelData.FontColor);
                cell.ContentColor = Autodesk.AutoCAD.Colors.Color.FromColor(color);
            }

            //字体样式
            if (excelData.FontBold && excelData.FontItalic)
                cell.TextStyleId = textStyleId_BoldItalic;
            else if (excelData.FontBold)
                cell.TextStyleId = textStyleId_Bold;
            else if (excelData.FontItalic)
                cell.TextStyleId = textStyleId_Italic;
            else
                cell.TextStyleId = textStyleId_Normal;

            //背景色
            if (excelData.BackgroundColor > 0 && excelData.BackgroundColor != 16777215)   //非DBNull  非默认白色
            {
                System.Drawing.Color color = System.Drawing.ColorTranslator.FromOle(excelData.BackgroundColor);
                cell.BackgroundColor = Autodesk.AutoCAD.Colors.Color.FromColor(color);
            }

            //单元格对齐方式
            var alingH = excelData.HorizontalAlignment;
            var alingV = excelData.VerticalAlignment;

            if (alingH == (int)XlHAlign.xlHAlignLeft && alingV == (int)XlVAlign.xlVAlignTop)
                cell.Alignment = CellAlignment.TopLeft;
            else if (alingH == (int)XlHAlign.xlHAlignCenter && alingV == (int)XlVAlign.xlVAlignTop)
                cell.Alignment = CellAlignment.TopCenter;
            else if (alingH == (int)XlHAlign.xlHAlignRight && alingV == (int)XlVAlign.xlVAlignTop)
                cell.Alignment = CellAlignment.TopRight;
            else if (alingH == (int)XlHAlign.xlHAlignGeneral && alingV == (int)XlVAlign.xlVAlignTop)
            {
                //水平对齐方式为：常规，即按照数据类型对齐，文本靠左，数字靠右
                if (double.TryParse(excelData.FormatText, out double _))
                    cell.Alignment = CellAlignment.TopRight;
                else
                    cell.Alignment = CellAlignment.TopLeft;
            }

            else if (alingH == (int)XlHAlign.xlHAlignLeft && alingV == (int)XlVAlign.xlVAlignCenter)
                cell.Alignment = CellAlignment.MiddleLeft;
            else if (alingH == (int)XlHAlign.xlHAlignCenter && alingV == (int)XlVAlign.xlVAlignCenter)
                cell.Alignment = CellAlignment.MiddleCenter;
            else if (alingH == (int)XlHAlign.xlHAlignRight && alingV == (int)XlVAlign.xlVAlignCenter)
                cell.Alignment = CellAlignment.MiddleRight;
            else if (alingH == (int)XlHAlign.xlHAlignGeneral && alingV == (int)XlVAlign.xlVAlignCenter)
            {
                if (double.TryParse(excelData.FormatText, out double _))
                    cell.Alignment = CellAlignment.MiddleRight;
                else
                    cell.Alignment = CellAlignment.MiddleLeft;
            }

            else if (alingH == (int)XlHAlign.xlHAlignLeft && alingV == (int)XlVAlign.xlVAlignBottom)
                cell.Alignment = CellAlignment.BottomLeft;
            else if (alingH == (int)XlHAlign.xlHAlignCenter && alingV == (int)XlVAlign.xlVAlignBottom)
                cell.Alignment = CellAlignment.BottomCenter;
            else if (alingH == (int)XlHAlign.xlHAlignRight && alingV == (int)XlVAlign.xlVAlignBottom)
                cell.Alignment = CellAlignment.BottomRight;
            else if (alingH == (int)XlHAlign.xlHAlignGeneral && alingV == (int)XlVAlign.xlVAlignBottom)
            {
                if (double.TryParse(excelData.FormatText, out double _))
                    cell.Alignment = CellAlignment.BottomRight;
                else
                    cell.Alignment = CellAlignment.BottomLeft;
            }

            else
                cell.Alignment = CellAlignment.MiddleCenter;

            //单元格数字显示格式
            cell.TextString = excelData.FormatText;
        }

        /// <summary>
        /// 获取Excel格式化数据
        /// </summary>
        /// <param name="tb">CAD表格</param>
        /// <param name="worksheet">Excel工作表</param>
        /// <param name="startRow">Excel数据起始行索引</param>
        /// <param name="startCol">Excel数据起始列索引</param>
        /// <param name="endRow">Excel数据结束行索引</param>
        /// <param name="endCol">Excel数据结束列索引</param>
        /// <param name="progressBar">进度条</param>
        /// <param name="proportion">进度条占比</param>
        /// <param name="preProgress">前面进度条占比</param>
        /// <returns>Excel格式化数据</returns>
        private List<ExcelData> GetExcelFormatData(Table tb, Worksheet worksheet, int startRow, int startCol, int endRow, int endCol, HLProgressBar progressBar, double proportion, double preProgress)
        {
            List<ExcelData> dataList = new List<ExcelData>();
            List<Task> tasks = new List<Task>();
            List<string> mergeCells = new List<string>();
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            double increment = proportion / ((endRow - startRow + 1) * (endCol - startCol + 1)); // 每笔数据进度条增量
            int count = 0;
            for (int i = startRow; i <= endRow; i++)
            {
                var task = Task.Factory.StartNew((object obj) =>
                 {
                     if (cancellationToken.IsCancellationRequested)
                         return;

                     int rowIndex = (int)obj;

                     for (int j = startCol; j <= endCol; j++)
                     {
                         Range range = worksheet.Cells[rowIndex, j];
                         if (range.Value2 != null || setting.IsEmptyCell)
                         {
                             ExcelData data = new ExcelData();
                             data.RowIndex = rowIndex;
                             data.ColumnIndex = j;

                             if (setting.IsMergeCells && range.MergeCells == true)
                             {
                                 var address = range.MergeArea.Address;
                                 mergeCells.Add(address);
                             }

                             try
                             {
                                 if (setting.IsFontSize)
                                 {
                                     //字体按30倍大小计算(理应按照28.4，但是绘制出来的字体有点大)。特殊情况下（单元格字体样式与双击单元格后选中文字设置的字体样式不一致）为DBNull
                                     if (isFastConvert)
                                         data.FontSize = range.Font.Size / 30 * 100;
                                     else
                                         data.FontSize = range.Font.Size is DBNull ? -1 : range.Font.Size / 30 * 100;
                                 }
                                 else
                                     data.FontSize = font_Size;

                                 if (setting.IsFontColor)
                                 {
                                     if (isFastConvert)
                                         data.FontColor = (int)range.Font.Color;
                                     else
                                         data.FontColor = range.Font.Color is DBNull ? -1 : (int)range.Font.Color;     //特殊情况下为DBNull
                                 }
                                 else
                                     data.FontColor = 0;

                                 if (setting.IsFontStyle)
                                 {
                                     if (isFastConvert)
                                     {
                                         data.FontBold = range.Font.Bold;
                                         data.FontItalic = range.Font.Italic;
                                     }
                                     else
                                     {
                                         data.FontBold = range.Font.Bold is DBNull ? false : range.Font.Bold;
                                         data.FontItalic = range.Font.Italic is DBNull ? false : range.Font.Italic;
                                     }
                                 }
                                 else
                                 {
                                     data.FontBold = false;
                                     data.FontItalic = false;
                                 }

                                 if (setting.IsFill)
                                 {
                                     if (isFastConvert)
                                         data.BackgroundColor = (int)range.Interior.Color;
                                     else
                                         data.BackgroundColor = range.Interior.Color is DBNull ? -1 : (int)range.Interior.Color;   //特殊情况下为DBNull
                                 }
                                 else
                                     data.BackgroundColor = 0;

                                 if (setting.IsAlign)
                                 {
                                     if (isFastConvert)
                                     {
                                         data.HorizontalAlignment = range.HorizontalAlignment;
                                         data.VerticalAlignment = range.VerticalAlignment;
                                     }
                                     else
                                     {
                                         data.HorizontalAlignment = range.HorizontalAlignment is DBNull ? (int)XlHAlign.xlHAlignGeneral : range.HorizontalAlignment;
                                         data.VerticalAlignment = range.VerticalAlignment is DBNull ? (int)XlVAlign.xlVAlignTop : range.VerticalAlignment;
                                     }
                                 }
                                 else
                                 {
                                     data.HorizontalAlignment = (int)XlHAlign.xlHAlignGeneral;
                                     data.VerticalAlignment = (int)XlVAlign.xlVAlignCenter;
                                 }
                             }
                             catch (Exception ex)
                             {
                                 cancellationToken.Cancel();
                                 throw ex;
                             }

                             if (setting.IsFormat)
                                 data.FormatText = range.Text;
                             else
                                 data.FormatText = range.Value2?.ToString();

                             lock (dataList)
                                 dataList.Add(data);
                         }

                         Interlocked.Increment(ref count);
                         progressBar.SetProcess((int)(count * increment + preProgress));
                     }
                 }, i, cancellationToken.Token);

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            if (mergeCells.Count > 0)
                MergeCells(mergeCells.Where(m => !string.IsNullOrEmpty(m)).Distinct(), tb, startRow, startCol, endRow, endCol);

            return dataList;
        }

        /// <summary>
        /// 合并单元格
        /// </summary>
        /// <param name="mergeCells">Excel合并单元格范围</param>
        /// <param name="tb">CAD表格</param>
        /// <param name="startRow">Excel数据起始行索引</param>
        /// <param name="startCol">Excel数据起始列索引</param>
        /// <param name="endRow">Excel数据结束行索引</param>
        /// <param name="endCol">Excel数据结束列索引</param>
        private void MergeCells(IEnumerable<string> mergeCells, Table tb, int startRow, int startCol, int endRow, int endCol)
        {
            //合并单元格范围转换为行列索引
            foreach (var item in mergeCells)
            {
                var address = item.Split(':');
                var start = address[0].Split('$').Where(s => !string.IsNullOrEmpty(s)).ToArray();
                var end = address[1].Split('$').Where(s => !string.IsNullOrEmpty(s)).ToArray();

                int mergeCellStartRow = int.Parse(start[1]);
                int mergeCellStartCol = GetColumnIndex(start[0]);
                int mergeCellEndRow = int.Parse(end[1]);
                int mergeCellEndCol = GetColumnIndex(end[0]);

                int topRow = mergeCellStartRow - startRow;
                int leftCol = mergeCellStartCol - startCol;
                int bottomRow = mergeCellEndRow - startRow;
                int rightCol = mergeCellEndCol - startCol;

                //获取到的有效数据行列内的合并单元格范围超出表格范围，会报错“eInvalidInput”
                if (tb.Rows.Count - 1 < topRow)
                    topRow = tb.Rows.Count - 1;
                if (tb.Columns.Count - 1 < leftCol)
                    leftCol = tb.Columns.Count - 1;
                if (tb.Rows.Count - 1 < bottomRow)
                    bottomRow = tb.Rows.Count - 1;
                if (tb.Columns.Count - 1 < rightCol)
                    rightCol = tb.Columns.Count - 1;

                //合并单元格
                CellRange cr = CellRange.Create(tb, topRow, leftCol, bottomRow, rightCol);
                tb.MergeCells(cr);
            }
        }

        /// <summary>
        /// 获取列索引
        /// </summary>
        /// <param name="columnName">列名</param>
        /// <returns>列索引</returns>
        private int GetColumnIndex(string columnName)
        {
            columnName = columnName.ToUpper();
            string columnRef = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            int index = 0;
            if (columnName.Length < 2)
            {
                index = columnRef.IndexOf(columnName) + 1;
            }
            else
            {
                string col1 = columnName.ToCharArray()[0].ToString();
                string col2 = columnName.ToCharArray()[1].ToString();

                index = ((columnRef.IndexOf(col1) + 1) * 26) + (columnRef.IndexOf(col2) + 1);
            }

            return index;
        }

    }
}
