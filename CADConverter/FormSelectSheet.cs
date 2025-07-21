using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CADConverter
{
    public partial class FormSelectSheet : Form
    {
        /// <summary>
        /// 选择的Sheet
        /// </summary>
        public List<string> SelectedSheets { get; internal set; } = new List<string>();

        public FormSelectSheet(List<string> sheetNames)
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;

            cklbSheets.CheckOnClick = true;
            foreach (string sheetName in sheetNames)
            {
                cklbSheets.Items.Add(sheetName);
            }

            ckbAll.Checked = true;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            foreach (var item in cklbSheets.CheckedItems)
            {
                SelectedSheets.Add(item.ToString());
            }

            DialogResult = DialogResult.OK;
        }

        private void ckbAll_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < cklbSheets.Items.Count; i++)
            {
                cklbSheets.SetItemChecked(i, ckbAll.Checked);
            }
        }
    }
}
