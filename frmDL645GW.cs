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
using System.Runtime.InteropServices;

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

        private GridRow CtrlGetRow = new GridRow();

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
            string cmdRead = "";
            string cmdReadNext = "";
            string cmdWrite = "";
            int groupFlag = 0x01;

            XmlNodeList GetNodeList = XMLHelper.GetXmlNodeListByXpath(System.Environment.CurrentDirectory + "\\数据结构\\DL645_GWPD.xml", "//protocol//item");
            GridPanel SGridPanel = sgridDL645GW.PrimaryGrid;
            SGridPanel.Rows.Clear();
            SGridPanel.Columns["gridColumn8"].EditorType = typeof(FlowerButton_o);
            SGridPanel.Columns["gridColumn8"].EditorParams = new object[] { orderArray };
            SGridPanel.Columns["gridColumn9"].EditorType = typeof(FlowerButton_o);
            SGridPanel.Columns["gridColumn9"].EditorParams = new object[] { orderArray };

            for (int i = 0; i < GetNodeList.Count; i++)
            {
                // item 
                cmdRead = GetNodeList[i].Attributes["readfunc"].Value.ToString();
                cmdWrite = GetNodeList[i].Attributes["writefunc"].Value.ToString();
                groupFlag = Convert.ToInt16(GetNodeList[i].Attributes["group"].Value);
                GridRow GetRow = new GridRow(GetNodeList[i].Attributes["name"].Value.ToString(), "", "", "", "", "", "", "", "");
                GetRow.Tag = "P" + GetNodeList[i].Attributes["id"].Value.ToString();

                if (groupFlag == 1)
                {
                    // group
                    XmlNodeList GetSubNodeList = GetNodeList[i].ChildNodes;
                    for (int j = 0; j < GetSubNodeList.Count; j++)
                    {
                        GridRow GetSubRow = new GridRow(GetSubNodeList[j].Attributes["name"].Value.ToString(), GetSubNodeList[j].Attributes["flag"].Value.ToString(), "", "",
                                                        "", "", "", cmdWrite, cmdRead);
                        GetSubRow.Tag = GetRow.Tag +",G" + j.ToString();

                        XmlNodeList GetSubSubNodeList = GetSubNodeList[j].ChildNodes;
                        for (int k = 0; k < GetSubSubNodeList.Count; k++)
                        {
                            XmlNodeList GetSSubNodeList = GetSubSubNodeList[k].ChildNodes;

                            GridRow GetSubSubRow = new GridRow(GetSSubNodeList[0].InnerXml.ToString(), GetSSubNodeList[1].InnerXml.ToString(), GetSSubNodeList[2].InnerXml.ToString(), GetSSubNodeList[3].InnerXml.ToString(),
                                                        GetSSubNodeList[4].InnerXml.ToString(), "终端", GetSSubNodeList[5].InnerXml.ToString(), cmdWrite, cmdRead);
                            GetSubSubRow.Tag = GetSubRow.Tag + ",C" + k.ToString();

                            GetSubRow.Rows.Add(GetSubSubRow);
                        }
                        GetRow.Rows.Add(GetSubRow);
                    }
                }
                else
                {
                    XmlNodeList GetSubNodeList = GetNodeList[i].ChildNodes;
                    for (int j = 0; j < GetSubNodeList.Count; j++)
                    {
                        XmlNodeList GetSSubNodeList = GetSubNodeList[j].ChildNodes;

                        GridRow GetSubSubRow = new GridRow(GetSSubNodeList[0].InnerXml.ToString(), GetSSubNodeList[1].InnerXml.ToString(), GetSSubNodeList[2].InnerXml.ToString(), GetSSubNodeList[3].InnerXml.ToString(),
                                                    GetSSubNodeList[4].InnerXml.ToString(), "终端", GetSSubNodeList[5].InnerXml.ToString(), cmdWrite, cmdRead);
                        GetSubSubRow.Tag = GetRow.Tag + ",C" + j.ToString();

                        GetRow.Rows.Add(GetSubSubRow);
                    }
                }
                SGridPanel.Rows.Add(GetRow);
            }
            CGlobalCtrl.Comms[0].Protocol = new CDL645();

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
                    GridRow GetRow = CommFrame.ShowRow;// (GridRow)SGridPanel.Rows[CommFrame.ShowIndex];
                    int startindex = 0, datalen = 0, checkstart = -1,rate=0;
                    Byte[] temp;
                    object getValue = new object();
                    string showstr = "";

                    if (CommFrame.FrameResope == 0x66)
                    {
                        //showstr = CGlobalCtrl.Modbushost.GetErrorStr(Convert.ToInt16(CommFrame.GetRecvContent()[0]));
                        GetRow.Cells[5].Value = showstr;
                    }
                    else
                    {
                        //GetRow.Cells[5].Value = "解析成功！";
                        try
                        {
                            //for (int i = 0; i < GetRow.Rows.Count; i++)
                            //{
                            //    GridRow GetSubRow = (GridRow)GetRow.Rows[i];
                            //    if (GetSubRow.Checked)
                            //    {
                            //        if (CommFrame.FrameIndex <= 0x04)
                            //        {
                            //            if (checkstart == -1)
                            //                checkstart = i;

                                        datatypestr = GetRow.Cells[2].Value.ToString().Trim();
                                        datalen = Convert.ToInt32(GetRow.Cells[3].Value.ToString().Trim());
                                        rate = Convert.ToInt32(GetRow.Cells[6].Value.ToString().Trim());
                            temp = new Byte[datalen];
                                        Array.Copy(CommFrame.GetRecvContent(), startindex, temp, 0, datalen);
                                        switch (datatypestr)
                                        {
                                            case "BIN":
                                                CFunc.bytestobin(temp, ref getValue);
                                                showstr = getValue.ToString();
                                                break;
                                            case "BCD":
                                                showstr = CDL645.DataTrans(temp,datatypestr, datalen, rate);//BitConverter.ToString(temp).Replace("-", null);
                                                break;
                                            case "BIT":
                                                //showstr = (temp[0] & (Byte)Math.Pow(2, i - checkstart)) > 0 ? "1" : "0";
                                                break;
                                        }
                            GetRow.Cells[5].Value = showstr;

                                        //if (datatypestr == "BIT")
                                        //{
                                        //    if (i > 0 && (i % 8) == 0)
                                        //    {
                                        //        startindex += 1;
                                        //    }
                                        //}
                                        //else
                                        //{
                                        //    startindex += datalen;
                                        //}
                                //    }
                                //}
                                //else
                                //{
                                //    if (datalen > 0)
                                //        break;
                                //}
                            //}
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
            //if (superTabControl2.SelectedTabIndex == 1)
                SetCommFrame(e, true);
            //if (superTabControl2.SelectedTabIndex == 0)
            //    SetCommFrame(e, false);
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
            GridButtonXEditControl btn = (GridButtonXEditControl)sender;

            string buttentext = btn.EditorCell.Value as string;

            Byte[] sendbuf = new Byte[256];
            int sendlen=0;
            UInt16 datalen = 0, subdatalen = 0;
            string datastr;
            string datatypestr;
            DL645HeadStr FrameHead = new DL645HeadStr();
            FrameHead.address = new byte[6];
            FrameHead.didt = new byte[4];
            FrameHead.msgload = new byte[100];
            int RowIndex = btn.EditorCell.RowIndex;
            int ColumnIndex = btn.EditorCell.ColumnIndex;

            // 规约数据项选择下发
            if (ColumnIndex == 7 || ColumnIndex == 8)
            {

                GridRow GetRow = btn.EditorCell.GridRow;
                if (GetRow.Cells[ColumnIndex].Value.ToString().Trim() == "")
                {
                    //CDebugInfo.writelogctrl(null, "命令不能为空", 0);
                    MessageBox.Show(this, "命令不能为空", "警告");
                    return;
                }
                else
                    FrameHead.func = Convert.ToByte(GetRow.Cells[ColumnIndex].Value.ToString().Trim(), 16);

                // dl645 di dt data item
                FrameHead.didt[3] = Convert.ToByte(GetRow.Cells[1].Value.ToString().Trim().Substring(0, 2), 16);
                FrameHead.didt[2] = Convert.ToByte(GetRow.Cells[1].Value.ToString().Trim().Substring(2, 2), 16);
                FrameHead.didt[1] = Convert.ToByte(GetRow.Cells[1].Value.ToString().Trim().Substring(4, 2), 16);
                FrameHead.didt[0] = Convert.ToByte(GetRow.Cells[1].Value.ToString().Trim().Substring(6, 2), 16);

                // fill content
                if (FrameHead.func == 0x14) // set param
                {
                    datatypestr = GetRow.Cells[2].Value.ToString().Trim();
                    datastr = GetRow.Cells[4].Value.ToString().Trim();
                    subdatalen = (UInt16)(Convert.ToUInt16(GetRow.Cells[3].Value));
                    FrameHead.msglen = CDL645.DataFill(FrameHead.msgload, datatypestr, datastr, subdatalen);
                    if (FrameHead.msglen < 0)
                    {
                        MessageBox.Show(this, "数据转换出错", "错误");
                        return;
                    }
                }
                else if (FrameHead.func == 0x11) // call param
                {
                    if (GetRow.Tag.ToString().Contains("P6") || GetRow.Tag.ToString().Contains("P7") ||
                        GetRow.Tag.ToString().Contains("P8"))
                    {
                        datatypestr = GetRow.Cells[2].Value.ToString().Trim();
                        datastr = GetRow.Cells[4].Value.ToString().Trim();
                        subdatalen = (UInt16)(Convert.ToUInt16(GetRow.Cells[3].Value));
                        FrameHead.msglen = CDL645.DataFill(FrameHead.msgload, datatypestr, datastr, subdatalen);
                        if (FrameHead.msglen < 0)
                        {
                            MessageBox.Show(this, "数据转换出错", "错误");
                            return;
                        }
                    }
                }

                // 地址
                FrameHead.address = new byte[6] { 0,0,0,0,0,0 };
                FrameHead.address[0] = CFunc.HEX2BCD((byte)CGlobalCtrl.ProtocolAddress);
                // Array.Copy( CGlobalCtrl.CommAddress,FrameHead.address, 6);

                CCommFrame CommFrame = new CCommFrame();
                sendlen = CGlobalCtrl.Comms[0].Protocol.makeframe(CFunc.StructToBytes(FrameHead), Marshal.SizeOf(FrameHead), sendbuf, ref sendlen);
                CommFrame.FillContent(sendbuf, sendlen, 0);
                CommFrame.ShowIndex = RowIndex;
                CommFrame.ShowRow = GetRow;
                // 通道加入发送帧
                CGlobalCtrl.Comms[0].FillFrame(CommFrame);

                CDebugInfo.writeconsole(RowIndex.ToString(), ColumnIndex.ToString());
            }
        }
        #endregion
    }
}