using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Autodesk.AutoCAD.LayerManager.LayerFilter;

namespace CADConverter
{
    public class Commands
    {
        /// <summary>
        /// 根据当前工作空间自动判断创建Cui或Ribbon工具栏
        /// </summary>
        internal static void CreateMenu()
        {
            try
            {
                var workSpaceName = Application.GetSystemVariable("WSCURRENT").ToString();

                if (workSpaceName == "AutoCAD 经典")
                {
                    //由于cui方式每次都会创建一个cui文件，所以判断文件已存在的话直接加载不重复创建
                    CuiTool cuiTool = new CuiTool(Program.ProgramName, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                    if (!cuiTool.LoadCuiFile())
                    {
                        var customization = cuiTool.CreateToolBar();
                        var group = cuiTool.CreateGroup(customization, "Group1");

                        //创建菜单栏 弹出菜单
                        var parent = cuiTool.CreateParentPopMenu(customization, Program.MenuName);

                        //添加按钮及对应的图片
                        var assembly = Assembly.GetExecutingAssembly();
                        string imgConvertPath = Path.Combine(Path.GetDirectoryName(assembly.Location), "转换.png");
                        string imgRegisterPath = Path.Combine(Path.GetDirectoryName(assembly.Location), "注册.png");
                        string imgRemainingDaysPath = Path.Combine(Path.GetDirectoryName(assembly.Location), "有效期.png");
                        if (!File.Exists(imgConvertPath))
                            Properties.Resources.转换.Save(imgConvertPath);
                        if (!File.Exists(imgRegisterPath))
                            Properties.Resources.注册.Save(imgRegisterPath);
                        if (!File.Exists(imgRemainingDaysPath))
                            Properties.Resources.有效期.Save(imgRemainingDaysPath);

                        cuiTool.CreateSubPopMenu(group, parent, "ExcelToCAD", "HLETC", imgConvertPath);
                        cuiTool.CreateSubPopMenu(group, parent, "CADToExcel", "HLCTE", imgConvertPath);

                        //保存并加载
                        cuiTool.Save(customization);
                        cuiTool.LoadCuiFile();
                    }
                }
                else
                {
                    RibbonTool ribbonTool = new RibbonTool();
                    var tab = ribbonTool.CreateRibbonTab(Program.MenuName, "CADTools", false);
                    if (tab != null)
                    {
                        var panel = ribbonTool.CreateRibbonPanel(tab, "Excel⇌CAD");
                        ribbonTool.CreateRibbonButton(panel, "ExcelToCAD", "HLETC", largeImage: Properties.Resources.转换);
                        ribbonTool.CreateRibbonButton(panel, "CADToExcel", "HLCTE", largeImage: Properties.Resources.转换);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Application.ShowAlertDialog(ex.Message);
                Log.WriteErrorLogAsync(ex);
            }
        }

        [CommandMethod("HLETC")]
        public void ExcelToCAD()
        {
            /* 
             * V1.0版本，用COM组件，功能OK，但速度太慢（ExcelToCAD2）
             * V2.0版本，用NPOI并重构代码，功能OK，速度提升巨大（ExcelToCAD）
             * V2.0.1版本，增加转换图片功能
             */
            try
            {
                FormExcelToCAD.GetInstance().Show();
            }
            catch (System.Exception ex)
            {
                Application.ShowAlertDialog(ex.Message);
                Log.WriteErrorLogAsync(ex);
            }
        }

        [CommandMethod("HLCTE")]
        public void CADToExcel()
        {
            try
            {
                FormCADToExcel.GetInstance().Show();
            }
            catch (System.Exception ex)
            {
                Application.ShowAlertDialog(ex.Message);
                Log.WriteErrorLogAsync(ex);
            }
        }

        //[CommandMethod("HLREG")]
        //public void Register()
        //{
        //    try
        //    {
        //        new RegisterTool(Program.MenuName, programRegisterName, 365).Register();
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Application.ShowAlertDialog(ex.Message);
        //        Log.WriteErrorLogAsync(ex);
        //    }
        //}

        //[CommandMethod("HLREM")]
        //public void RemainingDays()
        //{
        //    try
        //    {
        //        Application.ShowAlertDialog(new RegisterTool(Program.MenuName, programRegisterName, 365).GetRemainingDays());
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Application.ShowAlertDialog(ex.Message);
        //        Log.WriteErrorLogAsync(ex);
        //    }
        //}
    }
}
