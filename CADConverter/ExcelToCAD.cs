using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using CADApp = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.GraphicsInterface;
using System.Threading;
using System.Diagnostics.Eventing.Reader;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.POIFS.Crypt.Dsig;
using Autodesk.AutoCAD.Colors;
using NPOI.HSSF.Util;
using NPOI.SS.Formula.Functions;
using NPOI.SS.Util;
using NPOI.HSSF.Record.Chart;
using NPOI.XSSF.Model;
using System.Reflection;
using NPOI.XSSF.UserModel.Helpers;

namespace CADConverter
{
    /// <summary>
    /// Excel转CAD表格 NPOI方式
    /// </summary>
    internal class ExcelToCAD
    {
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
        private double preTableY = 0;

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
        private ConvertSetting setting = null;

        /// <summary>
        /// 是否全部转换成功
        /// </summary>
        private bool isSucess = true;

        /// <summary>
        /// 是否为97-2003版本Excel
        /// </summary>
        private bool is97_2003 = false;

        /// <summary>
        /// 图片序号
        /// </summary>
        private int imageNumber = 0;

        /// <summary>
        /// 执行转换
        /// </summary>
        /// <param name="excelPath">Excel文件路径</param>
        /// <param name="convertSetting">转换项设置</param>
        public void Run(string excelPath, ConvertSetting convertSetting)
        {
            setting = convertSetting;

            //导入Excel
            HLProgressBar progressBar = new HLProgressBar(title: "转换中...", isShow: false);

            IWorkbook workbook = null;
            try
            {
                //打开Excel并获取进程ID
                if (Path.GetExtension(excelPath).ToLower() == ".xls")
                    is97_2003 = true;
                else
                    is97_2003 = false;

                using (FileStream fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read))
                {
                    if (is97_2003)
                        workbook = new HSSFWorkbook(fs);
                    else
                        workbook = new XSSFWorkbook(fs);
                }

                List<string> sheetNames = new List<string>();

                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    sheetNames.Add(workbook.GetSheetName(i));
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

                    //清空模型，清空图片外部文件参照
                    CADTool.Clear();

                    if (setting.IsImage)
                        CADTool.ClearImageDefAndXRef();
                    imageNumber = 0;

                    progressBar.Show();
                    isSucess = true;

                    textStyleId_Normal = CADTool.CreateTextStyle("HLSTD", "Arial", "", font_Size, 0.7);
                    if (setting.IsFontStyle)
                    {
                        textStyleId_Bold = CADTool.CreateTextStyle("HLSTD_B", "Arial", "", font_Size, 0.7, true);
                        textStyleId_Italic = CADTool.CreateTextStyle("HLSTD_I", "Arial", "", font_Size, 0.7, false, true);
                        textStyleId_BoldItalic = CADTool.CreateTextStyle("HLSTD_BI", "Arial", "", font_Size, 0.7, true, true);
                    }

                    double proportion = 100d / selectedSheets.Count;  //每个Sheet的进度百分比
                    int index = 0;
                    foreach (var item in selectedSheets)
                    {
                        progressBar.SetTitle($"Sheet“{item}”转换中...");

                        ISheet sheet = workbook.GetSheet(item);
                        DrawTable(sheet, workbook, progressBar, proportion, index++ * proportion);
                    }

                    progressBar?.Exit();

                    CADTool.ZoomAll();

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
                workbook?.Close();
            }
        }

        /// <summary>
        /// 获取Excel单元格数据
        /// </summary>
        /// <param name="sheet">工作表</param>
        /// <param name="workbook">工作簿</param>
        /// <param name="rowHeights">行高集合</param>
        /// <param name="hideRows">隐藏行集合</param>
        /// <param name="rowBackgroundColor">行背景色集合(当设置整行的背景色时，该行没有获取到背景色的单元格用行背景色代替)</param>
        /// <returns>Excel单元格数据集合</returns>
        private List<ExcelData> GetExcelData(ISheet sheet, IWorkbook workbook, out Dictionary<int, double> rowHeights, out Dictionary<int, bool> hideRows, out Dictionary<int, byte[]> rowBackgroundColor)
        {
            rowHeights = new Dictionary<int, double>();
            hideRows = new Dictionary<int, bool>();
            rowBackgroundColor = new Dictionary<int, byte[]>();

            List<ExcelData> dataList = new List<ExcelData>();
            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                if (row != null)
                {
                    //获取行高
                    if (setting.IsRowColumnSize)
                    {
                        double rowHeight = row.Height / row.HeightInPoints;
                        rowHeights.Add(i, rowHeight);
                    }

                    //获取隐藏行
                    bool isHiddenRow = false;
                    if (is97_2003)
                        isHiddenRow = (row as NPOI.HSSF.UserModel.HSSFRow).IsHidden;
                    else
                        isHiddenRow = (row as NPOI.XSSF.UserModel.XSSFRow).Hidden.HasValue ? (row as NPOI.XSSF.UserModel.XSSFRow).Hidden.Value : false;
                    if (isHiddenRow)
                        hideRows.Add(i, isHiddenRow);

                    for (int j = 0; j < row.LastCellNum; j++)
                    {
                        ICell cell = row.GetCell(j);
                        if (cell != null)
                        {
                            //空白单元格
                            string cellValue = GetCellValue(cell, out bool isNumber);
                            if (!string.IsNullOrEmpty(cellValue) || setting.IsEmptyCell)
                            {
                                ExcelData data = new ExcelData();
                                data.RowIndex = i;
                                data.ColumnIndex = j;

                                data.FormatText = cellValue;
                                data.ValueIsNumeric = isNumber;

                                NPOI.SS.UserModel.IFont font = null;
                                if (setting.IsFontSize)
                                {
                                    font = workbook.GetFontAt(cell.CellStyle.FontIndex);
                                    data.FontSize = font.FontHeightInPoints / 30 * 100;
                                }
                                else
                                    data.FontSize = font_Size;

                                if (setting.IsFontColor)
                                {
                                    if (font == null)
                                        font = workbook.GetFontAt(cell.CellStyle.FontIndex);

                                    if (is97_2003)
                                    {
                                        HSSFColor color = (font as HSSFFont).GetHSSFColor((HSSFWorkbook)workbook);
                                        if (color != null)
                                            data.FontColorRGB = color.RGB;
                                    }
                                    else
                                    {
                                        XSSFColor color = (font as XSSFFont).GetXSSFColor();
                                        if (color != null)
                                            data.FontColorRGB = color.RGB;
                                    }
                                }

                                if (setting.IsFontStyle)
                                {
                                    if (font == null)
                                        font = workbook.GetFontAt(cell.CellStyle.FontIndex);

                                    data.FontBold = font.IsBold;
                                    data.FontItalic = font.IsItalic;
                                }
                                else
                                {
                                    data.FontBold = false;
                                    data.FontItalic = false;
                                }

                                if (setting.IsFill)
                                {
                                    if (is97_2003)
                                    {
                                        HSSFColor color = null;
                                        if (cell.CellStyle.FillPattern == FillPattern.SolidForeground)
                                            color = cell.CellStyle.FillForegroundColorColor as HSSFColor;
                                        else
                                            color = cell.CellStyle.FillBackgroundColorColor as HSSFColor;

                                        if (color != null)
                                            data.BackgroundColorRGB = color.RGB;
                                    }
                                    else
                                    {
                                        XSSFColor color = null;
                                        if (cell.CellStyle.FillPattern == FillPattern.SolidForeground)
                                            color = cell.CellStyle.FillForegroundColorColor as XSSFColor;
                                        else
                                            color = cell.CellStyle.FillBackgroundColorColor as XSSFColor;

                                        if (color != null)
                                            data.BackgroundColorRGB = color.RGB;
                                    }
                                }

                                if (setting.IsAlign)
                                {
                                    data.HorizontalAlignment = (int)cell.CellStyle.Alignment;
                                    data.VerticalAlignment = (int)cell.CellStyle.VerticalAlignment;
                                }
                                else
                                {
                                    data.HorizontalAlignment = (int)NPOI.SS.UserModel.HorizontalAlignment.General;
                                    data.VerticalAlignment = (int)NPOI.SS.UserModel.VerticalAlignment.Center;
                                }

                                dataList.Add(data);
                            }
                        }
                    }

                    if (setting.IsEmptyCell && setting.IsFill)   //需要处理空白单元格填充色，采用行的默认背景色。
                    {
                        if (row.IsFormatted && row.RowStyle != null)
                        {
                            byte[] colorByte = null;
                            if (is97_2003)
                            {
                                HSSFColor color = null;
                                if (row.RowStyle.FillPattern == FillPattern.SolidForeground)
                                    color = row.RowStyle.FillForegroundColorColor as HSSFColor;
                                else
                                    color = row.RowStyle.FillBackgroundColorColor as HSSFColor;

                                if (color != null)
                                    colorByte = color.RGB;
                            }
                            else
                            {
                                XSSFColor color = null;
                                if (row.RowStyle.FillPattern == FillPattern.SolidForeground)
                                    color = row.RowStyle.FillForegroundColorColor as XSSFColor;
                                else
                                    color = row.RowStyle.FillBackgroundColorColor as XSSFColor;

                                if (color != null)
                                    colorByte = color.RGB;
                            }

                            if (colorByte != null && (colorByte[0] + colorByte[1] + colorByte[2]) != 255 * 3)
                            {
                                rowBackgroundColor.Add(i, colorByte);
                            }
                        }
                    }
                }
                else if (setting.IsRowColumnSize)     //Row为null时，采用默认行高
                {
                    double rowHeight = sheet.DefaultRowHeight / sheet.DefaultRowHeightInPoints;
                    rowHeights.Add(i, rowHeight);
                }

            }

            return dataList;
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        /// <param name="cell">单元格</param>
        /// <param name="isNumber">是否为数字</param>
        /// <returns>单元格值</returns>
        private string GetCellValue(ICell cell, out bool isNumber)
        {
            string value = "";
            isNumber = false;

            Action<ICell> getNumberValue = (c) =>
            {
                if (setting.IsFormat)
                {
                    string format = c.CellStyle.GetDataFormatString();
                    if (format != null && format.Contains("_"))
                        format = format.Split('_')[0];
                    var stringValue = c.NumericCellValue.ToString(format);
                    if (double.TryParse(stringValue, out double doubleValue))
                        value = stringValue;
                    else
                        value = c.NumericCellValue.ToString();
                }
                else
                    value = c.NumericCellValue.ToString();
            };

            if (cell != null)
            {
                switch (cell.CellType)
                {
                    case NPOI.SS.UserModel.CellType.String:
                        value = cell.StringCellValue;
                        break;
                    case NPOI.SS.UserModel.CellType.Boolean:
                        value = cell.BooleanCellValue.ToString();
                        break;
                    case NPOI.SS.UserModel.CellType.Numeric:
                        bool isDate = DateUtil.IsCellDateFormatted(cell);//DateUtil.IsADateFormat(cell.CellStyle.DataFormat, cell.CellStyle.GetDataFormatString());  //先判断是否为日期
                        if (isDate)
                        {
                            //获取到的格式和实际格式不一致，会导致日期显示错误
                            //string format = cell.CellStyle.GetDataFormatString();
                            //if (format.Contains("_"))
                            //    format = format.Split('_')[0];

                            if (cell.DateCellValue.TimeOfDay.TotalSeconds == 0)
                                value = cell.DateCellValue.ToString("yyyy-MM-dd");
                            else
                                value = cell.DateCellValue.ToString();
                        }
                        else
                        {
                            getNumberValue(cell);

                            isNumber = true;
                        }
                        break;
                    case NPOI.SS.UserModel.CellType.Blank:
                        value = "";
                        break;
                    case NPOI.SS.UserModel.CellType.Error:
                        value = cell.ErrorCellValue.ToString();
                        break;
                    case NPOI.SS.UserModel.CellType.Formula:
                        try
                        {
                            getNumberValue(cell);

                            isNumber = true;
                        }
                        catch
                        {
                            try
                            {
                                value = cell.StringCellValue;
                            }
                            catch
                            {
                                value = "=" + cell.CellFormula;
                            }
                        }
                        break;
                    default:
                        value = "=" + cell.CellFormula;
                        break;
                }
            }

            return value;
        }

        /// <summary>
        /// 绘制表格
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="workbook">Excel工作簿</param>
        /// <param name="progressBar">进度条</param>
        /// <param name="proportion">每个Sheet的进度占比</param>
        /// <param name="preProgress">前面进度条占比</param>
        private void DrawTable(ISheet sheet, IWorkbook workbook, HLProgressBar progressBar, double proportion, double preProgress)
        {
            if (sheet.LastRowNum == 0 && sheet.GetRow(0) == null)
            {
                CADApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Sheet“{sheet.SheetName}”没有数据！\n");
                return;
            }

            //获取Excel格式化数据
            List<ExcelData> excelDatas = GetExcelData(sheet, workbook, out Dictionary<int, double> rowHeights, out Dictionary<int, bool> hideRows, out Dictionary<int, byte[]> rowBackgroundColor);

            int progressValue = (int)(preProgress + proportion * (setting.IsFormatSetting ? 0.05 : 0.02)); //此处进度占比：有转换项时为5%，否则为2%
            progressBar.SetProcess(progressValue);

            //数据范围
            var datas = excelDatas.Where(e => !string.IsNullOrWhiteSpace(e.FormatText));
            int startRow = datas.Min(x => x.RowIndex);
            int endRow = datas.Max(x => x.RowIndex);
            int startCol = datas.Min(x => x.ColumnIndex);
            int endCol = datas.Max(x => x.ColumnIndex);

            if (endCol < 0)
            {
                CADApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Sheet“{sheet.SheetName}”没有数据！\n");
                return;
            }

            //该行设置了整体背景色，但是某个单元格没有获取到背景色，则使用该行背景色
            foreach (var item in rowBackgroundColor)
            {
                for (int i = startCol; i <= endCol; i++)
                {
                    if (!excelDatas.Exists(e => e.RowIndex == item.Key && e.ColumnIndex == i))
                    {
                        ExcelData data = new ExcelData();
                        data.RowIndex = item.Key;
                        data.ColumnIndex = i;

                        data.FormatText = "";
                        data.BackgroundColorRGB = item.Value;
                        excelDatas.Add(data);
                    }
                }
            }

            //获取列宽、隐藏列
            Dictionary<int, double> colWidths = new Dictionary<int, double>();
            Dictionary<int, bool> hideCols = new Dictionary<int, bool>();
            for (int j = startCol; j <= endCol; j++)
            {
                if (setting.IsRowColumnSize)
                {
                    double colWidth = sheet.GetColumnWidth(j) / 256;
                    colWidths.Add(j, colWidth);
                }

                bool isHiddenCol = sheet.IsColumnHidden(j); //false;

                //var colStyle = sheet.GetColumnStyle(j);
                //if (colStyle != null)
                //    isHiddenCol = colStyle.IsHidden;
                if (isHiddenCol)
                    hideCols.Add(j, isHiddenCol);
            }

            progressValue = (int)(preProgress + proportion * (setting.IsFormatSetting ? 0.05 : 0.02));
            progressBar.SetProcess(progressValue);

            Autodesk.AutoCAD.DatabaseServices.Table tb = new Autodesk.AutoCAD.DatabaseServices.Table();
            tb.Direction = new Vector3d(1, 0, 0);  //调整表格方向，X横向排列，Y纵向排列
            tb.SetSize(endRow - startRow + 1, endCol - startCol + 1);
            tb.Position = new Point3d(0, preTableY - table_RowHeight * 10, 0);

            //设置行高列宽
            SettingRowColumnSize(tb, startRow, startCol, endRow, endCol, rowHeights, colWidths);

            //添加实体
            CADTool.AddEntity(tb);

            progressValue = (int)(preProgress + proportion * (setting.IsFormatSetting ? 0.05 : 0.02));
            progressBar.SetProcess(progressValue);

            //默认情况下，表格第一行为标题行，整行合并单元格，且行高较高，若需设置需要在添加实体后，从新打开进行编辑否则报错“eNotApplicable”
            //标题：_TITLE；表头：_HEADER；数据：_DATA
            using (DocumentLock doclock = CADApp.DocumentManager.MdiActiveDocument.LockDocument())
            {
                using (Database db = CADApp.DocumentManager.MdiActiveDocument.Database)
                {
                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        Entity entity = (Entity)trans.GetObject(tb.ObjectId, OpenMode.ForWrite);
                        tb.Rows[0].Style = "_DATA";
                        tb.Rows[0].Height = table_RowHeight;

                        //合并单元格 放在修改表头数据类型之后，否则第一行无法合并单元格
                        MergeCells(sheet, tb, startRow, startCol);

                        //绘制表格内容
                        DrawTableContent(tb, sheet, excelDatas, startRow, startCol, endRow, endCol, progressBar, (proportion + preProgress - progressValue), progressValue);

                        //删除隐藏行列
                        DeleteHiddenRowCol(tb, startRow, startCol, hideRows, hideCols);

                        trans.Commit();
                    }
                }
            }

            //绘制Sheet名称 ，计算Table末尾Y
            CADTool.CreateDBText(new Point3d(tb.Position.X - 20, tb.Position.Y - 5, 0), sheet.SheetName, table_RowHeight * 5, horizontalMode: TextHorizontalMode.TextRight, verticalMode: TextVerticalMode.TextTop, textStyleId: textStyleId_Normal);
            preTableY = tb.Position.Y - tb.Height;

            //爆破表格，否则鼠标划过表格时，会选中表格导致卡顿
            if (setting.IsExplode)
                CADTool.Explode(tb.ObjectId);

            //插入图片
            InsertImage(sheet, tb);

        }

        /// <summary>
        /// 设置CAD表格行高列宽
        /// </summary>
        /// <param name="tb">CAD表格</param>
        /// <param name="startRow">Excel数据起始行索引</param>
        /// <param name="startCol">Excel数据起始列索引</param>
        /// <param name="endRow">Excel数据结束行索引</param>
        /// <param name="endCol">Excel数据结束列索引</param>
        /// <param name="rowHeights">行高集合</param>
        /// <param name="colwidths">列宽集合</param>
        private void SettingRowColumnSize(Autodesk.AutoCAD.DatabaseServices.Table tb, int startRow, int startCol, int endRow, int endCol, Dictionary<int, double> rowHeights, Dictionary<int, double> colwidths)
        {
            //设置行高列宽
            //Excel行高转厘米约为 /28.5，列宽转厘米约为 /4.6（行高列宽改变该比例改变，此处只取默认行高列宽的比例，好像是边框固定占据一定的长宽，好像跟像素也有关系）
            if (setting.IsRowColumnSize)
            {
                for (int i = startRow; i <= endRow; i++)
                {
                    double rowHeight = rowHeights[i] == 0 ? table_RowHeight : rowHeights[i] / 28.5 * 100;
                    tb.Rows[i - startRow].Height = table_RowHeight;
                }

                for (int j = startCol; j <= endCol; j++)
                {
                    double colWidth = colwidths[j] == 0 ? table_RowHeight * 5 : colwidths[j] / 4.6 * 100;
                    tb.Columns[j - startCol].Width = colWidth;
                }
            }
            else
            {
                tb.SetRowHeight(table_RowHeight);
                tb.SetColumnWidth(table_RowHeight * 5);  //默认按照5倍行高来设置列宽
            }
        }

        /// <summary>
        /// 合并单元格
        /// </summary>
        /// <param name="tb">CAD表格</param>
        /// <param name="startRow">Excel数据起始行索引</param>
        /// <param name="startCol">Excel数据起始列索引</param>
        private void MergeCells(ISheet sheet, Autodesk.AutoCAD.DatabaseServices.Table tb, int startRow, int startCol)
        {
            if (setting.IsMergeCells)
            {
                foreach (var item in sheet.MergedRegions)
                {
                    int topRow = item.FirstRow - startRow;
                    int leftCol = item.FirstColumn - startCol;
                    int bottomRow = item.LastRow - startRow;
                    int rightCol = item.LastColumn - startCol;

                    //获取到的有效数据行列内的合并单元格范围超出表格范围，会报错“eInvalidInput”
                    if (tb.Rows.Count - 1 < topRow)
                        topRow = tb.Rows.Count - 1;
                    if (tb.Columns.Count - 1 < leftCol)
                        leftCol = tb.Columns.Count - 1;
                    if (tb.Rows.Count - 1 < bottomRow)
                    {
                        //若合并单元格有值，且合并单元格行范围大于表格行，则将表格行扩展，扩展行高等于最后一行。若有值，则值在第一个单元格。
                        IRow row = sheet.GetRow(item.FirstRow);
                        if (row != null)
                        {
                            ICell cell = row.GetCell(item.FirstColumn);
                            string value = GetCellValue(cell, out bool _);
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                int rows = tb.Rows.Count;

                                tb.SetSize(item.LastRow + 1, tb.Columns.Count);

                                for (int i = rows; i < item.LastRow + 1; i++)
                                {
                                    tb.Rows[i].Height = tb.Rows[rows - 1].Height;
                                }
                            }
                            else
                                bottomRow = tb.Rows.Count - 1;
                        }
                        else
                            bottomRow = tb.Rows.Count - 1;
                    }
                    if (tb.Columns.Count - 1 < rightCol)
                    {
                        //若合并单元格有值，且合并单元格列范围大于表格列，则将表格列扩展，扩展列宽等于最后一列。若有值，则值在第一个单元格。
                        IRow row = sheet.GetRow(item.FirstRow);
                        if (row != null)
                        {
                            ICell cell = row.GetCell(item.FirstColumn);
                            string value = GetCellValue(cell, out bool _);
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                int cols = tb.Columns.Count;

                                tb.SetSize(tb.Rows.Count, item.LastColumn + 1);

                                for (int j = cols; j < item.LastColumn + 1; j++)
                                {
                                    tb.Columns[j].Width = tb.Columns[cols - 1].Width;
                                }
                            }
                            else
                                rightCol = tb.Columns.Count - 1;
                        }
                        else
                            rightCol = tb.Columns.Count - 1;
                    }

                    //合并单元格
                    CellRange cr = CellRange.Create(tb, topRow, leftCol, bottomRow, rightCol);
                    tb.MergeCells(cr);
                }
            }
        }

        /// <summary>
        /// 绘制表格内容
        /// </summary>
        /// <param name="tb">CAD表格</param>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="startRow">Excel数据起始行索引</param>
        /// <param name="startCol">Excel数据起始列索引</param>
        /// <param name="endRow">Excel数据结束行索引</param>
        /// <param name="endCol">Excel数据结束列索引</param>
        /// <param name="progressBar">进度条</param>
        /// <param name="proportion">当前进度条占比</param>
        /// <param name="preProgress">前面进度条占比</param>
        private void DrawTableContent(Autodesk.AutoCAD.DatabaseServices.Table tb, ISheet sheet, List<ExcelData> excelDatas, int startRow, int startCol, int endRow, int endCol, HLProgressBar progressBar, double proportion, double preProgress)
        {
            double proportion2 = 0;

            //写入数据
            double increment = (proportion - proportion2) / ((endRow - startRow + 1) * (endCol - startCol + 1)); // 每笔数据进度条增量
            int count = 0;
            for (int i = startRow; i <= endRow; i++)
            {
                for (int j = startCol; j <= endCol; j++)
                {
                    ExcelData ed = excelDatas.FirstOrDefault(x => x.RowIndex == i && x.ColumnIndex == j);
                    if (ed != null)
                    {
                        string value = ed.FormatText;

                        if (setting.IsFormatSetting)
                        {
                            if (!string.IsNullOrWhiteSpace(value) || setting.IsEmptyCell)
                            {
                                DrawTableCell(tb.Cells[i - startRow, j - startCol], ed);

                                //表格单元内垂直水平边距。为了向下兼容，引用的14版dll，没有Cell.Top等属性或方法，只能用过期方法
                                tb.SetMargin(i - startRow, j - startCol, CellMargins.Top, 5);
                                tb.SetMargin(i - startRow, j - startCol, CellMargins.Left, 5);
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(value))
                        {
                            var cell = tb.Cells[i - startRow, j - startCol];
                            cell.TextString = value.ToString();
                            cell.TextHeight = font_Size;
                            cell.TextStyleId = textStyleId_Normal;
                            cell.Alignment = CellAlignment.MiddleCenter;

                            tb.SetMargin(i - startRow, j - startCol, CellMargins.Top, 5);
                            tb.SetMargin(i - startRow, j - startCol, CellMargins.Left, 5);
                        }
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
        private void DrawTableCell(Autodesk.AutoCAD.DatabaseServices.Cell cell, ExcelData excelData)
        {
            //字体大小。字体高度超出行高或长度超出列宽，会自动调整行高。 0说明解析Excel字号失败
            cell.TextHeight = excelData.FontSize == 0 ? font_Size : excelData.FontSize;

            //字体颜色
            if (excelData.FontColorRGB != null && (excelData.FontColorRGB[0] + excelData.FontColorRGB[1] + excelData.FontColorRGB[2]) != 0)   //0，0，0为默认黑色
            {
                cell.ContentColor = Autodesk.AutoCAD.Colors.Color.FromRgb(excelData.FontColorRGB[0], excelData.FontColorRGB[1], excelData.FontColorRGB[2]);
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
            if (excelData.BackgroundColorRGB != null && (excelData.BackgroundColorRGB[0] + excelData.BackgroundColorRGB[1] + excelData.BackgroundColorRGB[2]) != 255 * 3)   //0，0，0为默认白色（纯黑也是0，0，0比较奇怪），255，255，255纯白不绘制
            {
                cell.BackgroundColor = Autodesk.AutoCAD.Colors.Color.FromRgb(excelData.BackgroundColorRGB[0], excelData.BackgroundColorRGB[1], excelData.BackgroundColorRGB[2]);
            }

            //单元格对齐方式
            var alingH = excelData.HorizontalAlignment;
            var alingV = excelData.VerticalAlignment;

            if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.Left && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Top)
                cell.Alignment = CellAlignment.TopLeft;
            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.Center && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Top)
                cell.Alignment = CellAlignment.TopCenter;
            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.Right && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Top)
                cell.Alignment = CellAlignment.TopRight;
            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.General && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Top)
            {
                //水平对齐方式为：常规，即按照数据类型对齐，文本靠左，数字靠右
                if (excelData.ValueIsNumeric)
                    cell.Alignment = CellAlignment.TopRight;
                else
                    cell.Alignment = CellAlignment.TopLeft;
            }

            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.Left && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Center)
                cell.Alignment = CellAlignment.MiddleLeft;
            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.Center && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Center)
                cell.Alignment = CellAlignment.MiddleCenter;
            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.Right && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Center)
                cell.Alignment = CellAlignment.MiddleRight;
            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.General && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Center)
            {
                if (excelData.ValueIsNumeric)
                    cell.Alignment = CellAlignment.MiddleRight;
                else
                    cell.Alignment = CellAlignment.MiddleLeft;
            }

            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.Left && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Bottom)
                cell.Alignment = CellAlignment.BottomLeft;
            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.Center && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Bottom)
                cell.Alignment = CellAlignment.BottomCenter;
            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.Right && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Bottom)
                cell.Alignment = CellAlignment.BottomRight;
            else if (alingH == (int)NPOI.SS.UserModel.HorizontalAlignment.General && alingV == (int)NPOI.SS.UserModel.VerticalAlignment.Bottom)
            {
                if (excelData.ValueIsNumeric)
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
        /// 删除隐藏行列
        /// </summary>
        /// <param name="tb">CAD表格</param>
        /// <param name="startRow">Excel数据起始行索引</param>
        /// <param name="startCol">Excel数据起始列索引</param>
        /// <param name="hideRows">隐藏行集合</param>
        /// <param name="hideCols">隐藏列集合</param>
        private static void DeleteHiddenRowCol(Autodesk.AutoCAD.DatabaseServices.Table tb, int startRow, int startCol, Dictionary<int, bool> hideRows, Dictionary<int, bool> hideCols)
        {
            //如果存在隐藏行列，删除隐藏行列,要从后往前删除，否则会影响行列索引
            foreach (var row in hideRows.OrderByDescending(x => x.Key))
            {
                tb.DeleteRows(row.Key - startRow, 1);
            }

            foreach (var col in hideCols.OrderByDescending(x => x.Key))
            {
                tb.DeleteColumns(col.Key - startCol, 1);
            }
        }

        /// <summary>
        /// 插入表格图片
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="tb">CAD表格</param>
        private void InsertImage(ISheet sheet, Autodesk.AutoCAD.DatabaseServices.Table tb)
        {
            //todo Excel中图片有设置长宽比例，但目前获取到的是图片实际比例

            if (!setting.IsImage)
                return;

            List<ExcelImage> images = new List<ExcelImage>();
            Func<IPicture, ExcelImage> addImage = (pic) =>
            {
                ExcelImage image = new ExcelImage();
                image.StartRow = pic.ClientAnchor.Row1;
                image.EndRow = pic.ClientAnchor.Row2;
                image.StartColumn = pic.ClientAnchor.Col1;
                image.EndColumn = pic.ClientAnchor.Col2;
                image.ImageData = pic.PictureData.Data;
                image.Extension = pic.PictureData.PictureType.ToString();

                images.Add(image);
                return image;
            };

            if (sheet.DrawingPatriarch != null)
            {
                if (is97_2003)
                {
                    List<HSSFShape> shapes = (sheet.DrawingPatriarch as HSSFPatriarch).GetShapes();
                    if (shapes != null)
                    {
                        foreach (var item in shapes)
                        {
                            if (item is HSSFPicture pic)
                            {
                                ExcelImage image = addImage(pic);

                                var size = pic.GetImageDimension();
                                image.Width = size.Width;
                                image.Height = size.Height;
                            }
                        }
                    }
                }
                else
                {
                    List<XSSFShape> shapes = (sheet.DrawingPatriarch as XSSFDrawing).GetShapes();
                    if (shapes != null)
                    {
                        foreach (var item in shapes)
                        {
                            if (item is XSSFPicture pic)
                            {
                                ExcelImage image = addImage(pic);

                                var size = pic.GetImageDimension();
                                image.Width = size.Width;
                                image.Height = size.Height;
                            }
                        }
                    }
                }
            }

            double scale = 1; //图片缩放比例
            foreach (var item in images)
            {
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Temp";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string fileName = Path.Combine(path, imageNumber++.ToString() + "." + item.Extension);

                using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(item.ImageData, 0, item.ImageData.Length);
                }

                double x = 0, y = 0;
                for (int i = 0; i < item.StartRow; i++)
                {
                    if (i < tb.Rows.Count)
                        y += tb.Rows[i].Height;
                    else
                        y += sheet.DefaultRowHeight / sheet.DefaultRowHeightInPoints / 28.5 * 100;
                }
                for (int i = 0; i < item.StartColumn; i++)
                {
                    if (i < tb.Columns.Count)
                        x += tb.Columns[i].Width;
                    else
                        x += sheet.DefaultColumnWidth / 256 / 4.6 * 100;
                }

                //计算图片缩放比例
                double height = 0;
                for (int i = item.StartRow; i < item.EndRow; i++)
                {
                    if (i < tb.Rows.Count)
                        height += tb.Rows[i].Height;
                    else
                        height += sheet.DefaultRowHeight / sheet.DefaultRowHeightInPoints / 28.5 * 100;
                }

                scale = height / item.Height;

                CADTool.InsertImage(fileName, new Point3d(tb.Position.X + x, tb.Position.Y - y - item.Height * scale, 0), item.Width * scale, item.Height * scale);
            }
        }

    }
}
