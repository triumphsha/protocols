namespace 智能电容器
{
    partial class frmAutoTest
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.styleManager1 = new DevComponents.DotNetBar.StyleManager(this.components);
            this.superTabControl1 = new DevComponents.DotNetBar.SuperTabControl();
            this.superTabControlPanel2 = new DevComponents.DotNetBar.SuperTabControlPanel();
            this.sgridmodbus = new DevComponents.DotNetBar.SuperGrid.SuperGridControl();
            this.gridColumn1 = new DevComponents.DotNetBar.SuperGrid.GridColumn();
            this.gridColumn2 = new DevComponents.DotNetBar.SuperGrid.GridColumn();
            this.gridColumn4 = new DevComponents.DotNetBar.SuperGrid.GridColumn();
            this.gridColumn8 = new DevComponents.DotNetBar.SuperGrid.GridColumn();
            this.gridColumn9 = new DevComponents.DotNetBar.SuperGrid.GridColumn();
            this.gridColumn3 = new DevComponents.DotNetBar.SuperGrid.GridColumn();
            this.gridRow1 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.gridCell1 = new DevComponents.DotNetBar.SuperGrid.GridCell();
            this.gridRow5 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.gridRow6 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.gridRow7 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.gridRow8 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.gridRow9 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.gridRow10 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.gridRow11 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.gridRow2 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.gridRow3 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.gridRow4 = new DevComponents.DotNetBar.SuperGrid.GridRow();
            this.superTabItem2 = new DevComponents.DotNetBar.SuperTabItem();
            this.superTabControlPanel1 = new DevComponents.DotNetBar.SuperTabControlPanel();
            this.dgvwModbus = new DevComponents.DotNetBar.Controls.DataGridViewX();
            this.columnDesc = new DevComponents.DotNetBar.Controls.DataGridViewLabelXColumn();
            this.Column8 = new System.Windows.Forms.DataGridViewButtonColumn();
            this.Column1 = new DevComponents.DotNetBar.Controls.DataGridViewLabelXColumn();
            this.Column5 = new DevComponents.DotNetBar.Controls.DataGridViewLabelXColumn();
            this.superTabItem1 = new DevComponents.DotNetBar.SuperTabItem();
            ((System.ComponentModel.ISupportInitialize)(this.superTabControl1)).BeginInit();
            this.superTabControl1.SuspendLayout();
            this.superTabControlPanel2.SuspendLayout();
            this.superTabControlPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvwModbus)).BeginInit();
            this.SuspendLayout();
            // 
            // styleManager1
            // 
            this.styleManager1.ManagerStyle = DevComponents.DotNetBar.eStyle.Office2010Black;
            this.styleManager1.MetroColorParameters = new DevComponents.DotNetBar.Metro.ColorTables.MetroColorGeneratorParameters(System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255))))), System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(87)))), ((int)(((byte)(154))))));
            // 
            // superTabControl1
            // 
            this.superTabControl1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            // 
            // 
            // 
            // 
            // 
            // 
            this.superTabControl1.ControlBox.CloseBox.Name = "";
            // 
            // 
            // 
            this.superTabControl1.ControlBox.MenuBox.Name = "";
            this.superTabControl1.ControlBox.Name = "";
            this.superTabControl1.ControlBox.SubItems.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.superTabControl1.ControlBox.MenuBox,
            this.superTabControl1.ControlBox.CloseBox});
            this.superTabControl1.Controls.Add(this.superTabControlPanel2);
            this.superTabControl1.Controls.Add(this.superTabControlPanel1);
            this.superTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControl1.ForeColor = System.Drawing.Color.Black;
            this.superTabControl1.Location = new System.Drawing.Point(0, 0);
            this.superTabControl1.Margin = new System.Windows.Forms.Padding(4);
            this.superTabControl1.Name = "superTabControl1";
            this.superTabControl1.ReorderTabsEnabled = true;
            this.superTabControl1.SelectedTabFont = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.superTabControl1.SelectedTabIndex = 0;
            this.superTabControl1.Size = new System.Drawing.Size(1682, 750);
            this.superTabControl1.TabFont = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.superTabControl1.TabIndex = 1;
            this.superTabControl1.Tabs.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.superTabItem1,
            this.superTabItem2});
            this.superTabControl1.TabStyle = DevComponents.DotNetBar.eSuperTabStyle.OneNote2007;
            this.superTabControl1.Text = "superTabControl1";
            // 
            // superTabControlPanel2
            // 
            this.superTabControlPanel2.Controls.Add(this.sgridmodbus);
            this.superTabControlPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControlPanel2.Location = new System.Drawing.Point(0, 35);
            this.superTabControlPanel2.Margin = new System.Windows.Forms.Padding(4);
            this.superTabControlPanel2.Name = "superTabControlPanel2";
            this.superTabControlPanel2.Size = new System.Drawing.Size(1682, 715);
            this.superTabControlPanel2.TabIndex = 0;
            this.superTabControlPanel2.TabItem = this.superTabItem2;
            // 
            // sgridmodbus
            // 
            this.sgridmodbus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.sgridmodbus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sgridmodbus.FilterExprColors.SysFunction = System.Drawing.Color.DarkRed;
            this.sgridmodbus.ForeColor = System.Drawing.Color.Black;
            this.sgridmodbus.Location = new System.Drawing.Point(0, 0);
            this.sgridmodbus.Margin = new System.Windows.Forms.Padding(4);
            this.sgridmodbus.Name = "sgridmodbus";
            // 
            // 
            // 
            this.sgridmodbus.PrimaryGrid.CheckBoxes = true;
            this.sgridmodbus.PrimaryGrid.ColumnHeaderClickBehavior = DevComponents.DotNetBar.SuperGrid.ColumnHeaderClickBehavior.None;
            this.sgridmodbus.PrimaryGrid.Columns.Add(this.gridColumn1);
            this.sgridmodbus.PrimaryGrid.Columns.Add(this.gridColumn2);
            this.sgridmodbus.PrimaryGrid.Columns.Add(this.gridColumn4);
            this.sgridmodbus.PrimaryGrid.Columns.Add(this.gridColumn8);
            this.sgridmodbus.PrimaryGrid.Columns.Add(this.gridColumn9);
            this.sgridmodbus.PrimaryGrid.Columns.Add(this.gridColumn3);
            this.sgridmodbus.PrimaryGrid.RowHeaderIndexOffset = 1;
            this.sgridmodbus.PrimaryGrid.Rows.Add(this.gridRow1);
            this.sgridmodbus.PrimaryGrid.Rows.Add(this.gridRow2);
            this.sgridmodbus.PrimaryGrid.Rows.Add(this.gridRow3);
            this.sgridmodbus.PrimaryGrid.Rows.Add(this.gridRow4);
            this.sgridmodbus.PrimaryGrid.ShowRowGridIndex = true;
            this.sgridmodbus.PrimaryGrid.ShowTreeButtons = true;
            this.sgridmodbus.PrimaryGrid.ShowTreeLines = true;
            this.sgridmodbus.PrimaryGrid.UseAlternateRowStyle = true;
            this.sgridmodbus.Size = new System.Drawing.Size(1682, 715);
            this.sgridmodbus.TabIndex = 0;
            this.sgridmodbus.Text = "superGridControl1";
            this.sgridmodbus.CellClick += new System.EventHandler<DevComponents.DotNetBar.SuperGrid.GridCellClickEventArgs>(this.sgridmodbus_CellClick);
            this.sgridmodbus.CellDoubleClick += new System.EventHandler<DevComponents.DotNetBar.SuperGrid.GridCellDoubleClickEventArgs>(this.sgridmodbus_CellDoubleClick);
            this.sgridmodbus.Click += new System.EventHandler(this.sgridmodbus_Click);
            this.sgridmodbus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.sgridmodbus_MouseDown);
            // 
            // gridColumn1
            // 
            this.gridColumn1.EditorType = typeof(DevComponents.DotNetBar.SuperGrid.GridLabelXEditControl);
            this.gridColumn1.HeaderText = "在网电容器信息";
            this.gridColumn1.InfoImageAlignment = DevComponents.DotNetBar.SuperGrid.Style.Alignment.MiddleLeft;
            this.gridColumn1.MinimumWidth = 200;
            this.gridColumn1.Name = "gridColumnDesc";
            this.gridColumn1.ReadOnly = true;
            this.gridColumn1.SortIndicator = DevComponents.DotNetBar.SuperGrid.SortIndicator.None;
            // 
            // gridColumn2
            // 
            this.gridColumn2.CellStyles.Default.Alignment = DevComponents.DotNetBar.SuperGrid.Style.Alignment.MiddleCenter;
            this.gridColumn2.EditorType = typeof(DevComponents.DotNetBar.SuperGrid.GridLabelXEditControl);
            this.gridColumn2.HeaderText = "测试状态";
            this.gridColumn2.InfoImageAlignment = DevComponents.DotNetBar.SuperGrid.Style.Alignment.MiddleCenter;
            this.gridColumn2.Name = "gridColumn2";
            this.gridColumn2.ReadOnly = true;
            this.gridColumn2.Width = 200;
            // 
            // gridColumn4
            // 
            this.gridColumn4.CellStyles.Default.Alignment = DevComponents.DotNetBar.SuperGrid.Style.Alignment.MiddleCenter;
            this.gridColumn4.HeaderText = "测试结果";
            this.gridColumn4.InfoImageAlignment = DevComponents.DotNetBar.SuperGrid.Style.Alignment.MiddleCenter;
            this.gridColumn4.Name = "gridColumn4";
            this.gridColumn4.Width = 300;
            // 
            // gridColumn8
            // 
            this.gridColumn8.EditorType = typeof(DevComponents.DotNetBar.SuperGrid.GridButtonXEditControl);
            this.gridColumn8.HeaderText = "容量测试";
            this.gridColumn8.Name = "gridColumn8";
            // 
            // gridColumn9
            // 
            this.gridColumn9.EditorType = typeof(DevComponents.DotNetBar.SuperGrid.GridButtonXEditControl);
            this.gridColumn9.HeaderText = "投切测试";
            this.gridColumn9.Name = "gridColumn9";
            // 
            // gridColumn3
            // 
            this.gridColumn3.EditorType = typeof(DevComponents.DotNetBar.SuperGrid.GridButtonXEditControl);
            this.gridColumn3.HeaderText = "整机检验";
            this.gridColumn3.Name = "gridColumn3";
            // 
            // gridRow1
            // 
            this.gridRow1.Cells.Add(this.gridCell1);
            this.gridRow1.Checked = true;
            this.gridRow1.Expanded = true;
            this.gridRow1.Rows.Add(this.gridRow5);
            this.gridRow1.Rows.Add(this.gridRow6);
            this.gridRow1.Rows.Add(this.gridRow7);
            this.gridRow1.Rows.Add(this.gridRow8);
            this.gridRow1.Rows.Add(this.gridRow9);
            this.gridRow1.Rows.Add(this.gridRow10);
            this.gridRow1.Rows.Add(this.gridRow11);
            // 
            // gridCell1
            // 
            this.gridCell1.InfoText = "";
            // 
            // superTabItem2
            // 
            this.superTabItem2.AttachedControl = this.superTabControlPanel2;
            this.superTabItem2.GlobalItem = false;
            this.superTabItem2.Name = "superTabItem2";
            this.superTabItem2.Text = "整机测试";
            // 
            // superTabControlPanel1
            // 
            this.superTabControlPanel1.Controls.Add(this.dgvwModbus);
            this.superTabControlPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControlPanel1.Location = new System.Drawing.Point(0, 35);
            this.superTabControlPanel1.Margin = new System.Windows.Forms.Padding(4);
            this.superTabControlPanel1.Name = "superTabControlPanel1";
            this.superTabControlPanel1.Size = new System.Drawing.Size(1682, 715);
            this.superTabControlPanel1.TabIndex = 1;
            this.superTabControlPanel1.TabItem = this.superTabItem1;
            // 
            // dgvwModbus
            // 
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvwModbus.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvwModbus.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvwModbus.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.columnDesc,
            this.Column8,
            this.Column1,
            this.Column5});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvwModbus.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvwModbus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvwModbus.EnableHeadersVisualStyles = false;
            this.dgvwModbus.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
            this.dgvwModbus.Location = new System.Drawing.Point(0, 0);
            this.dgvwModbus.Margin = new System.Windows.Forms.Padding(4);
            this.dgvwModbus.Name = "dgvwModbus";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvwModbus.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dgvwModbus.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvwModbus.RowTemplate.Height = 23;
            this.dgvwModbus.Size = new System.Drawing.Size(1682, 715);
            this.dgvwModbus.TabIndex = 0;
            this.dgvwModbus.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvwModbus_CellContentClick);
            this.dgvwModbus.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvwModbus_CellDoubleClick);
            // 
            // columnDesc
            // 
            this.columnDesc.HeaderText = "测试项描述";
            this.columnDesc.Name = "columnDesc";
            this.columnDesc.TextAlignment = System.Drawing.StringAlignment.Center;
            // 
            // Column8
            // 
            this.Column8.HeaderText = "开始测试";
            this.Column8.Name = "Column8";
            this.Column8.ReadOnly = true;
            // 
            // Column1
            // 
            this.Column1.HeaderText = "测试状态";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            this.Column1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Column1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Column1.Text = "准备测试";
            this.Column1.Width = 300;
            // 
            // Column5
            // 
            this.Column5.HeaderText = "测试结果";
            this.Column5.Name = "Column5";
            this.Column5.ReadOnly = true;
            this.Column5.Width = 300;
            // 
            // superTabItem1
            // 
            this.superTabItem1.AttachedControl = this.superTabControlPanel1;
            this.superTabItem1.GlobalItem = false;
            this.superTabItem1.Name = "superTabItem1";
            this.superTabItem1.Text = "单板测试";
            // 
            // frmAutoTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1682, 750);
            this.Controls.Add(this.superTabControl1);
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.Color.Black;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "frmAutoTest";
            this.Text = "自动化测试";
            ((System.ComponentModel.ISupportInitialize)(this.superTabControl1)).EndInit();
            this.superTabControl1.ResumeLayout(false);
            this.superTabControlPanel2.ResumeLayout(false);
            this.superTabControlPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvwModbus)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevComponents.DotNetBar.StyleManager styleManager1;
        private DevComponents.DotNetBar.SuperTabControl superTabControl1;
        private DevComponents.DotNetBar.SuperTabControlPanel superTabControlPanel1;
        private DevComponents.DotNetBar.Controls.DataGridViewX dgvwModbus;
        private DevComponents.DotNetBar.SuperTabItem superTabItem1;
        private DevComponents.DotNetBar.SuperTabControlPanel superTabControlPanel2;
        private DevComponents.DotNetBar.SuperGrid.SuperGridControl sgridmodbus;
        private DevComponents.DotNetBar.SuperGrid.GridColumn gridColumn1;
        private DevComponents.DotNetBar.SuperGrid.GridColumn gridColumn2;
        private DevComponents.DotNetBar.SuperGrid.GridColumn gridColumn4;
        private DevComponents.DotNetBar.SuperGrid.GridColumn gridColumn8;
        private DevComponents.DotNetBar.SuperGrid.GridColumn gridColumn9;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow1;
        private DevComponents.DotNetBar.SuperGrid.GridCell gridCell1;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow5;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow6;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow7;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow8;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow9;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow10;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow11;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow2;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow3;
        private DevComponents.DotNetBar.SuperGrid.GridRow gridRow4;
        private DevComponents.DotNetBar.SuperTabItem superTabItem2;
        private DevComponents.DotNetBar.Controls.DataGridViewLabelXColumn columnDesc;
        private System.Windows.Forms.DataGridViewButtonColumn Column8;
        private DevComponents.DotNetBar.Controls.DataGridViewLabelXColumn Column1;
        private DevComponents.DotNetBar.Controls.DataGridViewLabelXColumn Column5;
        private DevComponents.DotNetBar.SuperGrid.GridColumn gridColumn3;
    }
}