namespace CADConverter
{
    partial class FormExcelToCAD
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtExcelPath = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnChooseExcel = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.ckbImage = new System.Windows.Forms.CheckBox();
            this.ckbExplode = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.ckbMergeCells = new System.Windows.Forms.CheckBox();
            this.ckbEmptyCell = new System.Windows.Forms.CheckBox();
            this.ckbFormat = new System.Windows.Forms.CheckBox();
            this.ckbAlign = new System.Windows.Forms.CheckBox();
            this.ckbFill = new System.Windows.Forms.CheckBox();
            this.ckbFontStyle = new System.Windows.Forms.CheckBox();
            this.ckbFontColor = new System.Windows.Forms.CheckBox();
            this.ckbFontSize = new System.Windows.Forms.CheckBox();
            this.ckbRowColumnSize = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.ckbAll = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtExcelPath
            // 
            this.txtExcelPath.Location = new System.Drawing.Point(6, 23);
            this.txtExcelPath.Name = "txtExcelPath";
            this.txtExcelPath.Size = new System.Drawing.Size(415, 21);
            this.txtExcelPath.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnChooseExcel);
            this.groupBox1.Controls.Add(this.txtExcelPath);
            this.groupBox1.Location = new System.Drawing.Point(13, 10);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(459, 55);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Excel路径：";
            // 
            // btnChooseExcel
            // 
            this.btnChooseExcel.Location = new System.Drawing.Point(425, 22);
            this.btnChooseExcel.Name = "btnChooseExcel";
            this.btnChooseExcel.Size = new System.Drawing.Size(30, 23);
            this.btnChooseExcel.TabIndex = 1;
            this.btnChooseExcel.Text = "...";
            this.btnChooseExcel.UseVisualStyleBackColor = true;
            this.btnChooseExcel.Click += new System.EventHandler(this.btnChooseExcel_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.ckbImage);
            this.groupBox2.Controls.Add(this.ckbExplode);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.ckbMergeCells);
            this.groupBox2.Controls.Add(this.ckbEmptyCell);
            this.groupBox2.Controls.Add(this.ckbFormat);
            this.groupBox2.Controls.Add(this.ckbAlign);
            this.groupBox2.Controls.Add(this.ckbFill);
            this.groupBox2.Controls.Add(this.ckbFontStyle);
            this.groupBox2.Controls.Add(this.ckbFontColor);
            this.groupBox2.Controls.Add(this.ckbFontSize);
            this.groupBox2.Controls.Add(this.ckbRowColumnSize);
            this.groupBox2.Location = new System.Drawing.Point(13, 75);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(459, 120);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "转换项设置(转换项会影响Excel转CAD的速度)";
            // 
            // ckbImage
            // 
            this.ckbImage.AutoSize = true;
            this.ckbImage.Location = new System.Drawing.Point(377, 51);
            this.ckbImage.Name = "ckbImage";
            this.ckbImage.Size = new System.Drawing.Size(72, 16);
            this.ckbImage.TabIndex = 13;
            this.ckbImage.Text = "转换图片";
            this.ckbImage.UseVisualStyleBackColor = true;
            this.ckbImage.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // ckbExplode
            // 
            this.ckbExplode.AutoSize = true;
            this.ckbExplode.Location = new System.Drawing.Point(6, 78);
            this.ckbExplode.Name = "ckbExplode";
            this.ckbExplode.Size = new System.Drawing.Size(72, 16);
            this.ckbExplode.TabIndex = 12;
            this.ckbExplode.Text = "分解表格";
            this.ckbExplode.UseVisualStyleBackColor = true;
            this.ckbExplode.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(4, 139);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(365, 12);
            this.label2.TabIndex = 10;
            this.label2.Text = "警    告：本程序受中国法律保护，未经授权不得复制或对外传播。";
            this.label2.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(4, 123);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(173, 12);
            this.label1.TabIndex = 9;
            this.label1.Text = "版权所属：华陆·信息档案中心";
            this.label1.Visible = false;
            // 
            // ckbMergeCells
            // 
            this.ckbMergeCells.AutoSize = true;
            this.ckbMergeCells.Location = new System.Drawing.Point(6, 24);
            this.ckbMergeCells.Name = "ckbMergeCells";
            this.ckbMergeCells.Size = new System.Drawing.Size(84, 16);
            this.ckbMergeCells.TabIndex = 8;
            this.ckbMergeCells.Text = "合并单元格";
            this.ckbMergeCells.UseVisualStyleBackColor = true;
            this.ckbMergeCells.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // ckbEmptyCell
            // 
            this.ckbEmptyCell.AutoSize = true;
            this.ckbEmptyCell.Location = new System.Drawing.Point(282, 51);
            this.ckbEmptyCell.Name = "ckbEmptyCell";
            this.ckbEmptyCell.Size = new System.Drawing.Size(84, 16);
            this.ckbEmptyCell.TabIndex = 7;
            this.ckbEmptyCell.Text = "空白单元格";
            this.ckbEmptyCell.UseVisualStyleBackColor = true;
            this.ckbEmptyCell.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // ckbFormat
            // 
            this.ckbFormat.AutoSize = true;
            this.ckbFormat.Location = new System.Drawing.Point(99, 51);
            this.ckbFormat.Name = "ckbFormat";
            this.ckbFormat.Size = new System.Drawing.Size(72, 16);
            this.ckbFormat.TabIndex = 6;
            this.ckbFormat.Text = "格式化值";
            this.ckbFormat.UseVisualStyleBackColor = true;
            this.ckbFormat.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // ckbAlign
            // 
            this.ckbAlign.AutoSize = true;
            this.ckbAlign.Location = new System.Drawing.Point(6, 51);
            this.ckbAlign.Name = "ckbAlign";
            this.ckbAlign.Size = new System.Drawing.Size(72, 16);
            this.ckbAlign.TabIndex = 5;
            this.ckbAlign.Text = "对齐方式";
            this.ckbAlign.UseVisualStyleBackColor = true;
            this.ckbAlign.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // ckbFill
            // 
            this.ckbFill.AutoSize = true;
            this.ckbFill.Location = new System.Drawing.Point(190, 51);
            this.ckbFill.Name = "ckbFill";
            this.ckbFill.Size = new System.Drawing.Size(72, 16);
            this.ckbFill.TabIndex = 4;
            this.ckbFill.Text = "填充颜色";
            this.ckbFill.UseVisualStyleBackColor = true;
            this.ckbFill.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // ckbFontStyle
            // 
            this.ckbFontStyle.AutoSize = true;
            this.ckbFontStyle.Location = new System.Drawing.Point(377, 24);
            this.ckbFontStyle.Name = "ckbFontStyle";
            this.ckbFontStyle.Size = new System.Drawing.Size(72, 16);
            this.ckbFontStyle.TabIndex = 3;
            this.ckbFontStyle.Text = "文字样式";
            this.ckbFontStyle.UseVisualStyleBackColor = true;
            this.ckbFontStyle.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // ckbFontColor
            // 
            this.ckbFontColor.AutoSize = true;
            this.ckbFontColor.Location = new System.Drawing.Point(282, 24);
            this.ckbFontColor.Name = "ckbFontColor";
            this.ckbFontColor.Size = new System.Drawing.Size(72, 16);
            this.ckbFontColor.TabIndex = 2;
            this.ckbFontColor.Text = "文字颜色";
            this.ckbFontColor.UseVisualStyleBackColor = true;
            this.ckbFontColor.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // ckbFontSize
            // 
            this.ckbFontSize.AutoSize = true;
            this.ckbFontSize.Location = new System.Drawing.Point(190, 24);
            this.ckbFontSize.Name = "ckbFontSize";
            this.ckbFontSize.Size = new System.Drawing.Size(72, 16);
            this.ckbFontSize.TabIndex = 1;
            this.ckbFontSize.Text = "文字大小";
            this.ckbFontSize.UseVisualStyleBackColor = true;
            this.ckbFontSize.CheckedChanged += new System.EventHandler(this.ckbFontSize_CheckedChanged);
            this.ckbFontSize.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // ckbRowColumnSize
            // 
            this.ckbRowColumnSize.AutoSize = true;
            this.ckbRowColumnSize.Location = new System.Drawing.Point(99, 24);
            this.ckbRowColumnSize.Name = "ckbRowColumnSize";
            this.ckbRowColumnSize.Size = new System.Drawing.Size(72, 16);
            this.ckbRowColumnSize.TabIndex = 0;
            this.ckbRowColumnSize.Text = "行高列宽";
            this.ckbRowColumnSize.UseVisualStyleBackColor = true;
            this.ckbRowColumnSize.CheckedChanged += new System.EventHandler(this.ckbFontSize_CheckedChanged);
            this.ckbRowColumnSize.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(149, 209);
            this.btnOK.Margin = new System.Windows.Forms.Padding(0);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "转换";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            this.btnOK.MouseHover += new System.EventHandler(this.ckbRowColumnSize_MouseHover);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(260, 209);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ckbAll
            // 
            this.ckbAll.AutoSize = true;
            this.ckbAll.Location = new System.Drawing.Point(13, 199);
            this.ckbAll.Name = "ckbAll";
            this.ckbAll.Size = new System.Drawing.Size(48, 16);
            this.ckbAll.TabIndex = 5;
            this.ckbAll.Text = "全选";
            this.ckbAll.UseVisualStyleBackColor = true;
            this.ckbAll.CheckedChanged += new System.EventHandler(this.ckbAll_CheckedChanged);
            // 
            // FormExcelToCAD
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(484, 242);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.ckbAll);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "FormExcelToCAD";
            this.Text = "转换设置";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtExcelPath;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnChooseExcel;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox ckbRowColumnSize;
        private System.Windows.Forms.CheckBox ckbFontSize;
        private System.Windows.Forms.CheckBox ckbFontStyle;
        private System.Windows.Forms.CheckBox ckbFontColor;
        private System.Windows.Forms.CheckBox ckbFormat;
        private System.Windows.Forms.CheckBox ckbAlign;
        private System.Windows.Forms.CheckBox ckbFill;
        private System.Windows.Forms.CheckBox ckbAll;
        private System.Windows.Forms.CheckBox ckbEmptyCell;
        private System.Windows.Forms.CheckBox ckbMergeCells;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox ckbExplode;
        private System.Windows.Forms.CheckBox ckbImage;
    }
}