using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CADConverter
{
    /// <summary>
    /// 自定义进度条（Form方法太多，可能会对使用者造成困惑，所以再添加一个类调用此进度条）
    /// </summary>
    internal partial class MyProgressBar : Form
    {
        /// <summary>
        /// 设置进度条最大值
        /// </summary>
        public Action<int> SetMaxValue;

        /// <summary>
        /// 设置进度条当前值
        /// </summary>
        public Action<int> SetProcess;

        /// <summary>
        /// 设置进度条标题
        /// </summary>
        public Action<string> SetTitle;

        /// <summary>
        /// 关闭进度条(进度条当前值等于最大值时会自动关闭)
        /// </summary>
        public Action Exit;

        /// <summary>
        /// 初始化进度条
        /// </summary>
        /// <param name="maxValue">进度条最大值</param>
        /// <param name="title">进度条标题</param>
        /// <param name="isShow">初始化后是否立即显示进度条</param>
        public MyProgressBar(int maxValue = 100, string title = "", bool isShow = true)
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterScreen;
            this.lblMsg.ForeColor = Color.Green;
            this.BackColor = Color.LightCyan;

            lblTitle.Text = title;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = maxValue;
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Step = 1;

            SetMaxValue = SetMaxValueMethod;
            SetProcess = SetProgressMethod;
            SetTitle = SetTitleMethod;
            Exit = ExitMethod;

            TopMost = true;

            if (isShow)
            {
                Task.Run(() =>
                {
                    if (this.IsDisposed == false)
                    {
                        TopMost = true;
                        ShowDialog();
                    }
                });
            }
        }

        private void SetMaxValueMethod(int maxValue)
        {
            if (progressBar1.InvokeRequired && this.IsDisposed == false && progressBar1.IsDisposed == false)     //在多线程中，可能调用该方法的线程已经结束，进度条窗体已经释放
            {
                try
                {
                    progressBar1.Invoke(SetMaxValue, new object[] { maxValue });
                }
                catch
                {

                }
            }
            else
            {
                progressBar1.Maximum = maxValue;
                Application.DoEvents();
            }

        }

        private void SetProgressMethod(int currentValue)
        {
            if (progressBar1.InvokeRequired && lblMsg.InvokeRequired && this.IsDisposed == false && progressBar1.IsDisposed == false && lblMsg.IsDisposed == false)     //在多线程中，可能调用该方法的线程已经结束，进度条窗体已经释放
            {
                try
                {
                    progressBar1.Invoke(SetProcess, new object[] { currentValue });
                }
                catch
                {

                }
            }
            else if (this.IsDisposed == false && progressBar1.IsDisposed == false && lblMsg.IsDisposed == false)
            {
                try
                {
                    //进度超出最大值，关闭进度条，进度小于等于当前值时，不更新进度条
                    if (currentValue >= progressBar1.Maximum)
                    {
                        currentValue = progressBar1.Maximum;

                        this.Close();
                        this.Dispose();
                    }
                    else if (currentValue <= progressBar1.Value)
                        return;

                    //解决进度条显示滞后问题
                    //原理：当进度条值增加时，进度条控件会有一个动画效果来更新控件显示，但是值减少时没有。
                    if (currentValue >= 1)
                    {
                        progressBar1.Value = currentValue;
                        progressBar1.Value = currentValue - 1;
                    }

                    progressBar1.Value = currentValue;
                    lblMsg.Text = ((double)currentValue / progressBar1.Maximum).ToString("P");
                    Application.DoEvents();
                }
                catch
                {

                }
            }
        }

        private void SetTitleMethod(string title)
        {
            if (lblTitle.InvokeRequired && this.IsDisposed == false && lblTitle.IsDisposed == false)     //在多线程中，可能调用该方法的线程已经结束，进度条窗体已经释放
            {
                try
                {
                    lblTitle.Invoke(SetTitle, new object[] { title });
                }
                catch
                {

                }
            }
            else if (this.IsDisposed == false && lblTitle.IsDisposed == false)
            {
                lblTitle.Text = title;
                Application.DoEvents();
            }
        }

        private void ExitMethod()
        {
            if (this.InvokeRequired && this.IsDisposed == false)
                this.Invoke(Exit, null);
            else if (this.IsDisposed == false)
            {
                this?.Close();
                this?.Dispose();
            }
        }

    }

    /// <summary>
    /// 华陆进度条
    /// </summary>
    public class HLProgressBar
    {
        private MyProgressBar progressBar;

        /// <summary>
        /// 初始化进度条
        /// </summary>
        /// <param name="maxValue">进度条最大值</param>
        /// <param name="title">进度条标题</param>
        /// <param name="isShow">初始化后是否立即显示进度条</param>
        public HLProgressBar(int maxValue = 100, string title = "", bool isShow = true)
        {
            progressBar = new MyProgressBar(maxValue, title, isShow);
        }

        ///// <summary>
        ///// 设置进度条最大值
        ///// </summary>
        ///// <param name="maxValue"></param>
        //public void SetMaxValue(int maxValue)
        //{
        //    progressBar.SetMaxValue(maxValue);
        //}

        /// <summary>
        /// 设置进度条当前值
        /// </summary>
        /// <param name="currentValue"></param>
        public void SetProcess(int currentValue)
        {
            progressBar.SetProcess(currentValue);
        }

        /// <summary>
        /// 设置进度条标题
        /// </summary>
        /// <param name="title"></param>
        public void SetTitle(string title)
        {
            progressBar.SetTitle(title);
        }

        /// <summary>
        /// 显示进度条
        /// </summary>
        public void Show()
        {
            Task.Run(() =>
            {
                if (progressBar != null && progressBar.IsDisposed == false)
                    progressBar.ShowDialog();
            });
        }

        /// <summary>
        /// 关闭进度条(进度条当前值等于最大值时会自动关闭)
        /// </summary>
        public void Exit()
        {
            progressBar.Exit();
        }
    }
}
