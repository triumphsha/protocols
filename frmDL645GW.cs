using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using System.Xml;
using CCXml;
using DevComponents.DotNetBar.SuperGrid;
using System.Collections;

namespace 智能电容器
{
    public partial class frmDL645GW : DevComponents.DotNetBar.Metro.MetroForm
    {
        private Timer mTimer = new Timer();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="param"></param>
        delegate void delegateSetCommFrame(CommEventArgs param, bool flag);

        public frmDL645GW()
        {
            InitializeComponent();

            InitializeData();

            mTimer.Interval = 1000;
            mTimer.Tick += MTimer_Tick;
            mTimer.Start();
        }


        private void MTimer_Tick(object sender, EventArgs e)
        {
            // 处理系统对时信息
            string strDateTime = (DateTime.Now.Year % 100).ToString("D2") + DateTime.Now.Month.ToString("D2") + DateTime.Now.Day.ToString("D2") +
                DateTime.Now.Hour.ToString("D2") + DateTime.Now.Minute.ToString("D2") + DateTime.Now.Second.ToString("D2");
        }

        private void InitializeData()
        {
            string[] orderArray = { "Asterids", "Eudicots", "Rosids" };
            XmlNodeList GetNodeList = XMLHelper.GetXmlNodeListByXpath(System.Environment.CurrentDirectory + "\\数据结构\\规约测试项配置.xml", "//protocol//item");
            GridPanel SGridPanel = sgridDL645GW.PrimaryGrid;
            SGridPanel.Rows.Clear();
            SGridPanel.Columns["gridColumn8"].EditorType = typeof(FlowerButton_o);
            SGridPanel.Columns["gridColumn8"].EditorParams = new object[] { orderArray };
            SGridPanel.Columns["gridColumn9"].EditorType = typeof(FlowerButton_o);
            SGridPanel.Columns["gridColumn9"].EditorParams = new object[] { orderArray };

            for (int i = 0; i < GetNodeList.Count; i++)
            {
                // item 
                GridRow GetRow = new GridRow(GetNodeList[i].Attributes["name"].Value.ToString(), "", "", "", "", "", "",
                            GetNodeList[i].Attributes["mwritefunc"].Value.ToString(), GetNodeList[i].Attributes["readfunc"].Value.ToString());
                GetRow.Tag = "P" + GetNodeList[i].Attributes["id"].Value.ToString();

                XmlNodeList GetSubNodeList = GetNodeList[i].ChildNodes;
                for (int j = 0; j < GetSubNodeList.Count; j++)
                {
                    XmlNodeList GetSSubNodeList = GetSubNodeList[j].ChildNodes;

                    GridRow GetSubRow = new GridRow(GetSSubNodeList[0].InnerXml.ToString(), GetSSubNodeList[1].InnerXml.ToString(), GetSSubNodeList[2].InnerXml.ToString(), GetSSubNodeList[3].InnerXml.ToString(),
                                                GetSSubNodeList[4].InnerXml.ToString(), "终端", GetSSubNodeList[5].InnerXml.ToString());
                    GetSubRow.Tag = "C" + j.ToString();
                    GetRow.Rows.Add(GetSubRow);
                }
                SGridPanel.Rows.Add(GetRow);
            }

            CGlobalCtrl.Comms[0].CommEvent += FrmDL645GW_CommEvent;
        }

        public void SetCommFrame(CommEventArgs param, bool flag)
        {
            if (flag)
            {
                if (sgridDL645GW.InvokeRequired)
                {
                    delegateSetCommFrame d = new delegateSetCommFrame(SetCommFrame);
                    this.Invoke(d, new object[] { param, flag });
                }
                else
                {
                    string datatypestr;
                    CCommFrame CommFrame = param.CommFrame;
                    GridPanel SGridPanel = sgridDL645GW.PrimaryGrid;
                    GridRow GetRow = (GridRow)SGridPanel.Rows[CommFrame.ShowIndex];
                    int startindex = 0, datalen = 0, checkstart = -1;
                    Byte[] temp;
                    object getValue = new object();
                    string showstr = "";

                    if (CommFrame.FrameResope == 0x66)
                    {
                        showstr = CGlobalCtrl.Modbushost.GetErrorStr(Convert.ToInt16(CommFrame.GetRecvContent()[0]));
                        GetRow.Cells[5].Value = showstr;

                    }
                    else
                    {
                        GetRow.Cells[5].Value = "解析成功！";
                        try
                        {
                            for (int i = 0; i < GetRow.Rows.Count; i++)
                            {
                                GridRow GetSubRow = (GridRow)GetRow.Rows[i];
                                if (GetSubRow.Checked)
                                {
                                    if (CommFrame.FrameIndex <= 0x04)
                                    {
                                        if (checkstart == -1)
                                            checkstart = i;

                                        datatypestr = GetSubRow.Cells[2].Value.ToString().Trim();
                                        datalen = Convert.ToInt32(GetSubRow.Cells[3].Value.ToString().Trim());
                                        temp = new Byte[datalen];
                                        Array.Copy(CommFrame.GetRecvContent(), startindex, temp, 0, datalen);
                                        switch (datatypestr)
                                        {
                                            case "BIN":
                                                CFunc.bytestobin(temp, ref getValue);
                                                showstr = getValue.ToString();
                                                break;
                                            case "BCD":
                                                showstr = BitConverter.ToString(temp).Replace("-", null);
                                                break;
                                            case "BIT":
                                                showstr = (temp[0] & (Byte)Math.Pow(2, i - checkstart)) > 0 ? "1" : "0";
                                                break;
                                        }
                                        GetSubRow.Cells[5].Value = showstr;

                                        if (datatypestr == "BIT")
                                        {
                                            if (i > 0 && (i % 8) == 0)
                                            {
                                                startindex += 1;
                                            }
                                        }
                                        else
                                        {
                                            startindex += datalen;
                                        }
                                    }
                                }
                                else
                                {
                                    if (datalen > 0)
                                        break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    CDebugInfo.writeconsole("帧数据：", System.BitConverter.ToString(CommFrame.GetRecvContent()));
                }
            }
            else
            {

            }
        }

        private void FrmDL645GW_CommEvent(object sender, CommEventArgs e)
        {
            //if(sender.GetType()=="")
            if (superTabControl2.SelectedTabIndex == 1)
                SetCommFrame(e, true);
            if (superTabControl2.SelectedTabIndex == 0)
                SetCommFrame(e, false);
        }


        public static void FillGuiValue(out Byte[] values, string datastr, string datatypestr, ushort datalen)
        {
            Byte temp;
            values = new Byte[datalen];
            switch (datatypestr)
            {
                case "BIN":
                    //if (CGlobalCtrl.CommLSB)
                    //{ 
                    //    //datalen = (UInt16)System.Net.IPAddress.HostToNetworkOrder(Convert.ToInt16(datastr));

                    //values = CFunc.u16memcpy((UInt16)System.Net.IPAddress.HostToNetworkOrder(Convert.ToInt16(datastr)));//.CopyTo(values,datalen);
                    CFunc.StrToBytes(datastr, datalen, false).CopyTo(values, 0);
                    //}
                    //else
                    //    values = CFunc.u16memcpy(Convert.ToUInt16(datastr));
                    break;
                case "BCD":
                    //values = new Byte[datalen];
                    CFunc.StrToBytes(datastr, datalen, true).CopyTo(values, 0);
                    break;
                case "BIT":
                    //values = new Byte[1];
                    values[0] = Convert.ToByte(datastr);
                    break;
                default:

                    break;
            }
        }

        private void sgridmodbus_BeforeCheck(object sender, GridBeforeCheckEventArgs e)
        {
            GridRow GetRow = (GridRow)e.Item;
            if (GetRow.Tag.ToString().Contains("P"))
            {

                foreach (GridRow row in GetRow.Rows)
                {
                    row.Checked = !GetRow.Checked;

                }
            }
        }

        private void frmModbus_FormClosing(object sender, FormClosingEventArgs e)
        {
            mTimer.Stop();
        }
    }

    internal class FlowerButton_o : GridButtonXEditControl
    {
        #region Private variables

        //private ImageList _FlowerImageList;

        #endregion

        public FlowerButton_o(IEnumerable orderarray)
        {
            //_FlowerImageList = flowerImageList;

            Click += FlowerButtonClick;
        }

        #region FlowerButtonClick

        void FlowerButtonClick(object sender, EventArgs e)
        {
            string buttentext = EditorCell.Value as string;

          
                Byte[] sendbuf = new Byte[256];
                int sendlen;
                UInt16 datalen=0,subdatalen=0;
            UInt16 regaddr=0,regnum=0;
                string datastr;
                string datatypestr;
                Byte[] temp;
                FrameHeadStr FrameHead = new FrameHeadStr();
            int RowIndex = EditorCell.RowIndex;
            int ColumnIndex = EditorCell.ColumnIndex;

            //if (e.RowIndex < 0 || e.RowIndex > dgvwModbus.RowCount - 2) return;
            // 规约数据项选择下发
            if (ColumnIndex == 7 || ColumnIndex == 8)
            {

                GridRow GetRow = EditorCell.GridRow;
                if (GetRow.Cells[ColumnIndex].Value.ToString().Trim() == "")
                {
                    //CDebugInfo.writelogctrl(null, "命令不能为空", 0);
                    MessageBox.Show(this, "命令不能为空", "警告");
                    return;
                }

                for (int i = 0; i < GetRow.Rows.Count; i++)
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
                if (FrameHead.funcode == 0x0f || FrameHead.funcode ==0x01)
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
                            datalen = (UInt16)CFunc.bitstobytes(FrameHead.values,sendbuf);
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

                    CDebugInfo.writeconsole(RowIndex.ToString(), ColumnIndex.ToString());
                }
            }
        

        #endregion
    }
}