using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using Autodesk.Windows;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CADConverter
{
    /// <summary>
    /// Ribbon工具，用于创建新版选项卡式工具栏，只适用于非"AutoCAD 经典"工作空间，针对此工作空间请使用<see cref="CuiTool"/>进行创建
    /// <para>创建结构为<see cref="RibbonTab"/>→<see cref="RibbonPanel"/>→<see cref="RibbonButton"/></para>
    /// <para>CAD2016(内部版本号20)版及以上Ribbon背景色为灰色，以下Ribbon背景色为白色，图标应选用合适颜色或者根据两个版本设置两套颜色的图标以达到和原生图标一致效果</para>
    /// </summary>
    public class RibbonTool
    {
        /// <summary>
        /// 创建<see cref="RibbonTool"/>工具实例
        /// </summary>
        public RibbonTool()
        {

        }

        /// <summary>
        /// 创建Tab选项卡
        /// </summary>
        /// <param name="title">选项卡标题</param>
        /// <param name="id">选项卡Id</param>
        /// <param name="isActive">选项卡是否激活</param>
        /// <returns>Ribbon选项卡</returns>
        public RibbonTab CreateRibbonTab(string title, string id = "", bool isActive = false)
        {
            //在“AutoCAD 经典”工作流下无法创建RibbonTab，RibbonPaletteSet为null
            //即使在新版的工作空间下，当本插件设为自动加载，且创建Ribbon方法写在Initialize()中时，由于CAD刚打开就会自动加载插件创建Ribbon，此时RibbonPaletteSet也会为null，需要手动创建一次。当然还有可能创建不成功，因为这段代码在CAD系统初始化时就会执行，所以最稳妥的方式是增加ComponentManager_ItemInitialized事件，判断ComponentManager.Ribbon != null
            //if (RibbonServices.RibbonPaletteSet == null)        //此对象需要引用AcWindows.dll ，其他都是AdWindows.dll
            //{
            //    return null;
            //    //RibbonServices.CreateRibbonPaletteSet();   //加了这个会导致CAD自身的菜单栏丢失
            //}

            //RibbonControl ribbonControl = RibbonServices.RibbonPaletteSet.RibbonControl;

            if (ComponentManager.Ribbon == null)
                return null;

            RibbonControl ribbonControl = ComponentManager.Ribbon;
            RibbonTab tab = ribbonControl.Tabs.FirstOrDefault(t => t.Title == title);   //检测是否已存在同名Tab
            if (tab == null)
            {
                tab = new RibbonTab();
                tab.Title = title;
                tab.Id = string.IsNullOrWhiteSpace(id) ? "MyRibbon1" : id;  //不设Id关闭CAD时报错
                ribbonControl.Tabs.Add(tab);
            }

            tab.IsActive = isActive;
            tab.IsVisible = true;

            return tab;
        }

        /// <summary>
        /// 创建Panel面板
        /// </summary>
        /// <param name="tab">Tab选项卡</param>
        /// <param name="title">panel标题</param>
        /// <returns>Panel面板</returns>
        public RibbonPanel CreateRibbonPanel(RibbonTab tab, string title)
        {
            RibbonPanel panel = tab.Panels.FirstOrDefault(t => t.Source.Title == title);
            if (panel == null)
            {
                RibbonPanelSource panelSource = new RibbonPanelSource();
                panelSource.Title = title;

                panel = new RibbonPanel();
                panel.Source = panelSource;
                tab.Panels.Add(panel);
            }

            return panel;
        }

        /// <summary>
        /// 创建普通按钮
        /// </summary>
        /// <param name="panel">Panel面板</param>
        /// <param name="text">按钮显示文字：按钮的宽度无法手动设置，但可以通过改变文字长度，比如在文字前后增加空格，即可加宽按钮</param>
        /// <param name="command">按钮对应命令</param>
        /// <param name="size">按钮大小</param>
        /// <param name="largeImage">按钮大图标(尺寸32*32)</param>   
        /// <param name="smallImage">按钮小图标(尺寸16*16)</param>
        /// <returns>普通按钮</returns>
        public RibbonButton CreateRibbonButton(RibbonPanel panel, string text, string command, RibbonItemSize size = RibbonItemSize.Large, Image largeImage = null, Image smallImage = null)
        {
            //新版用panel.Items，旧版没有，要用panel.Source.Items，为了向下兼容，统一使用旧版的panel.Source.Items
            RibbonItem ribbonItem = panel.Source.Items.FirstOrDefault(b => b.Text == text && b.GetType().Name == "RibbonButton");

            if (ribbonItem == null)
            {
                RibbonButton ribbonButton = new RibbonButton();
                ribbonButton.Text = text;
                ribbonButton.ShowText = true;
                ribbonButton.ShowToolTipOnDisabled = true;
                ribbonButton.ShowImage = true;
                ribbonButton.Size = size;
                ribbonButton.Orientation = System.Windows.Controls.Orientation.Vertical;
                ribbonButton.CommandParameter = command;                  //为按钮绑定命令
                ribbonButton.CommandHandler = new MyCommandHandler();     //命令执行处理

                //将位图转换成BitmapImage
                if (largeImage != null)
                    ribbonButton.LargeImage = ConvertToBitmapImage(largeImage);

                if (smallImage != null)
                    ribbonButton.Image = ConvertToBitmapImage(smallImage);

                panel.Source.Items.Add(ribbonButton);

                return ribbonButton;
            }

            return ribbonItem as RibbonButton;
        }

        /// <summary>
        /// 将Image转换为BitmapImage
        /// </summary>
        /// <param name="image">Image</param>
        /// <returns>BitmapImage</returns>
        private BitmapImage ConvertToBitmapImage(Image image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            return bitmapImage;
        }

    }

    /// <summary>
    /// 执行按钮命令
    /// </summary>
    public class MyCommandHandler : ICommand
    {
        /// <summary>
        /// 创建实例
        /// </summary>
        public MyCommandHandler()
        {

        }

        /// <summary>
        /// 命令是否可执行更改时发生。
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// 此命令可执行
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        /// 执行按钮命令
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object parameter)
        {
            RibbonButton ribBtn = parameter as RibbonButton;
            if (ribBtn != null)
            {
                //加 \n 是为了输入回车，提交命令
                Application.DocumentManager.MdiActiveDocument.SendStringToExecute($"{ribBtn.CommandParameter?.ToString()}\n", true, false, true);

                //此方法提交完命令即退出，命令中的错误此处无法捕获
            }
        }
    }
}
