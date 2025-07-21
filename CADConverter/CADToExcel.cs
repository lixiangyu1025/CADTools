using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using ICSharpCode.SharpZipLib;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.TextFormatting;
using CADApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADConverter
{
    /// <summary>
    /// CAD表格转Excel
    /// </summary>
    internal class CADToExcel
    {
        /// <summary>
        /// 文字选择过滤器
        /// </summary>
        private SelectionFilter filterText = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "TEXT,MTEXT") });

        /// <summary>
        /// 执行转换
        /// </summary>
        /// <param name="table">表格对象，为空时表示选择表格(由文字和线组成)</param>
        public void Run(Autodesk.AutoCAD.DatabaseServices.Table table = null)
        {
            try
            {
                bool result = false;
                if (table == null)
                {
                    //获取表格线、文字
                    if (!GetLinesAndTexts(out List<LineHelper> lines, out List<TextHelper> texts))
                    {
                        //按ESC等方式取消操作
                        return;
                    }


                    GetTableRowCol(lines, out List<LineHelper> horizontalLines, out List<LineHelper> verticalLines);

                    if (horizontalLines.Count() < 2 || verticalLines.Count() < 2)
                    {
                        throw new Exception("未找到有效表格！");
                    }

                    var vertexs = GetVertexs(horizontalLines, verticalLines);
                    var cells = GetCells(vertexs, texts, horizontalLines);

                    //写入Excel
                    result = WriteExcel(cells);
                }
                else
                {
                    var cells = GetCells(table);

                    result = WriteExcel(cells);
                }

                if (result)
                    CADApp.ShowAlertDialog("转换成功！");

                return;
            }
            catch (Exception ex)
            {
                CADApp.ShowAlertDialog(ex.Message);
            }
        }

        /// <summary>
        /// 获取线和文字
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="texts"></param>
        /// <returns></returns>
        private bool GetLinesAndTexts(out List<LineHelper> lines, out List<TextHelper> texts)
        {
            lines = new List<LineHelper>();
            texts = new List<TextHelper>();

            PromptSelectionOptions options = new PromptSelectionOptions();
            options.MessageForAdding = "\n请框选要转换的表格，按右键结束";
            string filterType = "LINE,LWPOLYLINE,MTEXT,TEXT";
            SelectionFilter filter = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, filterType) });
            PromptSelectionResult result = CADApp.DocumentManager.MdiActiveDocument.Editor.GetSelection(options, filter);
            if (result.Status == PromptStatus.OK)
            {
                //List<MText> mtexts = new List<MText>();
                //List<DBText> texts = new List<DBText>();

                using (var trans = CADApp.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject item in result.Value)
                    {
                        var entity = trans.GetObject(item.ObjectId, OpenMode.ForRead);
                        if (entity is Line line)
                        {
                            HandleLine(line, lines);
                        }
                        else if (entity is Polyline polyline)
                        {
                            HandlePolyline(polyline, lines);
                        }
                        else if (entity is MText mtext)
                        {
                            //mtexts.Add(mtext);
                            TextHelper textHelper = new TextHelper();
                            textHelper.Text = mtext.Contents.Replace(MText.ParagraphBreak, "\r\n");
                            textHelper.Position = mtext.Location;
                            textHelper.Rotation = mtext.Rotation;
                            texts.Add(textHelper);
                        }
                        else if (entity is DBText text)
                        {
                            //texts.Add(text);
                            TextHelper textHelper = new TextHelper();
                            textHelper.Text = text.TextString;
                            //优先使用对齐点，对齐点为空则用坐标点
                            if (text.HorizontalMode == TextHorizontalMode.TextLeft)
                                textHelper.Position = text.Position;
                            else
                                textHelper.Position = text.AlignmentPoint;
                            textHelper.Rotation = text.Rotation;
                            texts.Add(textHelper);
                        }
                    }
                }

                return true;
            }
            else
            {
                CADApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n未选择有效的表格！\n");
                return false;
            }
        }

        /// <summary>
        /// 处理直线
        /// </summary>
        /// <param name="line">直线</param>
        /// <param name="lines">处理完成的表格线集合</param>
        private void HandleLine(Line line, List<LineHelper> lines)
        {
            LineHelper lineHelper = new LineHelper();
            lineHelper.StartPoint = line.StartPoint;
            lineHelper.EndPoint = line.EndPoint;

            if (Math.Abs(line.EndPoint.Y - line.StartPoint.Y) < 1)
            {
                lineHelper.LineType = 0;
                lineHelper.Y = line.StartPoint.Y;
            }
            else if (Math.Abs(line.EndPoint.X - line.StartPoint.X) < 1)
            {
                lineHelper.LineType = 1;
                lineHelper.X = line.StartPoint.X;
            }
            else
                lineHelper.LineType = 2;

            //判断是否存在相同数据（有的地方可能存在多条相同的线）,避免重复添加
            foreach (var item in lines)
            {
                if (item.LineType == lineHelper.LineType && ((item.StartPoint.PointEquals(lineHelper.StartPoint) && item.EndPoint.PointEquals(lineHelper.EndPoint)) || (item.EndPoint.PointEquals(lineHelper.StartPoint) && item.StartPoint.PointEquals(lineHelper.EndPoint))))
                {
                    return;
                }
            }

            lines.Add(lineHelper);
        }

        /// <summary>
        /// 处理多段线
        /// </summary>
        /// <param name="polyline">多段线</param>
        /// <param name="lines">处理完成的表格线集合</param>    
        private void HandlePolyline(Polyline polyline, List<LineHelper> lines)
        {
            for (int i = 1; i <= polyline.NumberOfVertices; i++)
            {
                Point3d startPoint = new Point3d();
                Point3d endPoint = new Point3d();

                if (i < polyline.NumberOfVertices)
                {
                    startPoint = polyline.GetPoint3dAt(i - 1);
                    endPoint = polyline.GetPoint3dAt(i);
                }
                else if (i == polyline.NumberOfVertices && polyline.Closed)   //闭合多段线
                {
                    startPoint = polyline.GetPoint3dAt(i - 1);
                    endPoint = polyline.GetPoint3dAt(0);
                }

                LineHelper lineHelper = new LineHelper();
                lineHelper.StartPoint = startPoint;
                lineHelper.EndPoint = endPoint;

                if (Math.Abs(endPoint.Y - startPoint.Y) < 1)
                {
                    lineHelper.LineType = 0;
                    lineHelper.Y = startPoint.Y;
                }
                else if (Math.Abs(endPoint.X - startPoint.X) < 1)
                {
                    lineHelper.LineType = 1;
                    lineHelper.X = startPoint.X;
                }
                else
                    lineHelper.LineType = 2;

                //判断是否存在相同数据（有的地方可能存在多条相同的线）
                bool hasSameLine = false;
                foreach (var item in lines)
                {
                    if (item.LineType == lineHelper.LineType && ((item.StartPoint.PointEquals(lineHelper.StartPoint) && item.EndPoint.PointEquals(lineHelper.EndPoint)) || (item.EndPoint.PointEquals(lineHelper.StartPoint) && item.StartPoint.PointEquals(lineHelper.EndPoint))))
                    {
                        hasSameLine = true;
                        break;
                    }
                }

                if (!hasSameLine)
                    lines.Add(lineHelper);
            }
        }

        /// <summary>
        /// 获取CAD表格的行列数据
        /// </summary>
        /// <param name="lines">表格线集合</param>
        /// <param name="horizontalLines">行线集合</param>
        /// <param name="verticalLines">列线集合</param>
        private void GetTableRowCol(List<LineHelper> lines, out List<LineHelper> horizontalLines, out List<LineHelper> verticalLines)
        {
            horizontalLines = lines.Where(l => l.LineType == 0).ToList();
            verticalLines = lines.Where(l => l.LineType == 1).ToList();

            List<LineHelper> newHorizontalLines = new List<LineHelper>();
            List<LineHelper> newVerticalLines = new List<LineHelper>();

            if (horizontalLines.Count() > 0 && verticalLines.Count() > 0)
            {
                //将重叠的线合并
                List<LineHelper> tempHorizontalLines = horizontalLines.ToList();
                List<LineHelper> tempVerticalLines = verticalLines.ToList();

                foreach (var line in horizontalLines)
                {
                    List<LineHelper> overlapLines = new List<LineHelper>();   //与当前线重叠的线
                    GetOverlapLines(line, tempHorizontalLines, overlapLines);

                    if (overlapLines.Count > 1)   //存在Y轴相同，且有重叠的横线，用最小值和最大值组成新的横线
                    {
                        double minX1 = overlapLines.Min(l => l.StartPoint.X);
                        double minX2 = overlapLines.Min(l => l.EndPoint.X);
                        double maxX1 = overlapLines.Max(l => l.StartPoint.X);
                        double maxX2 = overlapLines.Max(l => l.EndPoint.X);

                        LineHelper lineHelper = new LineHelper();
                        lineHelper.StartPoint = new Point3d(Math.Min(minX1, minX2), line.StartPoint.Y, 0);
                        lineHelper.EndPoint = new Point3d(Math.Max(maxX1, maxX2), line.StartPoint.Y, 0);
                        lineHelper.LineType = line.LineType;
                        lineHelper.Y = line.Y;

                        newHorizontalLines.Add(lineHelper);
                    }
                    else if (overlapLines.Count() == 1)    //不存在Y轴相同，且有重叠的横线，用自己本身
                        newHorizontalLines.Add(line);

                    //等于0时，说明该线与其他线有重叠，直接合并到其他线

                    foreach (var l in overlapLines)
                    {
                        tempHorizontalLines.Remove(l);
                    }
                }

                foreach (var line in verticalLines)
                {
                    List<LineHelper> overlapLines = new List<LineHelper>();
                    GetOverlapLines(line, tempVerticalLines, overlapLines);

                    if (overlapLines.Count > 1)
                    {
                        double minY1 = overlapLines.Min(l => l.StartPoint.Y);
                        double minY2 = overlapLines.Min(l => l.EndPoint.Y);
                        double maxY1 = overlapLines.Max(l => l.StartPoint.Y);
                        double maxY2 = overlapLines.Max(l => l.EndPoint.Y);

                        LineHelper lineHelper = new LineHelper();
                        lineHelper.StartPoint = new Point3d(line.StartPoint.X, Math.Min(minY1, minY2), 0);
                        lineHelper.EndPoint = new Point3d(line.StartPoint.X, Math.Max(maxY1, maxY2), 0);
                        lineHelper.LineType = line.LineType;
                        lineHelper.X = line.X;

                        newVerticalLines.Add(lineHelper);
                    }
                    else if (overlapLines.Count() == 1)
                        newVerticalLines.Add(line);


                    foreach (var l in overlapLines)
                    {
                        tempVerticalLines.Remove(l);
                    }
                }

                //统计行列数
                var groupHorizontalLines = newHorizontalLines.GroupBy(l => l.Y, new DoubleEqualityComparer()).OrderByDescending(g => g.Key);   //行从上到下排列
                var groupVerticalLines = newVerticalLines.GroupBy(l => l.X, new DoubleEqualityComparer()).OrderBy(g => g.Key);    //列从左到右排列

                int rowIndex = 0;
                foreach (var group in groupHorizontalLines)
                {
                    foreach (var line in group)
                    {
                        line.RowIndex = rowIndex;
                    }

                    rowIndex++;
                }

                int columnIndex = 0;
                foreach (var group in groupVerticalLines)
                {
                    foreach (var line in group)
                    {
                        line.ColumnIndex = columnIndex;
                    }

                    columnIndex++;
                }

                horizontalLines = newHorizontalLines;
                verticalLines = newVerticalLines;
            }
            else
            {
                throw new Exception("未找到有效表格！");
            }
        }

        /// <summary>
        /// 获取与指定线段重叠的线段集合
        /// </summary>
        /// <param name="line">指定线段</param>
        /// <param name="lines">与指定线段在同一水平或垂直方向的线段集合</param>
        /// <param name="overlapLines">与指定线段重叠的线段(包含指定线段)</param>
        private void GetOverlapLines(LineHelper line, List<LineHelper> lines, List<LineHelper> overlapLines)
        {
            if (line.LineType == 0)
            {
                var sameYLines = lines.Where(l => Math.Abs(l.StartPoint.Y - line.StartPoint.Y) < 1).ToList();   //包含指定线在内的Y轴相同的线
                if (sameYLines.Count > 1)  //说明存在多条Y轴相同的线
                {
                    overlapLines.Add(line);
                    sameYLines.Remove(line);     //Y轴相同的线集合移除指定线

                    var sameYOtherLines = sameYLines.Where(l => IsLineOverlap(line, l)).ToList();   //不包含指定线在内的Y轴相同的线，且与指定线有重合部分

                    //用Y轴相同且与指定线有重合部分的线递归查找其他重叠线
                    foreach (var l in sameYOtherLines)
                    {
                        GetOverlapLines(l, sameYLines, overlapLines);
                    }
                }
                else if (sameYLines.Count() == 1)  //说明不存在Y轴相同的线，将自身加入重叠线集合
                    overlapLines.Add(line);
            }
            else if (line.LineType == 1)
            {
                var sameXLines = lines.Where(l => Math.Abs(l.StartPoint.X - line.StartPoint.X) < 1).ToList();
                if (sameXLines.Count > 1)  //说明存在多条X轴相同的线
                {
                    overlapLines.Add(line);
                    sameXLines.Remove(line);

                    var sameXOtherLines = sameXLines.Where(l => IsLineOverlap(line, l)).ToList();

                    foreach (var l in sameXOtherLines)
                    {
                        GetOverlapLines(l, sameXLines, overlapLines);
                    }
                }
                else if (sameXLines.Count == 1)
                    overlapLines.Add(line);
            }
        }

        /// <summary>
        /// 判断两条线段是否有重叠区域(前提是两条线必须X或Y轴相同)
        /// </summary>
        /// <param name="line1">线段1</param>
        /// <param name="line2">线段2</param>
        /// <returns>两条线段是否有重叠区域</returns>
        private bool IsLineOverlap(LineHelper line1, LineHelper line2)
        {
            if (line1.LineType != line2.LineType)
                return false;

            //if (line1.LineType == 0)  //横线
            //{
            //    if (line1.StartPoint.X >= line2.StartPoint.X && line1.StartPoint.X <= line2.EndPoint.X)   //line1起点在line2内部
            //        return true;
            //    else if (line1.StartPoint.X >= line2.EndPoint.X && line1.StartPoint.X <= line2.StartPoint.X)
            //        return true;
            //    else if (line1.EndPoint.X >= line2.StartPoint.X && line1.EndPoint.X <= line2.EndPoint.X)   //line1终点在line2内部
            //        return true;
            //    else if (line1.EndPoint.X >= line2.EndPoint.X && line1.EndPoint.X <= line2.StartPoint.X)
            //        return true;

            //    else if (line2.StartPoint.X >= line1.StartPoint.X && line2.StartPoint.X <= line1.EndPoint.X)  //line2起点在line1内部
            //        return true;
            //    else if (line2.StartPoint.X >= line1.EndPoint.X && line2.StartPoint.X <= line1.StartPoint.X)
            //        return true;
            //    else if (line2.EndPoint.X >= line1.StartPoint.X && line2.EndPoint.X <= line1.EndPoint.X)      //line2终点在line1内部
            //        return true;
            //    else if (line2.EndPoint.X >= line1.EndPoint.X && line2.EndPoint.X <= line1.StartPoint.X)
            //        return true;
            //}
            //else if (line1.LineType == 1)
            //{
            //    if (line1.StartPoint.Y >= line2.StartPoint.Y && line1.StartPoint.Y <= line2.EndPoint.Y)   //line1起点在line2内部
            //        return true;
            //    else if (line1.StartPoint.Y >= line2.EndPoint.Y && line1.StartPoint.Y <= line2.StartPoint.Y)
            //        return true;
            //    else if (line1.EndPoint.Y >= line2.StartPoint.Y && line1.EndPoint.Y <= line2.EndPoint.Y)   //line1终点在line2内部
            //        return true;
            //    else if (line1.EndPoint.Y >= line2.EndPoint.Y && line1.EndPoint.Y <= line2.StartPoint.Y)
            //        return true;

            //    else if (line2.StartPoint.Y >= line1.StartPoint.Y && line2.StartPoint.Y <= line1.EndPoint.Y)  //line2起点在line1内部
            //        return true;
            //    else if (line2.StartPoint.Y >= line1.EndPoint.Y && line2.StartPoint.Y <= line1.StartPoint.Y)
            //        return true;
            //    else if (line2.EndPoint.Y >= line1.StartPoint.Y && line2.EndPoint.Y <= line1.EndPoint.Y)      //line2终点在line1内部
            //        return true;
            //    else if (line2.EndPoint.Y >= line1.EndPoint.Y && line2.EndPoint.Y <= line1.StartPoint.Y)
            //        return true;
            //}

            //double acceptableDiff = 1;        //允许差值

            //if (line1.LineType == 0)  //横线
            //{
            //    if ((Math.Abs(line1.StartPoint.X - line2.StartPoint.X) < acceptableDiff || line1.StartPoint.X >= line2.StartPoint.X) && (Math.Abs(line1.StartPoint.X - line2.EndPoint.X) < acceptableDiff || line1.StartPoint.X <= line2.EndPoint.X))   //line1起点在line2内部
            //        return true;
            //    else if ((Math.Abs(line1.StartPoint.X - line2.EndPoint.X) < acceptableDiff || line1.StartPoint.X >= line2.EndPoint.X) && (Math.Abs(line1.StartPoint.X - line2.StartPoint.X) < acceptableDiff || line1.StartPoint.X <= line2.StartPoint.X))
            //        return true;
            //    else if ((Math.Abs(line1.EndPoint.X - line2.StartPoint.X) < acceptableDiff || line1.EndPoint.X >= line2.StartPoint.X) && (Math.Abs(line1.EndPoint.X - line2.EndPoint.X) < acceptableDiff || line1.EndPoint.X <= line2.EndPoint.X))   //line1终点在line2内部
            //        return true;
            //    else if ((Math.Abs(line1.EndPoint.X - line2.EndPoint.X) < acceptableDiff || line1.EndPoint.X >= line2.EndPoint.X) && (Math.Abs(line1.EndPoint.X - line2.StartPoint.X) < acceptableDiff || line1.EndPoint.X <= line2.StartPoint.X))
            //        return true;

            //    else if ((Math.Abs(line2.StartPoint.X - line1.StartPoint.X) < acceptableDiff || line2.StartPoint.X >= line1.StartPoint.X) && (Math.Abs(line2.StartPoint.X - line1.EndPoint.X) < acceptableDiff || line2.StartPoint.X <= line1.EndPoint.X))  //line2起点在line1内部
            //        return true;
            //    else if ((Math.Abs(line2.StartPoint.X - line1.EndPoint.X) < acceptableDiff || line2.StartPoint.X >= line1.EndPoint.X) && (Math.Abs(line2.StartPoint.X - line1.StartPoint.X) < acceptableDiff || line2.StartPoint.X <= line1.StartPoint.X))
            //        return true;
            //    else if ((Math.Abs(line2.EndPoint.X - line1.StartPoint.X) < acceptableDiff || line2.EndPoint.X >= line1.StartPoint.X) && (Math.Abs(line2.EndPoint.X - line1.EndPoint.X) < acceptableDiff || line2.EndPoint.X <= line1.EndPoint.X))    //line2终点在line1内部
            //        return true;
            //    else if ((Math.Abs(line2.EndPoint.X - line1.EndPoint.X) < acceptableDiff || line2.EndPoint.X >= line1.EndPoint.X) && (Math.Abs(line2.EndPoint.X - line1.StartPoint.X) < acceptableDiff || line2.EndPoint.X <= line1.StartPoint.X))
            //        return true;
            //}
            //else if (line1.LineType == 1)
            //{
            //    if ((Math.Abs(line1.StartPoint.Y - line2.StartPoint.Y) < acceptableDiff || line1.StartPoint.Y >= line2.StartPoint.Y) && (Math.Abs(line1.StartPoint.Y - line2.EndPoint.Y) < acceptableDiff || line1.StartPoint.Y <= line2.EndPoint.Y))  //line1起点在line2内部
            //        return true;
            //    else if ((Math.Abs(line1.StartPoint.Y - line2.EndPoint.Y) < acceptableDiff || line1.StartPoint.Y >= line2.EndPoint.Y) && (Math.Abs(line1.StartPoint.Y - line2.StartPoint.Y) < acceptableDiff || line1.StartPoint.Y <= line2.StartPoint.Y))
            //        return true;
            //    else if ((Math.Abs(line1.EndPoint.Y - line2.StartPoint.Y) < acceptableDiff || line1.EndPoint.Y >= line2.StartPoint.Y) && (Math.Abs(line1.EndPoint.Y - line2.EndPoint.Y) < acceptableDiff || line1.EndPoint.Y <= line2.EndPoint.Y)) //line1终点在line2内部
            //        return true;
            //    else if ((Math.Abs(line1.EndPoint.Y - line2.EndPoint.Y) < acceptableDiff || line1.EndPoint.Y >= line2.EndPoint.Y) && (Math.Abs(line1.EndPoint.Y - line2.StartPoint.Y) < acceptableDiff || line1.EndPoint.Y <= line2.StartPoint.Y))
            //        return true;

            //    else if ((Math.Abs(line2.StartPoint.Y - line1.StartPoint.Y) < acceptableDiff || line2.StartPoint.Y >= line1.StartPoint.Y) && (Math.Abs(line2.StartPoint.Y - line1.EndPoint.Y) < acceptableDiff || line2.StartPoint.Y <= line1.EndPoint.Y)) //line2起点在line1内部
            //        return true;
            //    else if ((Math.Abs(line2.StartPoint.Y - line1.EndPoint.Y) < acceptableDiff || line2.StartPoint.Y >= line1.EndPoint.Y) && (Math.Abs(line2.StartPoint.Y - line1.StartPoint.Y) < acceptableDiff || line2.StartPoint.Y <= line1.StartPoint.Y))
            //        return true;
            //    else if ((Math.Abs(line2.EndPoint.Y - line1.StartPoint.Y) < acceptableDiff || line2.EndPoint.Y >= line1.StartPoint.Y) && (Math.Abs(line2.EndPoint.Y - line1.EndPoint.Y) < acceptableDiff || line2.EndPoint.Y <= line1.EndPoint.Y))     //line2终点在line1内部
            //        return true;
            //    else if ((Math.Abs(line2.EndPoint.Y - line1.EndPoint.Y) < acceptableDiff || line2.EndPoint.Y >= line1.EndPoint.Y) && (Math.Abs(line2.EndPoint.Y - line1.StartPoint.Y) < acceptableDiff || line2.EndPoint.Y <= line1.StartPoint.Y))
            //        return true;
            //}

            //return false;

            return IsPointOnLine(line1.StartPoint, line2) || IsPointOnLine(line1.EndPoint, line2);

        }

        /// <summary>
        /// 判断点是否在线段上
        /// </summary>
        /// <param name="point">点</param>
        /// <param name="line">线段</param>
        /// <returns点是否在线段上></returns>
        private bool IsPointOnLine(Point3d point, LineHelper line)
        {
            //端点相接
            if (point.PointEquals(line.StartPoint) || point.PointEquals(line.EndPoint))
                return true;

            if (line.LineType == 0)  //横线
            {
                if (point.X >= line.StartPoint.X && point.X <= line.EndPoint.X)
                    return true;
                else if (point.X >= line.EndPoint.X && point.X <= line.StartPoint.X)
                    return true;
            }
            else if (line.LineType == 1) //竖线
            {
                if (point.Y >= line.StartPoint.Y && point.Y <= line.EndPoint.Y)
                    return true;
                else if (point.Y >= line.EndPoint.Y && point.Y <= line.StartPoint.Y)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获取表格顶点
        /// </summary>
        /// <param name="horizontalLines">行线集合</param>
        /// <param name="verticalLines">列线集合</param>
        /// <returns>表格顶点集合</returns>
        private List<Vertex> GetVertexs(IEnumerable<LineHelper> horizontalLines, IEnumerable<LineHelper> verticalLines)
        {
            List<Vertex> vertices = new List<Vertex>();

            horizontalLines = horizontalLines.OrderBy(l => l.RowIndex);
            verticalLines = verticalLines.OrderBy(l => l.ColumnIndex);

            //为避免精度问题，将线延长一点在判断相交。不能延太长，否则会将合并单元格断开的线也判断为相交
            List<double> rowSpacings = new List<double>();
            var ys = horizontalLines.Select(l => l.Y).GroupBy(l => l, new DoubleEqualityComparer()).ToList();
            for (int i = 0; i < ys.Count() - 1; i++)
            {
                rowSpacings.Add(Math.Abs(ys[i].Key - ys[i + 1].Key));
            }

            double extend = rowSpacings.Min() / 10;   //延长线段的长度为最小间距的1/10，以便于判断相交

            foreach (var verticalLine in verticalLines)
            {
                if (verticalLine.StartPoint.Y < verticalLine.EndPoint.Y)
                {
                    verticalLine.StartPoint = new Point3d(verticalLine.StartPoint.X, verticalLine.StartPoint.Y - extend, 0);
                    verticalLine.EndPoint = new Point3d(verticalLine.EndPoint.X, verticalLine.EndPoint.Y + extend, 0);
                }
                else
                {
                    verticalLine.StartPoint = new Point3d(verticalLine.StartPoint.X, verticalLine.StartPoint.Y + extend, 0);
                    verticalLine.EndPoint = new Point3d(verticalLine.EndPoint.X, verticalLine.EndPoint.Y - extend, 0);
                }
            }

            foreach (var horizontalLine in horizontalLines)
            {
                if (horizontalLine.StartPoint.X < horizontalLine.EndPoint.X)
                {
                    horizontalLine.StartPoint = new Point3d(horizontalLine.StartPoint.X - extend, horizontalLine.StartPoint.Y, 0);
                    horizontalLine.EndPoint = new Point3d(horizontalLine.EndPoint.X + extend, horizontalLine.EndPoint.Y, 0);
                }
                else
                {
                    horizontalLine.StartPoint = new Point3d(horizontalLine.StartPoint.X + extend, horizontalLine.StartPoint.Y, 0);
                    horizontalLine.EndPoint = new Point3d(horizontalLine.EndPoint.X - extend, horizontalLine.EndPoint.Y, 0);
                }

                foreach (var verticalLine in verticalLines)
                {
                    //不能放在此处延长，每次循环行都会延长一次
                    //if (verticalLine.StartPoint.Y < verticalLine.EndPoint.Y)
                    //{
                    //    verticalLine.StartPoint = new Point3d(verticalLine.StartPoint.X, verticalLine.StartPoint.Y - extend, 0);
                    //    verticalLine.EndPoint = new Point3d(verticalLine.EndPoint.X, verticalLine.EndPoint.Y + extend, 0);
                    //}
                    //else
                    //{
                    //    verticalLine.StartPoint = new Point3d(verticalLine.StartPoint.X, verticalLine.StartPoint.Y + extend, 0);
                    //    verticalLine.EndPoint = new Point3d(verticalLine.EndPoint.X, verticalLine.EndPoint.Y - extend, 0);
                    //}

                    //判断横线和竖线是否相交
                    if (GetLineSegmentIntersection(horizontalLine.StartPoint.X, horizontalLine.StartPoint.Y, horizontalLine.EndPoint.X, horizontalLine.EndPoint.Y, verticalLine.StartPoint.X, verticalLine.StartPoint.Y, verticalLine.EndPoint.X, verticalLine.EndPoint.Y, out double x, out double y))
                    {
                        Vertex vertex = new Vertex();
                        vertex.RowIndex = horizontalLine.RowIndex;
                        vertex.ColumnIndex = verticalLine.ColumnIndex;
                        vertex.Point = new Point3d(x, y, 0);

                        vertices.Add(vertex);
                    }
                }
            }

            return vertices;
        }

        /// <summary>
        /// 获取表格单元格
        /// </summary>
        /// <param name="vertices">顶点集合</param>
        /// <param name="texts">文本集合</param>
        /// <param name="horizontalLines">行线集合</param>
        /// <returns>单元格集合</returns>
        private List<CellHelper> GetCells(List<Vertex> vertices, List<TextHelper> texts, List<LineHelper> horizontalLines)
        {
            List<CellHelper> cells = new List<CellHelper>();
            var rowsVertices = vertices.GroupBy(v => v.RowIndex).Select(v => v.OrderBy(o => o.ColumnIndex).ToList()).ToList();   //将顶点按照行号分组，每行从左到右排序
            for (int i = 0; i < rowsVertices.Count - 1; i++)
            {
                var currRowVertices = rowsVertices[i];   //找到当前行的所有顶点

                for (int j = 0; j < currRowVertices.Count - 1; j++)
                {
                    int cellCount = cells.Count;

                    var currVertex1 = currRowVertices[j];         //当前行的第一个顶点

                    int currColumnCount = 0;         //当前行第二个顶点与第一个顶点之间跨越的列数
                    bool isCurrConnected = false;    //当前行第一个顶点与第二个顶点之间是否连通
                    while (cells.Count <= cellCount && j + ++currColumnCount < currRowVertices.Count)
                    {
                        var currVertex2 = currRowVertices[j + currColumnCount];     //当前行的第二个顶点。由于存在合并单元格，所以可能跨越多个列和行组成单元格

                        //检测当前行的第一个顶点和第二个之间是否连通，若不连通则说明是纵向合并单元格，且起点不在当前行
                        Point3d currCenterPoint = new Point3d((currVertex1.Point.X + currVertex2.Point.X) / 2, (currVertex1.Point.Y + currVertex2.Point.Y) / 2, 0);  //当前行两个顶点的中心点
                        foreach (var line in horizontalLines.Where(l => l.RowIndex == currVertex1.RowIndex))
                        {
                            if (IsPointOnLine(currCenterPoint, line))
                            {
                                isCurrConnected = true;
                                break;
                            }
                        }

                        //若当前行不连通则跳到下一组顶点
                        if (!isCurrConnected)
                        {
                            j = j + currColumnCount - 1;   //由于For循环还会将j+1，所以这里需要减1                               
                            break;
                        }

                        int nextRowIndex = i + 1;
                        while (cells.Count <= cellCount && nextRowIndex < rowsVertices.Count)
                        {
                            var nextRowVertices = rowsVertices[nextRowIndex];  //找到下一行的所有顶点
                            var nextVertex1 = nextRowVertices.FirstOrDefault(v => v.ColumnIndex == currVertex1.ColumnIndex);   //下一行与当前行第一个顶点对应的顶点
                            var nextVertex2 = nextRowVertices.FirstOrDefault(v => v.ColumnIndex == currVertex2.ColumnIndex);   //下一行与当前行第二个顶点对应的顶点

                            //不为空说明此两个顶点与上一行两个顶点组成一个单元格
                            if (nextVertex1 != null && nextVertex2 != null)
                            {
                                //检测下一行的第一个顶点和第二个顶点之间是否连通，若不连通则说明是横向合并单元格，且起点不在下一行
                                bool isNextConnected = false;
                                Point3d nextCenterPoint = new Point3d((nextVertex1.Point.X + nextVertex2.Point.X) / 2, (nextVertex1.Point.Y + nextVertex2.Point.Y) / 2, 0);  //下一行两个顶点的中心点
                                foreach (var line in horizontalLines.Where(l => l.RowIndex == nextVertex1.RowIndex))
                                {
                                    if (IsPointOnLine(nextCenterPoint, line))
                                    {
                                        isNextConnected = true;
                                        break;
                                    }
                                }

                                //若下一行两个顶点之间连通，则说明不是合并单元格，或横向合并单元格。否则为纵向合并单元格，且终点不在下一行。
                                if (!isNextConnected)
                                {
                                    nextRowIndex++;
                                    continue;   //下一行不连通，则再往下找一行判断
                                }

                                CellHelper cell = new CellHelper();
                                cell.RowIndex = currVertex1.RowIndex;
                                cell.ColumnIndex = currVertex1.ColumnIndex;
                                cell.MergeRowCount = nextRowIndex - i - 1;
                                cell.MergeColumnCount = currVertex2.ColumnIndex - currVertex1.ColumnIndex - 1;
                                cell.StartPoint = currVertex1.Point;
                                cell.EndPoint = nextVertex2.Point;
                                GetCellValue(cell, texts);
                                cells.Add(cell);

                                break;
                            }
                            else
                            {
                                //下一行未找到与当前行两个顶点对应的顶点。则判断下一行是否连通，是则说明当前行是横向合并单元格
                                bool isNextConnected = false;
                                Point3d nextCenterPoint = new Point3d(currCenterPoint.X, nextRowVertices[0].Point.Y, 0);
                                foreach (var line in horizontalLines.Where(l => l.RowIndex == nextRowVertices[0].RowIndex))
                                {
                                    if (IsPointOnLine(nextCenterPoint, line))
                                    {
                                        isNextConnected = true;
                                        break;
                                    }
                                }

                                //下一行连通，当前行为横向合并单元格，跳出与下一行的判断，当前行顶点后移一位
                                if (isNextConnected)
                                    break;
                            }

                            nextRowIndex++;
                            continue;
                        }

                        //可能由于未封闭等原因导致该组顶点未组成单元格，则继续下一组顶点。若能组成单元格，则该单元格内的顶点将被忽略，j直接指向单元格的第二个顶点所在列。
                        if (cells.Count > cellCount)
                        {
                            j = j + currColumnCount - 1;   //由于For循环还会将j+1，所以这里需要减1
                            break;
                        }
                    }

                }
            }

            return cells;
        }

        /// <summary>
        /// 获取单元格的值
        /// </summary>
        /// <param name="cell">单元格</param>
        /// <param name="texts">文本集合</param>
        private void GetCellValue(CellHelper cell, List<TextHelper> texts)
        {
            var datas = texts.Where(t => t.Position.X >= cell.StartPoint.X && t.Position.X <= cell.EndPoint.X && t.Position.Y <= cell.StartPoint.Y && t.Position.Y >= cell.EndPoint.Y).ToList();

            if (datas.Count > 0)
            {
                cell.Value = string.Join("\r\n", datas.OrderByDescending(t => t.Position.Y).ThenBy(t => t.Position.X).Select(t => t.Text));
                cell.Rotation = datas[0].Rotation;

                foreach (var data in datas)
                {
                    texts.Remove(data);
                }
            }
        }

        private void GetCellValue(CellHelper cell, Transaction trans)
        {
            Point3dCollection points = new Point3dCollection();
            points.Add(cell.StartPoint);  // 左上
            points.Add(new Point3d(cell.StartPoint.X, cell.EndPoint.Y, 0));  // 左下
            points.Add(cell.EndPoint);  // 右下 
            points.Add(new Point3d(cell.EndPoint.X, cell.StartPoint.Y, 0));  // 右上

            //PromptSelectionResult psr = CADApp.DocumentManager.MdiActiveDocument.Editor.SelectCrossingPolygon(points, filterText);  //与边框相交即可
            PromptSelectionResult psr = CADApp.DocumentManager.MdiActiveDocument.Editor.SelectWindowPolygon(points, filterText);      //完全框入
            if (psr.Status == PromptStatus.OK)
            {
                List<TextHelper> textHelpers = new List<TextHelper>();
                foreach (SelectedObject item in psr.Value)
                {
                    var entity = trans.GetObject(item.ObjectId, OpenMode.ForRead);
                    if (entity is MText mtext)
                    {
                        textHelpers.Add(new TextHelper { Position = mtext.Location, Text = mtext.Contents });
                    }
                    else if (entity is DBText text)
                    {
                        textHelpers.Add(new TextHelper { Position = text.Position, Text = text.TextString });
                    }
                }

                textHelpers.OrderByDescending(t => t.Position.Y).ThenBy(t => t.Position.X);  //从上到下，从左到右排列
                cell.Value = string.Join("\r\n", textHelpers.OrderByDescending(t => t.Position.Y).ThenBy(t => t.Position.X).Select(t => t.Text));
            }
        }

        private List<CellHelper> GetCells(Autodesk.AutoCAD.DatabaseServices.Table table)
        {
            List<CellHelper> cells = new List<CellHelper>();
            foreach (var cellRef in table.Cells)
            {
                Cell cell = table.Cells[cellRef.Row, cellRef.Column];
                //if (!string.IsNullOrWhiteSpace(cell.TextString))   //空单元格也导出
                {
                    CellHelper cellHelper = new CellHelper();
                    cellHelper.RowIndex = cellRef.Row;
                    cellHelper.ColumnIndex = cellRef.Column;
                    cellHelper.Value = cell.TextString;

                    if (cell.IsMerged.HasValue && cell.IsMerged.Value)
                    {
                        CellRange range = cell.GetMergeRange();
                        cellHelper.MergeRowCount = range.BottomRow - range.TopRow;
                        cellHelper.MergeColumnCount = range.RightColumn - range.LeftColumn;
                    }

                    cells.Add(cellHelper);
                }
            }

            return cells;
        }

        /// <summary>
        /// 写入Excel
        /// </summary>
        /// <param name="cells">单元格集合</param>
        private bool WriteExcel(List<CellHelper> cells)
        {
            if (cells.Count == 0)
                return false;

            string fileName = string.Empty;
            System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog();
            fileDialog.Title = "请选择Excel文件保存路径";
            fileDialog.DefaultExt = ".xlsx";
            fileDialog.AddExtension = true;
            fileDialog.Filter = "Excel文件|*.xlsx";
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fileName = fileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            //导出Excel
            XSSFWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet();
            //sheet.DefaultRowHeight = 30 * 20;     //加了这两句会导致生成的Excel打开时有问题，提示“部分内容有问题...尝试恢复...”
            //sheet.DefaultColumnWidth = 100 * 256;

            sheet.Autobreaks = true;
            //sheet.AutoSizeColumn(3);

            int rows = cells.Max(c => c.RowIndex) + 1;
            int cols = cells.Max(c => c.ColumnIndex + c.MergeColumnCount) + 1;

            ICellStyle cellStyle = workbook.CreateCellStyle();
            cellStyle.Alignment = HorizontalAlignment.Center;
            cellStyle.VerticalAlignment = VerticalAlignment.Center;
            cellStyle.WrapText = true;
            cellStyle.BorderBottom = BorderStyle.Thin;
            cellStyle.BorderLeft = BorderStyle.Thin;
            cellStyle.BorderRight = BorderStyle.Thin;
            cellStyle.BorderTop = BorderStyle.Thin;

            for (int i = 0; i < rows; i++)
            {
                IRow row = sheet.CreateRow(i);
                row.Height = 30 * 20;

                for (int j = 0; j < cols; j++)
                {
                    ICell cell = row.CreateCell(j);
                    sheet.SetColumnWidth(j, 10 * 256);

                    var cellData = cells.FirstOrDefault(c => c.RowIndex == i && c.ColumnIndex == j);
                    if (cellData != null)
                    {
                        cell.SetCellValue(cellData.Value);

                        if (cellData.Rotation != 0)
                        {
                            ICellStyle newStyle = workbook.CreateCellStyle();
                            newStyle.CloneStyleFrom(cellStyle);
                            newStyle.Rotation = (short)(cellData.Rotation * 180 / Math.PI);
                            cell.CellStyle = newStyle;
                        }
                        else
                            cell.CellStyle = cellStyle;
                    }
                    else
                        cell.CellStyle = cellStyle;
                }
            }

            //合并单元格
            foreach (var item in cells.Where(c => c.IsMerged))
            {
                //try
                //{
                sheet.AddMergedRegion(new CellRangeAddress(item.RowIndex, item.RowIndex + item.MergeRowCount, item.ColumnIndex, item.ColumnIndex + item.MergeColumnCount));
                //}
                //catch (Exception ex)
                //{
                //}
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }

            return true;
        }

        /// <summary>
        /// 计算两条线段交点
        /// </summary>
        /// <param name="startX1">线1起点X</param>
        /// <param name="startY1">线1起点Y</param>
        /// <param name="endX1">线1终点X</param>
        /// <param name="endY1">线1终点Y</param>
        /// <param name="startX2">线2起点X</param>
        /// <param name="startY2">线2起点Y</param>
        /// <param name="endX2">线2终点X</param>
        /// <param name="endY2">线2终点Y</param>
        /// <param name="intersectX">交点X</param>
        /// <param name="intersectY">交点Y</param>
        /// <returns>是否相交</returns>
        public static bool GetLineSegmentIntersection(double startX1, double startY1, double endX1, double endY1, double startX2, double startY2, double endX2, double endY2, out double intersectX, out double intersectY)
        {
            intersectX = 0;
            intersectY = 0;

            //排斥试验，判断p1p2在q1q2为对角线的矩形区之外
            if (Math.Max(startX1, endX1) < Math.Min(startX2, endX2))
            {//P1P2中最大的X比Q1Q2中的最小X还要小，说明P1P2在Q1Q2的最左点的左侧，不可能相交。
                return false;
            }

            if (Math.Min(startX1, endX1) > Math.Max(startX2, endX2))
            {//P1P2中最小的X比Q1Q2中的最大X还要大，说明P1P2在Q1Q2的最右点的右侧，不可能相交。
                return false;
            }

            if (Math.Max(startY1, endY1) < Math.Min(startY2, endY2))
            {//P1P2中最大的Y比Q1Q2中的最小Y还要小，说明P1P2在Q1Q2的最低点的下方，不可能相交。
                return false;
            }

            if (Math.Min(startY1, endY1) > Math.Max(startY2, endY2))
            {//P1P2中最小的Y比Q1Q2中的最大Y还要大，说明P1P2在Q1Q2的最高点的上方，不可能相交。
                return false;
            }

            //跨立试验
            var crossP1P2Q1 = VectorCross(startX1, startY1, endX1, endY1, startX2, startY2);
            var crossP1Q2P2 = VectorCross(startX1, startY1, endX2, endY2, endX1, endY1);
            var crossQ1Q2P1 = VectorCross(startX2, startY2, endX2, endY2, startX1, startY1);
            var crossQ1P2Q2 = VectorCross(startX2, startY2, endX1, endY1, endX2, endY2);

            bool isIntersect = (crossP1P2Q1 * crossP1Q2P2 >= 0) && (crossQ1Q2P1 * crossQ1P2Q2 >= 0);

            //求交点
            if (isIntersect)
            {
                double d = Math.Round((endX2 - startX2) * (endY1 - startY1) - (endX1 - startX1) * (endY2 - startY2), 1);
                if (d == 0)
                    return false;

                double d1 = ((startY2 - startY1) * (endX2 - startX2) - (startX2 - startX1) * (endY2 - startY2)) / d;
                //double d2 = ((p1.X - q1.X) * (p2.Y - p1.Y) - (p1.Y - q1.Y) * (p2.X - p1.X)) / d;

                intersectX = startX1 + d1 * (endX1 - startX1);
                intersectY = startY1 + d1 * (endY1 - startY1);
            }

            return isIntersect;
        }

        /// <summary>
        /// 向量叉乘
        /// </summary>
        /// <param name="startX">线起点X</param>
        /// <param name="startY">线起点Y</param>
        /// <param name="endX">线终点X</param>
        /// <param name="endY">线终点Y</param>
        /// <param name="outX">线外点X</param> 
        /// <param name="outY">线外点Y</param>
        /// <returns></returns>
        private static double VectorCross(double startX, double startY, double endX, double endY, double outX, double outY)
        {
            double x1 = endX - startX;
            double y1 = endY - startY;
            double x2 = outX - startX;
            double y2 = outY - startY;

            return x1 * y2 - x2 * y1;
        }
    }


    public class DoubleEqualityComparer : IEqualityComparer<double>
    {
        public bool Equals(double x, double y)
        {
            if (Math.Abs(x - y) < 1)
                return true;
            else
                return false;
        }

        public int GetHashCode(double obj)
        {
            return 1;
        }
    }

    public static class ExtendMethod
    {
        public static bool PointEquals(this Point3d point, Point3d pt)
        {
            double diffX = point.X - pt.X;
            double diffY = point.Y - pt.Y;

            //坐标差值小于1，则认为相等
            if (Math.Abs(diffX) < 1 && Math.Abs(diffY) < 1)
                return true;
            else
                return false;
        }
    }
}
