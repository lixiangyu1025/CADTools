using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using APP = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADConverter
{
    public partial class FormCADToExcel : FormBase<FormCADToExcel> //Form
    {

        private FormCADToExcel()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = $"CAD转Excel v{version.Major}.{version.Minor}.{version.Build}";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Hide();
            CADTool.SetFocus();

            new CADToExcel().Run();

            this.Show();
        }

        private void btnOK2_Click(object sender, EventArgs e)
        {
            PromptEntityOptions entityOptions = new PromptEntityOptions("\n请选择要转换的表格");
            entityOptions.SetRejectMessage("\n所选对象不是表格");
            entityOptions.AddAllowedClass(typeof(Table), true);

            this.Hide();
            CADTool.SetFocus();

            PromptEntityResult result = APP.DocumentManager.MdiActiveDocument.Editor.GetEntity(entityOptions);
            if (result.Status == PromptStatus.OK)
            {
                using (Transaction trans = APP.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
                {
                    Table table = trans.GetObject(result.ObjectId, OpenMode.ForRead) as Table;
                    if (table == null)
                    {
                        MessageBox.Show("选择的不是表格，请重新选择！");
                        return;
                    }

                    new CADToExcel().Run(table);
                }
            }

            this.Show();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
