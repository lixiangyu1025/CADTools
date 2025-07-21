using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Customization;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Controls.Primitives;

namespace CADConverter
{
    /// <summary>
    /// Cui(个人理解为Customization User Interface )工具，用于创建工具栏，适用于所有工作空间，需要创建并加载cui文件。新版的Ribbon工具栏也可使用<see cref="RibbonTool"/>进行创建
    /// <para>该工具共包含两种按钮创建方式，关于两种方式的具体介绍详见开发手册：</para>
    /// <para>1，传统的“弹出式菜单”按钮：创建结构为<see cref="CustomizationSection"/>→<see cref="MacroGroup"/>→<see cref="PopMenu"/>→<see cref="PopMenuItem"/></para>
    /// <para>2，新式的“Ribbon”按钮即选项卡按钮（即<see cref="RibbonTool"/>工具所创建按钮），创建结构为<see cref="CustomizationSection"/>→<see cref="MacroGroup"/>→<see cref="RibbonTabSource"/>→<see cref="RibbonPanelSource"/>→<see cref="RibbonCommandButton"/></para>
    /// <para>使用须知：</para>
    /// <para>1，为防止重复创建、加载，先使用<see cref="LoadCuiFile()"/>加载一次，成功直接退出，失败则重新创建cui文件并加载</para>
    /// <para>2，创建完成后要执行<see cref="Save(CustomizationSection)"/>保存cui文件</para>
    /// <para>3，保存文件后使用<see cref="LoadCuiFile()"/>加载工具栏</para>
    /// </summary>
    public partial class CuiTool
    {
        private string _cuiName;
        private string _cuiPath;

        /// <summary>
        /// 创建<see cref="CuiTool"/>工具实例
        /// </summary>
        /// <param name="cuiName">cui文件名对应到CAD中的自定义组名。为了保证cui文件不会互相覆盖，最好针对每个项目设置不同的名称</param>
        /// <param name="cuiPath">cui文件路径(不能使用该工具程序集运行路径，同时加载多个插件时运行本程序集的路径可能只有一个，导致多个插件cui文件生成到一个插件安装目录下。需使用调用程序集路径)</param>
        public CuiTool(string cuiName, string cuiPath)
        {
            _cuiName = cuiName;
            _cuiPath = cuiPath;
        }

        /// <summary>
        /// 判断是否已加载自定义cui文件
        /// <para>CAD中切换工作空间会导致自定义cui菜单不显示，但其确实已加载，不能再次加载</para>
        /// <para>切换工作空间导致的菜单隐藏目前无法解决，所以还是直接卸载再加载一次</para>
        /// </summary>
        /// <returns>是否已加载</returns>
        private bool HasLoadCuiFile()
        {
            string myCuiPath = _cuiName;
            string mainMenuPath = Application.GetSystemVariable("MENUNAME").ToString();
            var version = Application.Version;
            if (version.Major < 18)     //2010版之前
            {
                mainMenuPath += ".cui";
                myCuiPath += ".cui";
            }
            else
            {
                mainMenuPath += ".cuix";
                myCuiPath += ".cuix";
            }

            CustomizationSection customizationSection = new CustomizationSection(mainMenuPath);
            PartialCuiFileCollection partialCuiFileCollection = customizationSection.PartialCuiFiles;
            if (partialCuiFileCollection.Contains(myCuiPath))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 创建自定义工具栏
        /// </summary>
        /// <returns>自定义工具栏</returns>
        public CustomizationSection CreateToolBar()
        {
            CustomizationSection customization = new CustomizationSection();
            customization.MenuGroupName = _cuiName;
            //customization.MenuGroupName = "ACAD";  //会搞坏CAD主菜单

            //不要往CAD主菜单中添加，也不要往工作空间中添加，否则可能导致即使没有加载程序dll，菜单栏也会显示
            //即使不往主菜单和工作空间添加，cui菜单还是会残留在CAD中
            //CustomizationSection mainCustomization = new CustomizationSection(Application.GetSystemVariable("MENUNAME").ToString());
            ////customization.ParentCUI = mainCustomization;

            //foreach (Workspace item in mainCustomization.Workspaces)
            //{
            //    customization.Workspaces.Add(item);
            //}

            //var ss = customization.Workspaces.SetDefaultWorkspaceByName(Application.GetSystemVariable("WSCURRENT").ToString());

            return customization;
        }

        /// <summary>
        /// 创建命令组
        /// </summary>
        /// <param name="customization">工具栏</param>
        /// <param name="groupName">组名</param>
        /// <returns>命令组</returns>
        public MacroGroup CreateGroup(CustomizationSection customization, string groupName)
        {
            MacroGroup macroGroup = new MacroGroup(groupName, customization.MenuGroup);
            return macroGroup;
        }

        /// <summary>
        /// 创建弹出式顶级菜单-显示在工具栏最顶层
        /// </summary>
        /// <param name="customization">工具栏</param>
        /// <param name="menuName">菜单显示名称</param>
        /// <returns>顶级菜单</returns>
        public PopMenu CreateParentPopMenu(CustomizationSection customization, string menuName)
        {
            PopMenu popMenu = new PopMenu(menuName, new StringCollection() { customization.MenuGroupName }, "", customization.MenuGroup);

            return popMenu;
        }

        /// <summary>
        /// 创建弹出式多级菜单的父级
        /// </summary>
        /// <param name="customization">工具栏</param>
        /// <param name="menuName">菜单显示名称</param>
        /// <param name="parentMenu">顶级菜单</param>
        /// <returns>多级菜单的父级菜单</returns>
        public PopMenu CreateGroupPopMenu(CustomizationSection customization, PopMenu parentMenu, string menuName)
        {
            PopMenu popMenu = new PopMenu(menuName, new StringCollection(), "", customization.MenuGroup);
            PopMenuRef popMenuRef = new PopMenuRef(popMenu, parentMenu, -1);   //必须加这行

            return popMenu;
        }

        /// <summary>
        /// 创建弹出式子级菜单
        /// </summary>
        /// <param name="macroGroup">命令组</param>
        /// <param name="parentMenu">父级菜单(可为顶级菜单或多级菜单)</param>
        /// <param name="text">菜单显示文字</param>
        /// <param name="command">菜单对应的命令</param>
        /// <param name="smallImage">小图标路径</param>
        /// <param name="labelId">菜单Id</param>
        /// <returns>子级菜单</returns>
        public PopMenuItem CreateSubPopMenu(MacroGroup macroGroup, PopMenu parentMenu, string text, string command, string smallImage = "", /*string largeImage = "",*/ string labelId = "")
        {
            //largeImage：大图标路径，弹出式菜单只能显示小图标，该值无效
            MenuMacro menuMacro = macroGroup.CreateMenuMacro(command, command, "", "", MacroType.Any, smallImage, "", labelId);  // new MenuMacro(macroGroup, command, command, "");
            PopMenuItem popMenuItem = new PopMenuItem(menuMacro, text, parentMenu, -1);

            return popMenuItem;
        }

        //public void Create(CustomizationSection customization, MacroGroup macroGroup, PopMenu parentMenu)
        //{
        //    PopMenu popMenu = new PopMenu("测试父级", new StringCollection(), "", customization.MenuGroup);
        //    //

        //    MenuMacro menuMacro = macroGroup.CreateMenuMacro("11", "1", "", "", MacroType.Any, "", "", "");
        //    PopMenuItem popMenuItem = new PopMenuItem(menuMacro, "ceshi", popMenu, -1);


        //    //return popMenuRef;
        //}

        /// <summary>
        /// 保存工具栏
        /// </summary>
        /// <param name="customization"></param>
        public void Save(CustomizationSection customization)
        {
            var cuiFilename = GetFileName();
            if (File.Exists(cuiFilename))
                File.Delete(cuiFilename);

            customization.SaveAs(cuiFilename);
        }

        /// <summary>
        /// 加载工具栏
        /// </summary>
        /// <returns>加载是否成功</returns>
        public bool LoadCuiFile()
        {
            var cuiFilename = GetFileName();

            if (!File.Exists(cuiFilename))
                return false;

            //切换工作空间会导致的已加载的自定义菜单隐藏，目前无法解决，所以还是直接卸载再加载一次
            //方案1：内置方法 先卸载再加载
            Application.UnloadPartialMenu(cuiFilename);
            var b2 = Application.LoadPartialMenu(cuiFilename);

            //方案2：发送_.cuiload命令
            //第一种方案加载失败后，用第二种方案再加载一次    第一种方法加载即可，第一种失败可能是文件已加载，再用第二种方法加载会报错
            //if (b2 == false)
            //{
            //var doc = Application.DocumentManager.MdiActiveDocument;

            ////系统变量原始值
            //object oldCmdEcho = Application.GetSystemVariable("CMDECHO");
            //object oldFileDia = Application.GetSystemVariable("FILEDIA");

            ////更改系统变量
            //Application.SetSystemVariable("MENUBAR", 1);   //设置菜单栏显示
            //Application.SetSystemVariable("CMDECHO", 0);   //设置命令行回显关闭
            //Application.SetSystemVariable("FILEDIA", 0);   //设置文件选择方式为输入路径而不是弹窗选择

            ////加载cuix文件  安装后若路径中有空格，则加载错误，CAD中空格等于回车
            //doc.SendStringToExecute("_.cuiload " + cuiFilename + " ", false, false, false);

            ////还原系统变量，不能直接设置系统变量，这样会导致cuix文件还没被加载完成，就已经还原了系统变量，用发送命令的方式
            ////Application.SetSystemVariable("MENUBAR", oldMenuBar);
            ////Application.SetSystemVariable("CMDECHO", oldCmdEcho);
            ////Application.SetSystemVariable("FILEDIA", oldFileDia);
            //doc.SendStringToExecute("(setvar \"CMDECHO\" " + oldCmdEcho.ToString() + ")(princ) ", false, false, false);
            //doc.SendStringToExecute("(setvar \"FILEDIA\" " + oldFileDia.ToString() + ")(princ) ", false, false, false);
            //}

            #region 尝试将自定义选项卡式菜单加入主菜单，直接把主菜单都搞坏了
            //string myCuiPath = _cuiName;
            //string mainMenuPath = Application.GetSystemVariable("MENUNAME").ToString();
            //var version = Application.Version;
            //if (version.Major < 18)     //2010版之前
            //{
            //    mainMenuPath += ".cui";
            //    myCuiPath += ".cui";
            //}
            //else
            //{
            //    mainMenuPath += ".cuix";
            //    myCuiPath += ".cuix";
            //}

            //CustomizationSection mainCustomization = new CustomizationSection(mainMenuPath);
            //PartialCuiFileCollection partialCuiFileCollection = mainCustomization.PartialCuiFiles;
            //if (partialCuiFileCollection.Contains(myCuiPath))
            //{
            //    CustomizationSection myCustomization = new CustomizationSection(myCuiPath);
            //    foreach (RibbonTabSource item in myCustomization.MenuGroup.RibbonRoot.RibbonTabSources)
            //    {
            //        mainCustomization.MenuGroup.RibbonRoot.RibbonTabSources.Add(item);
            //    }

            //    foreach (RibbonPanelSource item in myCustomization.MenuGroup.RibbonRoot.RibbonPanelSources)
            //    {
            //        mainCustomization.MenuGroup.RibbonRoot.RibbonPanelSources.Add(item);
            //    }
            //}
            //mainCustomization.Save(); 
            #endregion

            //经测试，加了这行代码，会导致菜单全部不见，只剩自定义的
            //Application.ReloadAllMenus();      

            return true;
        }

        /// <summary>
        /// 卸载工具栏(防止下次打开CAD时，即使未加载程序Dll，菜单也会存在)
        /// </summary>
        /// <returns>卸载是否成功</returns>
        public bool UnLoadCuiFile()
        {
            try
            {
                string cuiFilename = GetFileName();
                if (File.Exists(cuiFilename))
                    File.Delete(cuiFilename);

                //var b = Application.UnloadPartialMenu(cuiFilename);   //不管用
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetFileName()
        {
            //新旧版cui文件扩展名不同，类似于office
            var version = Application.Version;
            string extName = ".cuix";    //扩展名
            if (version.Major < 18)     //2010版之前
                extName = ".cui";

            string cuiFilename = Path.Combine(_cuiPath, _cuiName + extName);

            return cuiFilename;
        }

    }

    public partial class CuiTool
    {
        /// <summary>
        /// 创建Tab选项卡（目前能创建成功，只是在选项卡中显示Tab，但是内中Panel不显示。但是通过cui命令打开自定义用户界面，在其中局部自定义文件可以看到选项卡下面的Panel-行-按钮）
        /// <para>所以目前Cui-选项卡式菜单暂时不要用</para>
        /// </summary>
        /// <param name="customization">工具栏</param>
        /// <param name="text">选项卡标题</param>
        /// <returns>Tab选项卡</returns>
        public RibbonTabSource CreateRibbonTabSource(CustomizationSection customization, string text)
        {
            RibbonRoot ribbonRoot = customization.MenuGroup.RibbonRoot;
            RibbonTabSource tab = new RibbonTabSource(ribbonRoot);
            tab.DefaultDisplay = DefaultDisplay.AddToWorkspace;
            tab.DisplayType = ContextualTabDisplayType.Full;
            tab.Text = text;
            tab.WorkspaceBehavior = TabWorkspaceBehavior.MergeOrAddTab;
            ribbonRoot.RibbonTabSources.Add(tab);

            return tab;
        }

        /// <summary>
        /// 创建Panel面板
        /// </summary>
        /// <param name="customization">工具栏</param>
        /// <param name="tab">Tab选项卡</param>
        /// <param name="text">panel标题</param>
        /// <returns>Panel面板</returns>
        public RibbonPanelSource CreateRibbonPanelSource(CustomizationSection customization, RibbonTabSource tab, string text)
        {
            RibbonRoot ribbonRoot = customization.MenuGroup.RibbonRoot;
            RibbonPanelSource panel = new RibbonPanelSource(ribbonRoot);
            panel.Text = text;
            ribbonRoot.RibbonPanelSources.Add(panel);

            RibbonPanelSourceReference panelReference = new RibbonPanelSourceReference(tab);
            panelReference.PanelId = panel.ElementID;
            tab.Items.Add(panelReference);

            return panel;
        }

        /// <summary>
        /// 创建按钮
        /// </summary>
        /// <param name="macroGroup">命令组</param>
        /// <param name="panel">Panel面板</param>
        /// <param name="text">按钮显示文字：按钮的宽度无法手动设置，但可以通过改变文字长度，比如在文字前后增加空格，即可加宽按钮</param>
        /// <param name="command">按钮对应命令</param>
        /// <param name="buttonStyle">按钮样式</param>
        /// <param name="smallImage">小图标路径</param>
        /// <param name="largeImage">大图标路径</param>
        /// <param name="labelId">按钮Id</param>
        /// <returns>按钮</returns>
        public RibbonCommandButton CreateRibbonCommandButton(MacroGroup macroGroup, RibbonPanelSource panel, string text, string command, RibbonButtonStyle buttonStyle = RibbonButtonStyle.LargeWithText, string smallImage = "", string largeImage = "", string labelId = "")
        {
            MenuMacro menuMacro = macroGroup.CreateMenuMacro(command, command, "", "", MacroType.Any, smallImage, largeImage, labelId);

            //必须先有行，用行添加按钮，panel添加行。panel直接添加按钮不显示
            RibbonRow row = null;         //按钮按照一行横向排列。否则每个按钮添加一行纵向排列太难看
            if (panel.Items.Count > 0)
                row = panel.Items[0] as RibbonRow;
            else
                row = new RibbonRow(panel);
            panel.Items.Add(row);

            RibbonCommandButton button = new RibbonCommandButton(row);
            button.ButtonStyle = buttonStyle;
            button.Text = text;
            button.MacroID = menuMacro.ElementID;            //为按钮绑定命令 
            row.Items.Add(button);

            return button;
        }
    }
}
