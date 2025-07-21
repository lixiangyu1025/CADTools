using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace CADConverter
{
    /// <summary>
    /// 基础工具：操作CAD软件、实体等
    /// </summary>
    public partial class CADTool
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        /// <summary>
        /// 设置CAD文档焦点
        /// </summary>
        public static void SetFocus()
        {
            //将焦点移到CAD窗口。试了用WinAPI(SetFocus)和CADAPI(Focus)设置窗口焦点，没起作用，最后模拟鼠标中键点击实现
            //var cadWindow = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Window;
            //var cadWindow = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow;
            //var ss = SetFocus(cadWindow.Handle);
            //var aa = SetFocus(Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Window.Handle);
            //cadWindow.Focus();

            var cadWindow = Application.DocumentManager.MdiActiveDocument.Window;
            uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
            uint MOUSEEVENTF_MIDDLEUP = 0x0040;
            mouse_event(MOUSEEVENTF_MIDDLEDOWN, (uint)(cadWindow.DeviceIndependentLocation.X + cadWindow.DeviceIndependentSize.Width / 2), (uint)(cadWindow.DeviceIndependentLocation.Y + cadWindow.DeviceIndependentSize.Height / 2), 0, 0);
            mouse_event(MOUSEEVENTF_MIDDLEUP, (uint)(cadWindow.DeviceIndependentLocation.X + cadWindow.DeviceIndependentSize.Width / 2), (uint)(cadWindow.DeviceIndependentLocation.Y + cadWindow.DeviceIndependentSize.Height / 2), 0, 0);
        }

        /// <summary>
        /// 爆破实体
        /// </summary>
        /// <param name="id">实体Id</param>
        /// <returns>爆破后产生的实体Id集合</returns>
        public static ObjectIdCollection Explode(ObjectId id)
        {
            ObjectIdCollection ids = new ObjectIdCollection();

            using (DocumentLock doclock = Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Database db = HostApplicationServices.WorkingDatabase;

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    Entity entity = (Entity)trans.GetObject(id, OpenMode.ForWrite);

                    //爆破后产生的实体集合
                    DBObjectCollection objs = new DBObjectCollection();
                    entity.Explode(objs);

                    //删除源实体，否则源实体和爆破后实体重叠
                    entity.Erase();

                    //将爆破后的零散实体加入模型空间，图层还是源实体的图层
                    foreach (DBObject item in objs)
                    {
                        if (item is Entity)
                        {
                            btr.AppendEntity((Entity)item);
                            trans.AddNewlyCreatedDBObject(item, true);

                            ids.Add(item.ObjectId);
                        }
                    }

                    trans.Commit();
                }
            }

            return ids;
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="msg">消息</param>
        /// <param name="isShowDialog">是否以模式对话框显示消息：true，弹出模式对话框显示消息；false，在命令栏显示消息</param>
        public static void ShowMessage(string msg, bool isShowDialog = true)
        {
            if (isShowDialog)
                Application.ShowAlertDialog(msg);
            else
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(msg + "\n");  // + "\n"将消息从命令栏发送出去
        }

        /// <summary>
        /// 清空当前模型空间所有实体
        /// </summary>
        public static void Clear()
        {
            using (DocumentLock doclock = Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                Database db = Application.DocumentManager.MdiActiveDocument.Database;

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    foreach (ObjectId entityId in btr)
                    {
                        Entity entity = trans.GetObject(entityId, OpenMode.ForWrite) as Entity;
                        entity?.Erase();
                    }

                    trans.Commit();
                }
            }
        }

        /// <summary>
        /// 插入光栅图像(不支持的图片格式插入失败，返回ObjectId.Null，目前已知不支持emf格式)
        /// </summary>
        /// <param name="fileName">图像文件名</param>
        /// <param name="position">插入坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="layerId">图层</param>
        /// <returns>插入的光栅图像(<see cref="RasterImage"/>)Id</returns>
        public static ObjectId InsertImage(string fileName, Point3d position, double width = 1, double height = 1, ObjectId layerId = default)
        {
            using (DocumentLock docLock = Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                using (Database db = Application.DocumentManager.MdiActiveDocument.Database)
                {
                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        ObjectId objectId = RasterImageDef.GetImageDictionary(db);
                        if (objectId == ObjectId.Null)
                            objectId = RasterImageDef.CreateImageDictionary(db);

                        DBDictionary dBDictionary = trans.GetObject(objectId, OpenMode.ForWrite) as DBDictionary;
                        RasterImageDef rasterImageDef = new RasterImageDef();

                        //目前发现.emf图片无法插入
                        try
                        {
                            rasterImageDef.SourceFileName = fileName;
                            rasterImageDef.Load();
                        }
                        catch
                        {
                            return ObjectId.Null;
                        }

                        string name = RasterImageDef.SuggestName(dBDictionary, rasterImageDef.SourceFileName);
                        ObjectId id = dBDictionary.SetAt(name, rasterImageDef);
                        trans.AddNewlyCreatedDBObject(rasterImageDef, true);

                        RasterImage image = new RasterImage();
                        image.ImageDefId = rasterImageDef.ObjectId;
                        image.Orientation = new CoordinateSystem3d(position, new Vector3d(width, 0, 0), new Vector3d(0, height, 0));  // new CoordinateSystem3d(position, Vector3d.XAxis, Vector3d.YAxis);

                        if (layerId != default)
                            image.LayerId = layerId;

                        btr.AppendEntity(image);
                        trans.AddNewlyCreatedDBObject(image, true);

                        trans.Commit();

                        return image.ObjectId;
                    }
                }
            }
        }

        /// <summary>
        /// 清空光栅图像定义及对应的外部文件参照
        /// </summary>
        public static void ClearImageDefAndXRef()
        {
            using (DocumentLock doclock = Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                using (Database db = Application.DocumentManager.MdiActiveDocument.Database)
                {
                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        List<RasterImageDef> imageDefs = new List<RasterImageDef>();
                        ObjectId dicId = RasterImageDef.GetImageDictionary(db);
                        if (dicId != ObjectId.Null)
                        {
                            DBDictionary dBDictionary = trans.GetObject(RasterImageDef.GetImageDictionary(db), OpenMode.ForWrite) as DBDictionary;
                            foreach (var item in dBDictionary)
                            {
                                RasterImageDef imageDef = trans.GetObject(item.Value, OpenMode.ForWrite) as RasterImageDef;
                                imageDefs.Add(imageDef);
                            }

                            //移除字典，删除图像定义
                            foreach (var item in imageDefs)
                            {
                                dBDictionary.Remove(item.ObjectId);
                                item.Erase();
                            }
                        }

                        trans.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// 缩放-按照所有实体范围
        /// </summary>
        public static void ZoomAll()
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute("ZOOM E\n", false, false, false);
        }

        /// <summary>
        /// 创建单行文本
        /// </summary>
        /// <param name="position">位置坐标</param>
        /// <param name="text">文本内容</param>
        /// <param name="textheight">高度</param>
        /// <param name="widthFactor">宽度因子</param>
        /// <param name="rotation">旋转角度</param>
        /// <param name="color">Windows颜色</param>
        /// <param name="lineWeight">线宽</param>
        /// <param name="horizontalMode">水平对齐方式：其中<see cref="TextHorizontalMode.TextCenter"/>只是X居中，<see cref="TextHorizontalMode.TextMid"/>则是X、Y都居中</param>
        /// <param name="verticalMode">垂直对齐方式：当设置水平对齐为<see cref="TextHorizontalMode.TextMid"/>时，此参数无效；以下几种组合会导致Y轴高度失效置为0：
        /// <para><see cref="TextHorizontalMode.TextLeft"/>+<see cref=" TextVerticalMode.TextBottom"/>;</para>
        /// <para><see cref="TextHorizontalMode.TextLeft"/>+<see cref=" TextVerticalMode.TextTop"/>;</para>
        /// <para><see cref="TextHorizontalMode.TextLeft"/>+<see cref=" TextVerticalMode.TextVerticalMid"/></para></param>
        /// <param name="textStyleId">文本样式：若为空或者不存在则使用默认样式</param>
        /// <param name="layerId">图层ID</param>
        /// <param name="btr">块表记录：若为null，则自动创建，配合事务 trans 一起使用</param>
        /// <param name="trans">事务：若为null，则自动创建并提交，否则需手动提交，配合块表记录 btr 一起使用</param>
        /// <param name="updateScreen">是否更新屏幕：每创建一个元素都会更新到屏幕上，耗时较长，否则程序运行完成后所有元素统一更新到屏幕上。<para>只适用于块表记录和事务为空的情况</para></param>
        /// <returns>单行文本</returns>
        public static DBText CreateDBText(Point3d position, string text, double textheight = 2.5, double widthFactor = 0.7, double rotation = 0, System.Drawing.Color color = default, LineWeight lineWeight = LineWeight.ByLayer, TextHorizontalMode horizontalMode = TextHorizontalMode.TextLeft, TextVerticalMode verticalMode = TextVerticalMode.TextBase, ObjectId textStyleId = default, ObjectId layerId = default, BlockTableRecord btr = null, Transaction trans = null, bool updateScreen = false)
        {
            DBText dBText = new DBText();
            //单行文本有两个坐标，第一个是位置，第二个是对齐点(AlignmentPoint)。初始设置Position的值，AlignmentPoint为空，设置文字的对齐方式后，Position会自动更新，AlignmentPoint一直保持初始Position值。默认对齐方式为左对齐。非"左对齐"对齐方式，文字的几何坐标和对齐坐标不同，几何坐标是文字的左下角坐标，对齐坐标是文字的基准点
            dBText.Position = position;
            dBText.TextString = text ?? "";
            dBText.Rotation = rotation.ToRadian();
            dBText.Height = textheight;
            dBText.WidthFactor = widthFactor;
            dBText.HorizontalMode = horizontalMode;
            dBText.VerticalMode = verticalMode;
            dBText.LineWeight = lineWeight;

            //当对齐方式为TextLeft，不要设置AlignmentPoint，否则报错。其他对齐方式则要设置该值，否则对齐失效且Y值会置为0
            if (horizontalMode != TextHorizontalMode.TextLeft)
                dBText.AlignmentPoint = position;

            if (color != default)
                dBText.Color = Color.FromColor(color);

            if (textStyleId != default)
                dBText.TextStyleId = textStyleId;

            if (layerId != default)
                dBText.LayerId = layerId;

            if (btr != null && trans != null)
            {
                btr.AppendEntity(dBText);
                trans.AddNewlyCreatedDBObject(dBText, true);
            }
            else
                AddEntity(dBText, updateScreen);

            return dBText;
        }

        /// <summary>
        /// 创建文字样式，若存在同名样式则返回该样式
        /// </summary>
        /// <param name="styleName">样式名称</param>
        /// <param name="fontName">字体名称</param>
        /// <param name="bigFontName">大字体名称</param>
        /// <param name="textSize">文字尺寸</param>
        /// <param name="widthFactor">宽度因子</param>
        /// <param name="bold">是否加粗(非大字体适用)</param>
        /// <param name="italic">是否斜体(非大字体适用)</param>
        /// <returns>文字样式Id</returns>
        public static ObjectId CreateTextStyle(string styleName, string fontName, string bigFontName = "", double textSize = 2.5, double widthFactor = 0.7, bool bold = false, bool italic = false)
        {
            using (DocumentLock docLock = Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                using (Database db = Application.DocumentManager.MdiActiveDocument.Database)
                {
                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        TextStyleTable tst = trans.GetObject(db.TextStyleTableId, OpenMode.ForWrite) as TextStyleTable;
                        if (tst.Has(styleName))
                        {
                            trans.Abort();
                            return tst[styleName];
                        }

                        TextStyleTableRecord newTR = new TextStyleTableRecord();
                        newTR.Name = styleName;

                        try
                        {
                            newTR.FileName = fontName;    //首先设置文字样式的shx形样式

                            if (string.IsNullOrWhiteSpace(bigFontName))   //若有大字体则不设windows字体，否则大字体不生效
                            {
                                TextStyleTableRecord tr = trans.GetObject(db.Textstyle, OpenMode.ForWrite) as TextStyleTableRecord;
                                Autodesk.AutoCAD.GraphicsInterface.FontDescriptor newFont = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(fontName, bold, italic, tr.Font.CharacterSet, tr.Font.PitchAndFamily);
                                newTR.Font = newFont;      //然后再设置字体样式，即Windows字体样式，否则没有shx样式时字体无效，2014中会提示“** 形 XX 未定义”
                            }

                            ////若设置完字体样式后，shx样式没有了，说明该样式为常规样式，用Font即可；若shx样式还在且名称一致，则再设一次(设置Font后，shx样式更改为.ttf)
                            //if (!string.IsNullOrWhiteSpace(newTR.FileName) && newTR.FileName.Substring(0, newTR.FileName.LastIndexOf('.')).ToLower() == fontName.ToLower())
                            //{
                            //    newTR.FileName = fontName;        //shx样式不能设置加粗，倾斜
                            //}

                            if (!string.IsNullOrWhiteSpace(bigFontName))
                                newTR.BigFontFileName = bigFontName;

                            newTR.TextSize = textSize;
                            newTR.XScale = widthFactor;


                            tst.UpgradeOpen();
                            tst.Add(newTR);
                            trans.AddNewlyCreatedDBObject(newTR, true);
                        }
                        catch (System.Exception ex)
                        {
                            CADTool.ShowMessage($"文字样式“{styleName}创建失败，请检查是否存在字体文件“{fontName}”或“{bigFontName}”");
                            return ObjectId.Null;
                        }

                        trans.Commit();

                        return newTR.ObjectId;
                    }
                }
            }
        }

        /// <summary>
        /// 将实体添加到模型空间
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="updateScreen">是否更新屏幕：每创建一个元素都会更新到屏幕上，耗时较高，否则程序运行完成后所有元素统一更新到屏幕上</param>
        public static void AddEntity(Entity entity, bool updateScreen = false)
        {
            using (DocumentLock doclock = Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                using (Database db = Application.DocumentManager.MdiActiveDocument.Database)
                {
                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        //entity.SetDatabaseDefaults();    //加入模型之前可设置实体的默认数据库
                        //btr.UpgradeOpen();   //将只读模式升级为可写模式，前面已经以OpenMode.ForWrite打开，此处不需要
                        btr.AppendEntity(entity);
                        trans.AddNewlyCreatedDBObject(entity, true);

                        trans.Commit();

                        if (updateScreen)
                            Application.UpdateScreen();
                    }
                }
            }
        }
    }

    public static class Extensions
    {
        /// <summary>
        /// 角度转为弧度
        /// </summary>
        /// <param name="value">角度</param>
        /// <returns>弧度</returns>
        public static double ToRadian(this double value)
        {
            return value / 180 * Math.PI;
        }
    }
}
