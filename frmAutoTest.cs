using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevComponents.DotNetBar.SuperGrid;
using System.Collections;
using System.Threading;

namespace 智能电容器
{    
    public partial class frmAutoTest : DevComponents.DotNetBar.Metro.MetroForm
    {
        private System.Threading.Timer mTimer;

        private const int TEST_STATION_MAX = 10;
        private const int TEST_TIMES_MAX = 10;
        private const int SUCC_TIMES_MAX = 3;
        private const int START_DELAY_MAX = 5;
        private int mTestTimes = TEST_TIMES_MAX;

        private int mTestStartDelay = START_DELAY_MAX;

        private int mIndex = 0;
        private bool mShowFlag = false;
        private int mShowIndex = 0;
        
        public static int mTestCount = 0;

        private int mTestType = 0;

        private CFlowCtrl mFlowCtrl = null;

        private bool mCompareDelayFlag = false;

        private bool mStartTestFlag = false;   // 测试开始

        //private List<CPutInOutRec> mPutInOutRec;

        public static Dictionary<string, CPutInOutRec> mDicPutInOutRec;

        delegate void SetFlowCtrlStateCallback(StateEventArgs e);

        private frmShow mFrmShow;
        public static bool mCheckFlag = false;

        public frmAutoTest()
        {
            InitializeComponent();

            InitializeData();

            CGlobalCtrl.AuotTestFrom = this;

            if ((CGlobalCtrl.Authority & 0x30) == 0)
            {
                superTabControl1.Tabs[0].Enabled = false;
            }
            if((CGlobalCtrl.Authority & 0xC0) == 0)
                superTabControl1.Tabs[1].Enabled = false;

            //mPutInOutRec = new List<智能电容器.CPutInOutRec>();

            mDicPutInOutRec = new Dictionary<string, 智能电容器.CPutInOutRec>();

            // 初始化
            for (int i = 0; i < CGlobalCtrl.MAX_VARNODE_NUM; i++)
            {
                CGlobalCtrl.SnToAddressTable[i].address = 0;// i+1;// i+1;
                CGlobalCtrl.SnToAddressTable[i].serail = new Byte[6];
                CGlobalCtrl.SnToAddressTable[i].serail[0] = 0x3f;
                CGlobalCtrl.SnToAddressTable[i].serail[1] = 0x20;
                CGlobalCtrl.SnToAddressTable[i].serail[2] = 0x18;
                CGlobalCtrl.SnToAddressTable[i].serail[3] = 0x01;
                CGlobalCtrl.SnToAddressTable[i].serail[4] = 0x00;
                CGlobalCtrl.SnToAddressTable[i].serail[5] = Convert.ToByte((i+1).ToString(),16);
                CGlobalCtrl.SnToAddressTable[i].validflag = CNetWork.SN_VALID;
            }

            FlashShow(1);
        }

        private void sgridmodbus_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void InitializeData()
        {
            string[] orderArray = { "Asterids", "Eudicots", "Rosids" };
            
            dgvwModbus.Rows.Clear();

            dgvwModbus.Rows.Add("设置SN编号", "开始测试", "准备测试", "自动生成");
            dgvwModbus.Rows.Add("校表投切测试", "开始测试", "准备测试", "读取SN号");

            GridPanel SGridPanel = sgridmodbus.PrimaryGrid;
            SGridPanel.Rows.Clear();
            ((GridButtonXEditControl)SGridPanel.Columns["gridColumn8"].EditControl).Click += FrmAutoTest_Click;
            ((GridButtonXEditControl)SGridPanel.Columns["gridColumn9"].EditControl).Click += FrmAutoTest_Click;
            ((GridButtonXEditControl)SGridPanel.Columns["gridColumn3"].EditControl).Click += FrmAutoTest_Click;
        }

        public void FlashShow(int type)
        {
            int index = 0;
            sgridmodbus.PrimaryGrid.Rows.Clear();

            while (index < (CGlobalCtrl.SnToAddressTable.Length > TEST_STATION_MAX ? TEST_STATION_MAX : CGlobalCtrl.SnToAddressTable.Length))
            {
                if (CGlobalCtrl.SnToAddressTable[index].validflag == CNetWork.SN_VALID)
                {
                    GridRow gridRow = new GridRow(CFunc.BytesToHexStr(CGlobalCtrl.SnToAddressTable[index].serail, 6), "准备测试", "", "容量测试", "投切测试","整机验证");

                    gridRow.Tag = CGlobalCtrl.SnToAddressTable[index].address.ToString();
                    gridRow.Checked = true;
                    sgridmodbus.PrimaryGrid.Rows.Add(gridRow);
                }
                index++;
            }
        }
        public void TimerRun_Check(object obj)
        {
            //GridRow

            if (mIndex >= TEST_STATION_MAX || mIndex >= sgridmodbus.PrimaryGrid.Rows.Count)
            {

                mTestCount++;
                if (mTestCount < mTestTimes)//mTestTimes = TEST_TIMES_MAX
                {
                    mIndex = 0;
                    mFlowCtrl = null;
                }
                else
                {
                    mIndex = 0;
                    mFlowCtrl = null;
                    mTestCount = 0;
                    mTimer.Dispose();
                    mCheckFlag = false;
                    mStartTestFlag = false;
                    FreshOverFlag(mTestType);
                    return;
                }
            }


            if (mFlowCtrl == null || mFlowCtrl.FlowCtrlState == EnumFlowCtrlState.FLOWCTRL_STOP)
            {

                // 矫正投入延时，最多调整5次，记录调整过程参数值，选择最优参数作为运行参数
                if (mFlowCtrl != null)
                {
                    // 记录结果                            
                    // CPutInOutRec PutInOutRec = new CPutInOutRec();
                    mFlowCtrl.FillPutInOutRec(mDicPutInOutRec, 2);

                    //GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mIndex];
                    //if (!mDicPutInOutRec[GetRow.Cells[0].Value.ToString().Trim()].NeedAdjust)
                    //{
                    //    GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mIndex];
                    //    GetRow.Checked = false;
                    //}
                    mIndex++;
                }

                if (mIndex >= TEST_STATION_MAX || mIndex >= sgridmodbus.PrimaryGrid.Rows.Count) return;

                GridRow GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mIndex];
                if (GetRow.Checked)
                    mFlowCtrl = new CFlowCtrl("//testflow//putinoutcheck", (byte)CGlobalCtrl.SnToAddressTable[mIndex].address, 0, "整机投切验证", 25);
                else
                {
                    //if (mFlowCtrl == null)
                    mFlowCtrl = null;
                    mIndex++;
                    return;
                }

                //GridRow GetRow;
                mFlowCtrl.StateEvent += FlowCtrl_StateEvent;
                mShowIndex = CGlobalCtrl.SnToAddressTable[mIndex].address - 1;  // triumph_sha change by 2018/2/8
                if (mShowIndex >= 0)
                {
                    GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mShowIndex];
                    GetRow.Cells[1].Value = "准备测试";
                }
                mFlowCtrl.Start();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public void TimerRun_SetDelay(object obj)
        {
            if (mIndex >= mDicPutInOutRec.Count)
            {
                mIndex = 0;
                mFlowCtrl = null;
                mShowFlag = false;
                mTimer.Dispose();
                //mStartTestFlag = false;
                //FreshOverFlag(mTestType);

                // 进一步验证
                mDicPutInOutRec.Clear();
                mTestCount = 0;
                mTestTimes = 3;

                // 重新勾选,全部验证
                //GridRow GetRow;
                //for (int i = 0; i < sgridmodbus.PrimaryGrid.Rows.Count; i++)
                //{
                //    GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mIndex];
                //    GetRow.Checked = true;
                //}
                mCheckFlag = true;
                mTimer = new System.Threading.Timer(TimerRun_Check, null, 0, 1000);

                return;
            }

            if (mFlowCtrl == null || mFlowCtrl.FlowCtrlState == EnumFlowCtrlState.FLOWCTRL_STOP)
            {

                if (mFlowCtrl != null)
                    mIndex++;
                if (mIndex >= mDicPutInOutRec.Count) return;
                // 具备合格测试记录，并没有调整到目标值的情况选择最小涌流的投入时间设置
                if (mDicPutInOutRec.ElementAt(mIndex).Value.Result && mDicPutInOutRec.ElementAt(mIndex).Value.NeedAdjust)
            {
                    if (mIndex >= mDicPutInOutRec.Count) return;

                    mFlowCtrl = new CFlowCtrl("//testflow//adjustdelay", mDicPutInOutRec.ElementAt(mIndex).Value.Address, 0, "电容延时设置", 18);
                    mFlowCtrl.StateEvent += FlowCtrl_StateEvent;
                    mFlowCtrl.AddCondition(mDicPutInOutRec.ElementAt(mIndex).Value);
                    mShowFlag = true;
                    mShowIndex = mDicPutInOutRec.ElementAt(mIndex).Value.Address - 1;
                    GridRow GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mShowIndex];
                    GetRow.Cells[1].Value = "准备测试";
                    mFlowCtrl.Start();
               
            }
            else if (!mDicPutInOutRec.ElementAt(mIndex).Value.Result)
            {
                mShowIndex = mDicPutInOutRec.ElementAt(mIndex).Value.Address - 1;
                GridRow GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mShowIndex];
                GetRow.Checked = false;
                mIndex++;
            }
            else
            {
                    mShowIndex = mDicPutInOutRec.ElementAt(mIndex).Value.Address - 1;
                    GridRow GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mShowIndex];
                GetRow.Checked = true;
                mIndex++;
            }
            }
            /*
            if (mIndex >= mDicPutInOutRec.Count)
            {
                mIndex = 0;
                mFlowCtrl = null;
                mShowFlag = false;
                //((GridButtonXEditControl)sgridmodbus.PrimaryGrid.Columns["gridColumn8"].EditControl).Enabled = true;
                //((GridButtonXEditControl)sgridmodbus.PrimaryGrid.Columns["gridColumn9"].EditControl).Enabled = true;
                mTimer.Dispose();

                if (!mCompareDelayFlag)
                {
                    mFlowCtrl = null;
                    mIndex = 0;
                    mTestType = 2;
                    mDicPutInOutRec.Clear();
                    mCompareDelayFlag = true;
                    //mTestTimes = 2;
                    //((GridButtonXEditControl)sgridmodbus.PrimaryGrid.Columns["gridColumn8"].EditControl).Enabled = false;
                    //((GridButtonXEditControl)sgridmodbus.PrimaryGrid.Columns["gridColumn9"].EditControl).Enabled = false;
                    mTimer = new System.Threading.Timer(TimerRun, null, 0, 1000);
                }
                else
                {
                    mStartTestFlag = false;
                    FreshOverFlag(mTestType);
                }
                return;
            }

            
            if (mDicPutInOutRec.ElementAt(mIndex).Value.Times >= SUCC_TIMES_MAX)
            {
                if (mFlowCtrl == null || mFlowCtrl.FlowCtrlState == EnumFlowCtrlState.FLOWCTRL_STOP)
                {
                    if (mFlowCtrl != null)
                        mIndex++;

                    if (mIndex >= mDicPutInOutRec.Count) return;

                    if (mCompareDelayFlag)
                        mFlowCtrl = new CFlowCtrl("//testflow//comparedelay", mDicPutInOutRec.ElementAt(mIndex).Value.Address, 0, "电容延时判断", 18);
                    else
                        mFlowCtrl = new CFlowCtrl("//testflow//adjustdelay", mDicPutInOutRec.ElementAt(mIndex).Value.Address, 0, "电容延时设置", 18);
                    mFlowCtrl.StateEvent += FlowCtrl_StateEvent;
                    mFlowCtrl.AddCondition(mDicPutInOutRec.ElementAt(mIndex).Value);
                    mShowFlag = true;
                    mShowIndex = mDicPutInOutRec.ElementAt(mIndex).Value.Address - 1;
                    GridRow GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mShowIndex];
                    GetRow.Cells[1].Value = "准备测试";
                    mFlowCtrl.Start();                       
                }
            }
            else
            {
                mIndex++;
            }*/



        }

        public static CPutInOutRec GetPutRecBySn(string v)
        {
            //throw new NotImplementedException();

            if (mDicPutInOutRec.ContainsKey(v))
            {
                CPutInOutRec tmpRec = mDicPutInOutRec[v];

                return tmpRec;
            }

            return null;
        }

        public void TimerRun(object obj)
        {
            GridRow GetRow;
            //
            if (mTestStartDelay > 0)
            {
                mTestStartDelay--;
                if (sgridmodbus.PrimaryGrid.Rows.Count() > 0)
                {
                    GridRow gridRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[0];
                    gridRow.Cells[1].Value = "等待延时";
                    gridRow.Cells[2].Value = mTestStartDelay.ToString() + "秒"; //Convert.ToInt32(msgvalues[1]) +
                }

                return;
            }


            if (mIndex >= TEST_STATION_MAX || mIndex >= sgridmodbus.PrimaryGrid.Rows.Count)
            {
                if (mTestType == 1)
                {
                    mTimer.Dispose();
                    mStartTestFlag = false;
                    FreshOverFlag(mTestType);
                    //((GridButtonXEditControl)sgridmodbus.PrimaryGrid.Columns["gridColumn8"].EditControl).Enabled = true;
                    //((GridButtonXEditControl)sgridmodbus.PrimaryGrid.Columns["gridColumn9"].EditControl).Enabled = true;
                    return;
                }
                if (mTestType == 2)
                {
                    mTestCount++;
                    if (mTestCount < mTestTimes)//mTestTimes = TEST_TIMES_MAX
                    {
                        mIndex = 0;
                        mFlowCtrl = null;
                    }
                    else
                    {
                        mIndex = 0;
                        mFlowCtrl = null;
                        mTestCount = 0;
                        mTimer.Dispose();
                        mTimer = new System.Threading.Timer(TimerRun_SetDelay, null, 0, 1000);
                        return;
                    }
                }
            }

            if (mFlowCtrl == null || mFlowCtrl.FlowCtrlState == EnumFlowCtrlState.FLOWCTRL_STOP)
            {
                if (mTestType == 1)
                {
                    if (mFlowCtrl != null)
                        mIndex++;

                    if (mIndex >= TEST_STATION_MAX || mIndex >= sgridmodbus.PrimaryGrid.Rows.Count) return;
                    GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mIndex];
                    if (GetRow.Checked)
                        mFlowCtrl = new CFlowCtrl("//testflow//setcapacity", (byte)CGlobalCtrl.SnToAddressTable[mIndex].address, 0, "电容容量测试", 25);
                    else
                    {
                        if (mFlowCtrl == null)
                            mIndex++;
                        return;
                    }
                }
                else if (mTestType == 2)
                {
                    // 矫正投入延时，最多调整5次，记录调整过程参数值，选择最优参数作为运行参数
                    if (mFlowCtrl != null)
                    {
                        // 记录结果                            
                        // CPutInOutRec PutInOutRec = new CPutInOutRec();
                        mFlowCtrl.FillPutInOutRec(mDicPutInOutRec);

                        GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mIndex];
                        if (!mDicPutInOutRec[GetRow.Cells[0].Value.ToString().Trim()].NeedAdjust)
                        {
                            GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mIndex];
                            GetRow.Checked = false;
                        }
                        mIndex++;
                    }

                    if (mIndex >= TEST_STATION_MAX || mIndex >= sgridmodbus.PrimaryGrid.Rows.Count) return;

                    GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mIndex];
                    if (GetRow.Checked)
                        mFlowCtrl = new CFlowCtrl("//testflow//putinout", (byte)CGlobalCtrl.SnToAddressTable[mIndex].address, 0, "整机投切测试", 25);
                    else
                    {
                        //if (mFlowCtrl == null)
                        mFlowCtrl = null;
                        mIndex++;
                        return;
                    }
                }

                mFlowCtrl.StateEvent += FlowCtrl_StateEvent;
                mShowIndex = CGlobalCtrl.SnToAddressTable[mIndex].address - 1;  // triumph_sha change by 2018/2/8
                if(mShowIndex >= 0)
                { 
                    GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mShowIndex];
                    GetRow.Cells[1].Value = "准备测试";
                }
                mFlowCtrl.Start();
            }
        }

        private void FreshOverFlag(int mTestType)
        {
            int i = 0;
            GridRow gridRow;// = (GridRow)sgridmodbus.PrimaryGrid.Rows[mShowIndex];
            if (mTestType == 1)
            {
                for (i = 0; i < sgridmodbus.PrimaryGrid.Rows.Count(); i++)
                {
                    gridRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[i];
                    if (gridRow.Checked)
                        gridRow.Cells[1].Value = "电容容量测试-结束";
                }
            }
            else if (mTestType == 2)
            {

                for (i = 0; i < sgridmodbus.PrimaryGrid.Rows.Count(); i++)
                {
                    gridRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[i];
                    gridRow.Cells[1].Value = "整机投切测试-结束";
                }
                for (i = 0; i < mDicPutInOutRec.Count(); i++)
                {
                    gridRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mDicPutInOutRec.ElementAt(i).Value.Address - 1];
                    if (mDicPutInOutRec.ElementAt(i).Value.Result2)
                    {
                        gridRow.Cells[2].Value = "测试合格";
                        gridRow.Cells[2].CellStyles.Default.Background.Color1 = Color.White;
                    }
                    else
                    {
                        gridRow.Cells[2].Value = "测试不合格";
                        gridRow.Cells[2].CellStyles.Default.Background.Color1 = Color.Red;
                    }
                }
            }
            else if (mTestType == 22)
            {
                for (i = 0; i < sgridmodbus.PrimaryGrid.Rows.Count(); i++)
                {
                    gridRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[i];
                    if (gridRow.Checked)
                    {
                        if (!mDicPutInOutRec.ContainsKey(gridRow.Cells[0].Value.ToString().Trim()))
                        {
                            gridRow.Checked = false;
                            gridRow.Cells[2].Value = "整机投切合格次数不满足";
                        }
                        else
                        {
                            if (mDicPutInOutRec[gridRow.Cells[0].Value.ToString().Trim()].Times < SUCC_TIMES_MAX)
                            {
                                gridRow.Checked = false;
                                gridRow.Cells[2].Value = "整机投切合格次数不满足";
                            }
                        }
                    }
                }
            }
        }

        private void FrmAutoTest_Click(object sender, EventArgs e)
        {
            
            GridButtonXEditControl btn = (GridButtonXEditControl)sender;
            if (mStartTestFlag) return;
            if (btn.EditorCell.ColumnIndex == 3)
            {
                mFlowCtrl = null;
                mIndex = 0;
                mTestType = 1;
                mCompareDelayFlag = false;
                mStartTestFlag = true;
                mTestStartDelay = START_DELAY_MAX;
                ((GridButtonXEditControl)sgridmodbus.PrimaryGrid.Columns["gridColumn8"].EditControl).Enabled = false;
                ((GridButtonXEditControl)sgridmodbus.PrimaryGrid.Columns["gridColumn9"].EditControl).Enabled = false;
                mTimer = new System.Threading.Timer(TimerRun, null, 0, 1000);
            }
            else if (btn.EditorCell.ColumnIndex == 4)
            {
                mFlowCtrl = null;
                mIndex = 0;
                mTestType = 2;
                mDicPutInOutRec.Clear();
                CGlobalCtrl.gFirstPutInFlag.Clear();
                mCompareDelayFlag = false;
                mStartTestFlag = true;
                mTestTimes = TEST_TIMES_MAX;
                mTestCount = 0;  // 
                mTestStartDelay = START_DELAY_MAX;
                ((GridButtonXEditControl)sgridmodbus.PrimaryGrid.Columns["gridColumn8"].EditControl).Enabled = false;
                ((GridButtonXEditControl)sgridmodbus.PrimaryGrid.Columns["gridColumn9"].EditControl).Enabled = false;

                // 测试后,重新勾选
                GridRow GetRow;
                for (int i = 0; i < sgridmodbus.PrimaryGrid.Rows.Count; i++)
                {
                    GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mIndex];
                    GetRow.Checked = true;
                }

                mTimer = new System.Threading.Timer(TimerRun, null, 0, 1000);
            }
            else if (btn.EditorCell.ColumnIndex == 5)
            {
                mIndex = 0;
                mFlowCtrl = null;
                mShowFlag = false;
                mTestType = 2;
                //mTimer.Dispose();
                mStartTestFlag = true;
                
                // 进一步验证
                mDicPutInOutRec.Clear();
                mTestCount = 0;
                mTestTimes = 3;

                // 重新勾选,全部验证
                //GridRow GetRow;
                //for (int i = 0; i < sgridmodbus.PrimaryGrid.Rows.Count; i++)
                //{
                //    GetRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mIndex];
                //    GetRow.Checked = true;
                //}
                mCheckFlag = true;
                mTimer = new System.Threading.Timer(TimerRun_Check, null, 0, 1000);
            }
            }
                
        /// <summary>
        /// 
        /// </summary>
        private void startsetsn()
        {            
            CFlowCtrl FlowCtrl = new CFlowCtrl("//testflow//setsn", 0, 0, "电容SN设置", 18);

            FlowCtrl.StateEvent += FlowCtrl_StateEvent;
            
            dgvwModbus.Rows[0].Cells[2].Value = "准备测试";

            FlowCtrl.Start();

            mTestType = 1;
        }
        
        private void SetGridModbus(StateEventArgs args)
        {
            //string[] msgvalues;

            //if (dgvwModbus.InvokeRequired)
            //{
            //    SetFlowCtrlStateCallback d = new SetFlowCtrlStateCallback(SetGridModbus);
            //    this.Invoke(d, new object[] { args });
            //}
            //else
            //{
            //    msgvalues = args.Message.Split(',');
            //    if (mIndex < sgridmodbus.PrimaryGrid.Rows.Count())
            //    {
            //        GridRow gridRow;
            //        if (mShowFlag)
            //            gridRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mShowIndex];
            //        else
            //            gridRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[mShowIndex];
            //        gridRow.Cells[1].Value = msgvalues[0];
            //        gridRow.Cells[2].Value = msgvalues[3]; //Convert.ToInt32(msgvalues[1]) +
            //    }
            //}
            int showindex = 0;
            string[] msgvalues;

            if (dgvwModbus.InvokeRequired)
            {
                SetFlowCtrlStateCallback d = new SetFlowCtrlStateCallback(SetGridModbus);
                this.Invoke(d, new object[] { args });
            }
            else
            {
                msgvalues = args.Message.Split(',');
                showindex = Convert.ToInt32(msgvalues[4].Trim()) - 1;
                if ((showindex < sgridmodbus.PrimaryGrid.Rows.Count()) && (showindex >= 0))
                {
                    GridRow gridRow;
                    if (mShowFlag)
                        gridRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[showindex];
                    else
                        gridRow = (GridRow)sgridmodbus.PrimaryGrid.Rows[showindex];
                    gridRow.Cells[1].Value = msgvalues[0];
                    gridRow.Cells[2].Value = msgvalues[3];
                    if (msgvalues[3] == "测试不合格")
                        gridRow.Cells[2].CellStyles.Default.Background.Color1 = Color.Red;
                    else
                        gridRow.Cells[2].CellStyles.Default.Background.Color1 = Color.White;
                    //Convert.ToInt32(msgvalues[1]) +
                }
            }
        }
        private void SetGrvwModbus(StateEventArgs args)
        {
            string[] msgvalues;
            try
            {
                // e.CommNum
                if (dgvwModbus.InvokeRequired)
                {
                    SetFlowCtrlStateCallback d = new SetFlowCtrlStateCallback(SetGrvwModbus);
                    this.Invoke(d, new object[] { args });

                }
                else
                {
                    msgvalues = args.Message.Split(',');
                    dgvwModbus.Rows[mTestType - 1].Cells[2].Value = msgvalues[0];
                    dgvwModbus.Rows[mTestType - 1].Cells[3].Value = msgvalues[3];
                    if (msgvalues[3] == "测试不合格")
                        dgvwModbus.Rows[mTestType - 1].Cells[3].Style.BackColor = Color.Red;// .Default.Background.Color1 = Color.Red;
                    else
                        dgvwModbus.Rows[mTestType - 1].Cells[3].Style.BackColor = Color.White;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误");
            }
        }

        private void FlowCtrl_StateEvent(object sender, StateEventArgs e)
        {
            CFlowCtrl FlowCtrl = (CFlowCtrl)sender;
            if (FlowCtrl.Name.Contains("setsn") || FlowCtrl.Name.Contains("putcheck"))
                SetGrvwModbus(e);
            else
                SetGridModbus(e);
        }

        private void dgvwModbus_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex > dgvwModbus.RowCount - 2) return;
            // 规约数据项选择下发
            if (e.ColumnIndex == 1)
            {
                if (e.RowIndex == 0)
                {
                    if ((CGlobalCtrl.Authority & 0x10) > 0)
                    {
                        startsetsn();
                    }
                    else
                        MessageBox.Show("不能这样做......","告警");
                } 
                else if(e.RowIndex == 1)
                    {
                    if ((CGlobalCtrl.Authority & 0x20) > 0)
                    {
                        startputcheck();
                    }
                    else
                        MessageBox.Show("不能这样做......", "告警");
                }             
            }
        }

        private void startputcheck()
        {
            CFlowCtrl FlowCtrl = new CFlowCtrl("//testflow//putcheck", 0, 0,"校准测试",25);
            //CFlowCtrl FlowCtrl = new CFlowCtrl("//testflow//adjustdelay", 0, 0, "调整延时参数", 25);
            //FlowCtrl.AddCondition(new CPutInOutRec(5000, 5000, 5000, 5000, 3000, 3000, 3000, 3000));
            FlowCtrl.StateEvent += FlowCtrl_StateEvent;

            dgvwModbus.Rows[1].Cells[2].Value = "准备测试";

            FlowCtrl.Start();

            mTestType = 2;
        }

        private void sgridmodbus_Click(object sender, EventArgs e)
        {

        }
        

        private void sgridmodbus_CellClick(object sender, GridCellClickEventArgs e)
        {
            
        }

        private void dgvwModbus_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex > dgvwModbus.RowCount - 2) return;
            // 规约数据项选择下发
            if (e.ColumnIndex >= 2)
            {
                if (e.RowIndex == 0)
                {
                    if ((CGlobalCtrl.Authority & 0x10) > 0)
                    {
                        // 显示内容
                        mFrmShow = new frmShow();
                        mFrmShow.MsgFlash("电容SN设置", null,1);
                        mFrmShow.ShowDialog();
                    }
                    else
                        MessageBox.Show("不能这样做......", "告警");
                }
                else if (e.RowIndex == 1)
                {
                    //startputcheck();
                    // 显示内容
                    mFrmShow = new frmShow();
                    mFrmShow.MsgFlash("校准测试", null,1);
                    mFrmShow.StartPosition = FormStartPosition.CenterParent;
                    mFrmShow.ShowDialog();
                }
            }
        }

        private void sgridmodbus_CellDoubleClick(object sender, GridCellDoubleClickEventArgs e)
        {
            if (e.GridCell.ColumnIndex == 1 || e.GridCell.ColumnIndex == 2)
            {
                mFrmShow = new frmShow();
                GridRow row = e.GridCell.GridRow;
                if (e.GridCell.ColumnIndex == 1)
                    mFrmShow.MsgFlash(null, row.Cells[0].Value.ToString().Trim(), 1);
                else
                    mFrmShow.MsgFlash(null, row.Cells[0].Value.ToString().Trim(), 1);

                mFrmShow.StartPosition = FormStartPosition.CenterParent;
                mFrmShow.ShowDialog();
            }
        }
    }
    /// <summary>
    /// FlowerButtonA 类
    /// </summary>
    internal class FlowerButtonA : GridButtonXEditControl
    {
        #region Private variables

        //private ImageList _FlowerImageList;

        #endregion

        public FlowerButtonA(IEnumerable orderarray)
        {
            //_FlowerImageList = flowerImageList;

            //Click += FlowerButtonClick;
        }

        #region FlowerButtonClick

        void FlowerButtonClick(object sender, EventArgs e)
        {
            string buttentext = EditorCell.Value as string;


            Byte[] sendbuf = new Byte[256];
            //int sendlen;
            //UInt16 datalen = 0, subdatalen = 0;
            //UInt16 regaddr = 0, regnum = 0;
            //string datastr;
            //string datatypestr;
            //Byte[] temp;
            //FrameHeadStr FrameHead = new FrameHeadStr();
            int RowIndex = EditorCell.RowIndex;
            int ColumnIndex = EditorCell.ColumnIndex;

            //if (e.RowIndex < 0 || e.RowIndex > dgvwModbus.RowCount - 2) return;
            // 规约数据项选择下发
            if (ColumnIndex == 8 || ColumnIndex == 9)
            {
                // 开始组网
                // CGlobalCtrl.MainForm.NetWorkStart();

                if (ColumnIndex == 8)
                {
                    //CFlowCtrl FlowCtrl = new CFlowCtrl("//testflow//setcapacity", 0, CGlobalCtrl.CommNum);

                    //FlowCtrl.StateEvent += FlowCtrl_StateEvent;

                    //dgvwModbus.Rows[0].Cells[2].Value = "准备测试";

                   // FlowCtrl.Start();

                    //mTestType = 1;

                }

                GridRow GetRow = EditorCell.GridRow;
                if (GetRow.Cells[ColumnIndex].Value.ToString().Trim() == "")
                {
                    //CDebugInfo.writelogctrl(null, "命令不能为空", 0);
                    MessageBox.Show(this, "命令不能为空", "警告");
                    return;
                }

                /*for (int i = 0; i < GetRow.Rows.Count; i++)
                {
                    GridRow GetSubRow = (GridRow)GetRow.Rows[i];

                    if (GetSubRow.Checked)
                    {
                        if (regaddr == 0)
                        {
                            regaddr = Convert.ToUInt16(GetSubRow.Cells[1].Value.ToString().Trim(), 16);
                        }


                        subdatalen = (UInt16)(Convert.ToUInt16(GetSubRow.Cells[3].Value));
                        if (ColumnIndex == 7)
                        {
                            datatypestr = GetSubRow.Cells[2].Value.ToString().Trim();
                            datastr = GetSubRow.Cells[4].Value.ToString().Trim();
                            frmModbus.FillGuiValue(out FrameHead.values, datastr, datatypestr, subdatalen);
                            Array.Copy(FrameHead.values, 0, sendbuf, datalen, subdatalen);
                        }
                        datalen += subdatalen;
                    }
                    else
                    {
                        if (regaddr > 0)
                            break;

                    }
                }

                // 数据装载
                Array.Resize<Byte>(ref FrameHead.values, datalen);
                Array.Copy(sendbuf, FrameHead.values, datalen);

                // 地址
                FrameHead.address = (Byte)CGlobalCtrl.ProtocolAddress;
                // 功能码
                FrameHead.funcode = Convert.ToByte(GetRow.Cells[ColumnIndex].Value.ToString().Trim(), 16);

                // 寄存器地址
                temp = CFunc.u16memcpy(regaddr);
                FrameHead.affirmstr.startlw = temp[0];
                FrameHead.affirmstr.starthi = temp[1];

                // 寄存器数量
                if (FrameHead.funcode == 0x0f || FrameHead.funcode == 0x01)
                {
                    regnum = datalen;
                }
                else
                {
                    regnum = (UInt16)(datalen / 2);
                }
                temp = CFunc.u16memcpy(regnum);
                FrameHead.affirmstr.amountlw = temp[0];
                FrameHead.affirmstr.amounthi = temp[1];

                if (ColumnIndex == 7)  // 设置
                {
                    if (FrameHead.funcode == 0x0f)
                    {
                        if (regnum == 1)
                            FrameHead.funcode = 0x05;
                        else
                        {
                            datalen = (UInt16)CFunc.bitstobytes(FrameHead.values, sendbuf);
                            Array.Resize<Byte>(ref FrameHead.values, datalen);
                            Array.Copy(sendbuf, FrameHead.values, datalen);
                        }
                    }
                    else if (FrameHead.funcode == 0x10)
                    {
                        if (regnum == 1)
                            FrameHead.funcode = 0x06;
                    }
                }
                else if (ColumnIndex == 8) // 查询
                {

                }

                CCommFrame CommFrame = new CCommFrame();
                sendlen = CGlobalCtrl.Modbushost.makeframe(ref FrameHead, sendbuf, false);
                CommFrame.FillContent(sendbuf, sendlen, CGlobalCtrl.CommNum);
                CommFrame.ShowIndex = RowIndex;
                // 通道加入发送帧
                CGlobalCtrl.Comms[CGlobalCtrl.CommNum - 1].FillFrame(CommFrame);

                CDebugInfo.writeconsole(RowIndex.ToString(), ColumnIndex.ToString());*/
            }
        }


        #endregion
    }


    public class CPutInOutDetail : Object
    {
        /// <summary>
        /// 
        /// </summary>
        public CPutInOutDetail()
        {

        }
        /// <summary>
        /// 实时投入时间
        /// </summary>
        public int PutInTime1 { get; set; }
        public int PutInTime2 { get; set; }
        public int PutInTime3 { get; set; }
        public int PutInTime4 { get; set; }
        /// <summary>
        /// 设置投入时间
        /// </summary>
        public int SetInTime1 { get; set; }
        public int SetInTime2 { get; set; }
        public int SetInTime3 { get; set; }
        public int SetInTime4 { get; set; }
        /// <summary>
        /// 调整投入时间
        /// </summary>
        public int AdjustInTime1 { get; set; }
        public int AdjustInTime2 { get; set; }
        public int AdjustInTime3 { get; set; }
        public int AdjustInTime4 { get; set; }
        /// <summary>
        /// 电容器的电流值
        /// </summary>
        public int Current1 { get; set; }
        public int Current2 { get; set; }
        public int Current3 { get; set; }
        //public int Current4 { get; set; }

        /// <summary>
        /// 电容器的涌流值
        /// </summary>
        public int InrushCurrent1 { get; set; }
        public int InrushMultiple1 { get; set; }
        public int InrushCurrent2 { get; set; }
        public int InrushMultiple2 { get; set; }
        public int InrushCurrent3 { get; set; }
        public int InrushMultiple3 { get; set; }

        public int PutOutTime1 { get; set; }
        public int PutOutTime2 { get; set; }
        public int PutOutTime3 { get; set; }
        public int PutOutTime4 { get; set; }
        public bool Result { get; internal set; }
        public int Current4 { get; internal set; }
        //public int Current5 { get; internal set; }
        public int InrushCurrent4 { get; internal set; }
        //public int InrushCurrent5 { get; internal set; }
    }
    public class CPutInOutRec : Object
    {
        private List<CPutInOutDetail> m_TestDetail;

        public CPutInOutRec()
        {
            Result2 = true;
            //Adjust50Times = 1;
            //Adjust20Times = 1;
            this.Result = false;
            m_TestDetail = new List<CPutInOutDetail>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="address"></param>
        /// <param name="maxturns"></param>
        public CPutInOutRec(string sn,byte address)
        {
            this.SN = sn;
            this.Address = address;
            this.Times = 0;
            this.Result = false;
            Result2 = true;

            m_TestDetail = new List<CPutInOutDetail>();
        }
        /// <summary>
        /// 加入测试明细
        /// </summary>
        /// <param name="detail"></param>
        public void AddPutInOutDetail(CPutInOutDetail detail)
        {
            m_TestDetail.Add(detail);
        }
        /// <summary>
        /// 获取投入明细
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public CPutInOutDetail GetPutInOutDetail(int index)
        {
            if (index > m_TestDetail.Count())
                return null;

            return m_TestDetail[index];
        }

        internal int GetRecCount()
        {
            return m_TestDetail.Count;
        }

        /// <summary>
        /// SN 编号
        /// </summary>
        public string SN { get; set; }

        /// <summary>
        /// 测试次数
        /// </summary>
        public int Times { get; set; }

        /// <summary>
        /// 组网分配的地址
        /// </summary>
        public byte Address { get; internal set; }

        /// <summary>
        /// 测试结果
        /// </summary>
        public bool Result { get; set; }
        public bool NeedAdjust { get; internal set; }
        public int Adjust50Times { get; internal set; }
        public int Adjust20Times { get; internal set; }
        public bool Result2 { get; internal set; }
    }

}
