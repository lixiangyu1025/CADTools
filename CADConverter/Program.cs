using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using CADConverter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

[assembly: ExtensionApplication(typeof(Program))]
namespace CADConverter
{
    public class Program : IExtensionApplication
    {
        public static string ProgramName = "CADConverter";
        public static string MenuName = "CAD工具集";

        public void Initialize()
        {
            Application.SystemVariableChanged += Application_SystemVariableChanged;

            //适用于RibbonTool
            if (Application.GetSystemVariable("WSCURRENT").ToString() != "AutoCAD 经典")
                ComponentManager.ItemInitialized += ComponentManager_ItemInitialized;

            //加载工具栏
            Commands.CreateMenu();

            //初始化日志路径
            Log.InitializePath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CADConverterLogs"));
        }

        public void Terminate()
        {
            CuiTool cuiTool = new CuiTool(ProgramName, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            cuiTool.UnLoadCuiFile();

            //删除图片缓存
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Temp";
            if(Directory.Exists(path))
                Directory.Delete(path, true);
        }

        /// <summary>
        /// CAD系统变量改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_SystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            //当CAD的工作空间改变时，外加的Cui工具栏会隐藏，Ribbon工具栏会丢失，需再重新加载或创建。工作空间系统变量：WSCURRENT（WorkspaceCURRENT）
            if (e.Name.ToUpper() == "WSCURRENT")
            {
                Commands.CreateMenu();
            }
        }

        int count = 0;
        /// <summary>
        /// CAD组件初始化事件，主要用于<see cref="RibbonTool"/>在创建自定义Ribbon之前先判断系统Ribbon是否加载，否则可能创建不成功。但是在“AutoCAD 经典”工作空间下，不会执行此事件
        /// <para>所有CAD原生Ribbon按钮初始化都会执行一次，所以创建自定义Ribbon之后立即卸载事件</para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComponentManager_ItemInitialized(object sender, RibbonItemEventArgs e)
        {
            if (ComponentManager.Ribbon != null)
            {
                if (!ComponentManager.Ribbon.Tabs.Any(t => t.Title == MenuName))
                {
                    Commands.CreateMenu();

                    if (++count >= 2)
                        ComponentManager.ItemInitialized -= ComponentManager_ItemInitialized;
                }
            }
        }
    }
}
