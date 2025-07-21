using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace CADConverter
{
    public partial class FormExcelToCAD : FormBase<FormExcelToCAD>//Form
    {
        private FormExcelToCAD()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            txtExcelPath.ReadOnly = true;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = $"Excel转CAD v{version.Major}.{version.Minor}.{version.Build}";
        }

        private void btnChooseExcel_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Title = "请选择要转换的Excel文件";
            fileDialog.Filter = "xlsx Files|*.xlsx|xls Files|*.xls";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                txtExcelPath.Text = fileDialog.FileName;
            }
        }

        private void ckbRowColumnSize_MouseHover(object sender, EventArgs e)
        {
            ToolTip toolTip = new ToolTip();
            toolTip.IsBalloon = true;
            toolTip.UseFading = true;
            toolTip.AutoPopDelay = 6000;
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;

            string text = "";
            Control control = (Control)sender;
            switch (control.Name)
            {
                case "ckbMergeCells":
                    text = "转换合并单元格";
                    break;
                case "ckbRowColumnSize":
                    text = "等比例转换行高和列宽";
                    break;
                case "ckbFontSize":
                    text = "等比例转换文字大小(部分文字大小、颜色、样式与单元格设置不符，无法解析，将采用默认值)";
                    break;
                case "ckbFontColor":
                    text = "转换文字颜色";
                    break;
                case "ckbFontStyle":
                    text = "转换文字样式(加粗，斜体)";
                    break;
                case "ckbAlign":
                    text = "转换文字对齐方式";
                    break;
                case "ckbFormat":
                    text = "转换数字显示格式";
                    break;
                case "ckbFill":
                    text = "转换单元格填充颜色";
                    break;
                case "ckbEmptyCell":
                    text = "转换空白单元格样式";
                    break;
                case "ckbExplode":
                    text = "转换后分解表格为直线";
                    break;
                case "ckbImage":
                    text = "转换表格内嵌图片";
                    break;
                    //case "btnOK":
                    //    text = "设置转换项时，全格式支持，但速度较慢";
                    //    break;
                    //case "btnOK2":
                    //    text = "设置转换项时，单元格文字样式与其内部文字样式不同时将无法解析、转换，但速度较快";
                    //    break;
            }
            toolTip.SetToolTip(control, text);
        }

        private void ckbFontSize_CheckedChanged(object sender, EventArgs e)
        {
            // 文字大小和行高、列宽绑定，要转全转
            if (((Control)sender).Name == "ckbRowColumnSize")
                ckbFontSize.Checked = ckbRowColumnSize.Checked;
            else
                ckbRowColumnSize.Checked = ckbFontSize.Checked;
        }

        private void ckbAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (Control control in groupBox2.Controls)
            {
                if (control is CheckBox)
                {
                    ((CheckBox)control).Checked = ckbAll.Checked;
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (GetSetting(out ConvertSetting setting))
                new ExcelToCAD().Run(txtExcelPath.Text, setting);
        }

        //private void btnOK2_Click(object sender, EventArgs e)
        //{
        //    if (GetSetting(out ConvertSetting setting))
        //        new ExcelToCAD2().Run(txtExcelPath.Text, setting);
        //}

        private bool GetSetting(out ConvertSetting setting)
        {
            setting = null;
            if (string.IsNullOrWhiteSpace(txtExcelPath.Text))
            {
                MessageBox.Show("请选择Excel文件！");
                return false;
            }

            setting = new ConvertSetting()
            {
                IsMergeCells = ckbMergeCells.Checked,
                IsRowColumnSize = ckbRowColumnSize.Checked,
                IsFontSize = ckbFontSize.Checked,
                IsFontColor = ckbFontColor.Checked,
                IsFontStyle = ckbFontStyle.Checked,
                IsFill = ckbFill.Checked,
                IsAlign = ckbAlign.Checked,
                IsFormat = ckbFormat.Checked,
                IsEmptyCell = ckbEmptyCell.Checked,
                IsImage = ckbImage.Checked,
                IsExplode = ckbExplode.Checked
            };

            return true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
