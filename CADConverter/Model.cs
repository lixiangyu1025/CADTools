using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADConverter
{
    /// <summary>
    /// Excel转CAD转换项设置
    /// </summary>
    public class ConvertSetting
    {
        /// <summary>
        /// 是否合并单元格
        /// </summary>
        public bool IsMergeCells { get; set; }

        /// <summary>
        /// 是否等比例转换行高和列宽
        /// </summary>
        public bool IsRowColumnSize { get; set; }

        /// <summary>
        /// 是否等比例转换文字大小
        /// </summary>
        public bool IsFontSize { get; set; }

        /// <summary>
        /// 是否转换文字颜色
        /// </summary>
        public bool IsFontColor { get; set; }

        /// <summary>
        /// 是否转换文字样式(加粗，斜体)
        /// </summary>
        public bool IsFontStyle { get; set; }

        /// <summary>
        /// 是否转换文字对齐方式
        /// </summary>
        public bool IsAlign { get; set; }

        /// <summary>
        /// 是否转换数字显示格式
        /// </summary>
        public bool IsFormat { get; set; }

        /// <summary>
        /// 是否转换填充颜色
        /// </summary>
        public bool IsFill { get; set; }

        /// <summary>
        /// 是否转换空白单元格样式
        /// </summary>
        public bool IsEmptyCell { get; set; }

        /// <summary>
        /// 爆破表格(不算作转换参数<see cref="IsFormatSetting"/>)
        /// </summary>
        public bool IsExplode { get; set; }

        /// <summary>
        /// 是否转换图片
        /// </summary>
        public bool IsImage { get; set; }

        /// <summary>
        /// 是否有设置格式转换参数
        /// </summary>
        public bool IsFormatSetting
        {
            get
            {
                return IsMergeCells || IsRowColumnSize || IsFontSize || IsFontColor || IsFontStyle || IsAlign || IsFormat || IsFill || IsEmptyCell;
            }
        }
    }

    /// <summary>
    /// Excel数据
    /// </summary>
    public class ExcelData
    {
        /// <summary>
        /// 行索引
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 列索引
        /// </summary>
        public int ColumnIndex { get; set; }

        /// <summary>
        /// 文字大小
        /// </summary>
        public double FontSize { get; set; }

        /// <summary>
        /// 文字颜色
        /// </summary>
        public int FontColor { get; set; }

        /// <summary>
        /// 文字颜色
        /// </summary>
        public byte[] FontColorRGB { get; set; }

        /// <summary>
        /// 文字加粗
        /// </summary>
        public bool FontBold { get; set; }

        /// <summary>
        /// 文字斜体
        /// </summary>
        public bool FontItalic { get; set; }

        /// <summary>
        /// 单元格填充色
        /// </summary>
        public int BackgroundColor { get; set; }

        /// <summary>
        /// 单元格填充色
        /// </summary>
        public byte[] BackgroundColorRGB { get; set; }

        /// <summary>
        /// 单元格对齐方式-水平
        /// </summary>
        public int HorizontalAlignment { get; set; }

        /// <summary>
        /// 单元格对齐方式-垂直
        /// </summary>
        public int VerticalAlignment { get; set; }

        /// <summary>
        /// 格式化显示文本/单元格值
        /// </summary>
        public string FormatText { get; set; }

        /// <summary>
        /// 值是否为数字
        /// </summary>
        public bool ValueIsNumeric { get; set; }
    }

    /// <summary>
    /// Excel图片
    /// </summary>
    public class ExcelImage
    {
        public int StartRow { get; set; }
        public int StartColumn { get; set; }
        public int EndRow { get; set; }
        public int EndColumn { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] ImageData { get; set; }

        /// <summary>
        /// 图片文件扩展名
        /// </summary>
        public string Extension { get; set; }
    }

    /// <summary>
    /// CAD线
    /// </summary>
    public class LineHelper
    {
        /// <summary>
        /// 线类型。0-横线，1-竖线，2-斜线
        /// </summary>
        public int LineType { get; set; }

        /// <summary>
        /// 线的起点
        /// </summary>
        public Point3d StartPoint { get; set; }

        /// <summary>
        /// 线的终点
        /// </summary>
        public Point3d EndPoint { get; set; }

        /// <summary>
        /// 线的X坐标(适用于竖线)
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// 线的Y坐标(适用于横线)
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// 行索引
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 列索引
        /// </summary>
        public int ColumnIndex { get; set; }
    }

    /// <summary>
    /// CAD单元格
    /// </summary>
    public class CellHelper
    {
        /// <summary>
        /// 起始行索引(从0开始)
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 起始列索引
        /// </summary>
        public int ColumnIndex { get; set; }
        public string Value { get; set; }

        /// <summary>
        /// 合并行数
        /// </summary>
        public int MergeRowCount { get; set; }

        /// <summary>
        /// 合并列数    
        /// </summary>
        public int MergeColumnCount { get; set; }

        /// <summary>
        /// 是否合并单元格
        /// </summary>
        public bool IsMerged { get => MergeRowCount > 1 || MergeColumnCount > 1; }

        /// <summary>
        /// 旋转角度
        /// </summary>
        public double Rotation { get; set; }

        /// <summary>
        /// 左上角坐标
        /// </summary>
        public Point3d StartPoint { get; set; }

        /// <summary>
        /// 右下角坐标
        /// </summary>
        public Point3d EndPoint { get; set; }
    }

    /// <summary>
    /// CAD表格顶点
    /// </summary>
    public class Vertex
    {
        /// <summary>
        /// 所在行索引
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 所在列索引
        /// </summary>
        public int ColumnIndex { get; set; }

        /// <summary>
        /// 顶点坐标    
        /// </summary>
        public Point3d Point { get; set; }
    }

    public class TextHelper
    {
        public Point3d Position { get; set; }
        public string Text { get; set; }
        public double Rotation { get; set; }
    }

    public class DataHelper
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public string Value { get; set; }
    }

}
