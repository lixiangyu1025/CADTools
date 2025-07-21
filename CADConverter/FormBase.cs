using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CADConverter
{
    public class FormBase<T> : Form where T : Form
    {
        private static T instance;

        public FormBase()
        {
            StartPosition = FormStartPosition.CenterScreen;
            FormClosed += FrmSingleBase_FormClosed;
        }

        public static T GetInstance(params object[] args)
        {
            if (instance == null || instance.IsDisposed)
            {
                instance = (T)Activator.CreateInstance(typeof(T), BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, args, null);

                return instance;
            }

            return instance;
        }


        new public void Show()
        {
            Size size = instance.Size;
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(instance);   //非模态显示

            //非模态显示时，最小化窗体后再点击按钮不会将窗体恢复，所以需要设置窗体状态，模态显示则不需要
            instance.Size = size;   //调用CAD非模态显示，有时会莫名其妙改变窗体大小
            instance.WindowState = FormWindowState.Normal;

            instance.Activate();   //打开多个窗体时，激活当前窗体，否则可能被其他窗体遮挡
            CADTool.SetFocus();
        }


        private void FrmSingleBase_FormClosed(object sender, FormClosedEventArgs e)
        {
            CADTool.SetFocus();
        }
    }
}
