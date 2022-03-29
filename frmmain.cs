using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevComponents.DotNetBar.Metro;
using DevComponents.AdvTree;
using System.Xml;
using CCXml;

namespace 智能电容器
{
    public partial class frmmain : MetroForm
    {

        delegate void SetTextCallback(string text);


        private DevComponents.DotNetBar.ButtonItem m_PopupFromCode = null;
        public frmmain()
        {
            InitializeComponent();

            InitializeData();

            CGlobalCtrl.MainForm = this;
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            string NodeName = "";
            string SubNodeName = "";
            // 创建操作树控件 
            advtreeOperate.Nodes.Clear();

            string strPath = System.Environment.CurrentDirectory;
            strPath = System.Windows.Forms.Application.StartupPath;
            XmlNodeList GetNodeList = XMLHelper.GetXmlNodeListByXpath(System.Environment.CurrentDirectory + "\\数据结构\\操作栏目配置.xml", "//operation//tree//工位");

            
            Node ParentNode = new Node(GetNodeList[0].ParentNode.Attributes["name"].Value.ToString());
           //ParentNode.
            for (int i = 0; i < GetNodeList.Count; i++)
            {
                XmlNodeList GetSubNodeList = GetNodeList[i].ChildNodes;
                SubNodeName = GetNodeList[i].Attributes["name"].Value.ToString();
                Node MidNode = new Node("测试工位-" + GetNodeList[i].Attributes["id"].Value.ToString());
                MidNode.Tag = GetNodeList[i].Attributes["id"].Value;
                ParentNode.Nodes.Add(MidNode);
                for (int j = 0; j < GetSubNodeList.Count; j++)
                {
                    XmlNodeList GetSSubNodeList = GetSubNodeList[j].ChildNodes;

                    NodeName = SubNodeName + "-" + GetSubNodeList[j].Attributes["id"].Value.ToString() + "," + GetSSubNodeList[0].InnerXml.ToString()+ "," + GetSSubNodeList[1].InnerXml.ToString();
                   
                    Node NewNode = new Node(NodeName);
                    NewNode.Tag = GetNodeList[i].Attributes["id"].Value.ToString() + "," + GetSubNodeList[j].Attributes["id"].Value.ToString(); 
                    MidNode.Nodes.Add(NewNode);
                }
            }

            advtreeOperate.Nodes.Add(ParentNode);

            // 创建通道
            InitializeComm();

            for(int i=0;i<CGlobalCtrl.MAX_COMM_NUM;i++)
            { 
                if(CGlobalCtrl.Comms[i] != null)
                { 
                    CGlobalCtrl.Comms[i].StateEvent += Frmmain_StateEvent;
                    // 
                }
            }
            ///// 
            for (int i = 0; i < CGlobalCtrl.MAX_VARNODE_NUM; i++)
            {
                CGlobalCtrl.SnToAddressTable[i].address = i + 1;
                CGlobalCtrl.SnToAddressTable[i].serail = new Byte[6] { 0x3f, 00, 00, 00, 00, (byte)(i+1)};
                CGlobalCtrl.SnToAddressTable[i].validflag = CNetWork.SN_NOVALID;
            }
        }

        public void SetText(string text)
        {
            //throw new NotImplementedException();        
            if (this.rchText.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.rchText.Text += text;

                if (text.Contains( "Network 线程安全退出！"))
                {
                    // 更新组网信息
                    // MetroForm frm = (MetroForm) this.MdiChildren[0];
                    if(CGlobalCtrl.AuotTestFrom != null)
                        CGlobalCtrl.AuotTestFrom.FlashShow(1);
                }
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Frmmain_StateEvent(object sender, StateEventArgs e)
        {
            SetText(CDebugInfo.writelogctrl(e.Message,0));

            // 更新列表框
        }

        bool InitializeComm()
        {
            string channeltype;
            string channeldesc;
            int i;
            XmlNodeList GetNodeList = XMLHelper.GetXmlNodeListByXpath(System.Environment.CurrentDirectory + "\\数据结构\\系统配置.xml", "//sysconfig//transform");
            CGlobalCtrl.CommLSB = GetNodeList[0].InnerText.Contains("LSB") ? true : false;


            GetNodeList = XMLHelper.GetXmlNodeListByXpath(System.Environment.CurrentDirectory +  "\\数据结构\\系统配置.xml", "//sysconfig//channelconfig//comm");
            
            XmlElement xmlElement;
            for (i = 0; i < GetNodeList.Count; i++)
            {
                channeltype = GetNodeList[i].Attributes["type"].Value.ToString();
                //xmlDoc = GetNodeList[i].OwnerDocument;
                xmlElement = (XmlElement)GetNodeList[i];
                CDebugInfo.writeconsole("", xmlElement["config"].InnerText.Trim());
                channeldesc = xmlElement["config"].InnerText.Trim(); //xmlDoc.DocumentElement["config"].InnerText.Trim();

                CComm newComm = CCommFactory.CreateComm(channeltype, channeldesc);
                // newComm.CommEvent += CGlobalCtrl.Comms[CGlobalCtrl.CommNum - 1].CommEvent += FrmModbus_CommEvent;
                newComm.CommNum = i;   // 是否合适
                CGlobalCtrl.Comms[i] = newComm;
            }

            // 
            GetNodeList = XMLHelper.GetXmlNodeListByXpath(System.Environment.CurrentDirectory + "\\数据结构\\系统配置.xml", "//sysconfig//Authority");
            //string datenow = string.Format("{0:D4}{1:D2}", System.DateTime.Now.Year, System.DateTime.Now.Month);
            //if (datenow.Equals(GetNodeList[0].ChildNodes[0].InnerXml.Trim().Substring(0, 6)))
            //    CGlobalCtrl.SoleCommonSN = GetNodeList[0].ChildNodes[0].InnerXml.Trim();
            //else
            //    CGlobalCtrl.SoleCommonSN = datenow + "0001";  // 新的一月  
            for (i = 0; i < GetNodeList[0].ChildNodes.Count; i++)
            {
                if (GetNodeList[0].ChildNodes[i].InnerXml.Trim().Equals("Enable"))
                { 
                    if(GetNodeList[0].ChildNodes[i].Name == "setsn")
                        CGlobalCtrl.Authority |= 0x10;
                    else if (GetNodeList[0].ChildNodes[i].Name == "putcheck")
                        CGlobalCtrl.Authority |= 0x20;
                    else if (GetNodeList[0].ChildNodes[i].Name == "setcapacity")
                        CGlobalCtrl.Authority |= 0x40;
                    else if (GetNodeList[0].ChildNodes[i].Name == "putinout")
                        CGlobalCtrl.Authority |= 0x80;
                }
            }
            //
            GetNodeList = XMLHelper.GetXmlNodeListByXpath(System.Environment.CurrentDirectory + "\\数据结构\\系统配置.xml", "//sysconfig//SN");            
            string datenow = string.Format("{0:D4}{1:D2}", System.DateTime.Now.Year, System.DateTime.Now.Month);   
            if (datenow.Equals(GetNodeList[0].ChildNodes[0].InnerXml.Trim().Substring(0,6)))
                CGlobalCtrl.SoleCommonSN = GetNodeList[0].ChildNodes[0].InnerXml.Trim();
            else
                CGlobalCtrl.SoleCommonSN = datenow + "0001";  // 新的一月            

            return true;
        }

        private void btnModbus_Click(object sender, EventArgs e)
        {

        }

        private void advtreeOperate_NodeMouseDown(object sender, TreeNodeMouseEventArgs e)
        {
            string[] nodeText;
            Node EventNode = (Node)e.Node;

            if (EventNode.Text.Trim().Contains("工位"))
            {
                if (e.Button == MouseButtons.Right)
                {
                    CGlobalCtrl.CommNum = Convert.ToInt16(EventNode.Tag.ToString().Trim());
                    if (m_PopupFromCode == null)
                        CreatePopupMenu();

                    // Apply style
                    DevComponents.DotNetBar.eDotNetBarStyle style = DevComponents.DotNetBar.eDotNetBarStyle.VS2005;
                    m_PopupFromCode.Style = style;

                    // MUST ALWAYS register popup with DotNetBar Manager if popup does not belong to ContextMenus collection
                    dotNetBarManager1.RegisterPopup(m_PopupFromCode);

                    //Control ctrl = sender as Control;
                    //bar3.PopupAnimation bar3.Bounds.X+
                    Point p = this.PointToScreen(new Point(EventNode.Bounds.X + EventNode.Bounds.Width + 5, EventNode.Bounds.Y + EventNode.Bounds.Height + 60));
                    m_PopupFromCode.PopupMenu(p);
                }
                //m_PopupFromCode.
            }
            else if (EventNode.Text.Trim().Contains("电容器"))
            {
                nodeText = EventNode.Tag.ToString().Split(',');

                CGlobalCtrl.CommNum = Convert.ToInt16(nodeText[0].Trim());
                CGlobalCtrl.ProtocolAddress = Convert.ToInt16(nodeText[1].Trim());

                if (ShowChildrenForm("frmModbus")) return;

                frmModbus frm = new frmModbus();
                frm.MdiParent = this;
                frm.Show();
                frm.WindowState = FormWindowState.Maximized;
                frm.Refresh();
            }
            else if (EventNode.Text.Trim().Contains("断路器"))
            {
                nodeText = EventNode.Tag.ToString().Split(',');

                CGlobalCtrl.CommNum = Convert.ToInt16(nodeText[0].Trim());
                CGlobalCtrl.ProtocolAddress = Convert.ToInt16(nodeText[1].Trim());

                if (ShowChildrenForm("frmDL645GW")) return;

                frmDL645GW frm = new frmDL645GW();
                frm.MdiParent = this;
                frm.Show();
                frm.WindowState = FormWindowState.Maximized;
                frm.Refresh();
            }


        }

        //防止打开多个窗体
        private bool ShowChildrenForm(string p_ChildrenFormText)
        {
            int i;
            // 依次检测当前窗体的子窗体
            for (i = 0; i < this.MdiChildren.Length; i++)
            {
                //判断当前子窗体的Text属性值是否与传入的字符串值相同
                if (this.MdiChildren[i].Name == p_ChildrenFormText)
                {
                    //如果值相同则表示此子窗体为想要调用的子窗体，激活此子窗体并返回true值
                    this.MdiChildren[i].Activate();
                    return true;
                }
            }
            //如果没有相同的值则表示要调用的子窗体还没有被打开，返回false值
            return false;
        }
        private void CreatePopupMenu()
        {
            DevComponents.DotNetBar.ButtonItem item;

            m_PopupFromCode = new DevComponents.DotNetBar.ButtonItem();

            // Create items
            item = new DevComponents.DotNetBar.ButtonItem("setcomm");
            item.Text = "设置(&S)";
            item.MouseDown += Item_MouseDown;     
            // To remember: cannot use the ImageIndex for items that we create from code
            //item.Image = imageList1.Images[0];
            m_PopupFromCode.SubItems.Add(item);

            item = new DevComponents.DotNetBar.ButtonItem("autonet");
            item.Text = "组网(&N)";
            item.MouseDown += Item_MouseDown;
            item.BeginGroup = true;
            //item.Image = imageList1.Images[1];
            m_PopupFromCode.SubItems.Add(item);

            item = new DevComponents.DotNetBar.ButtonItem("resswitch");
            item.Text = "自动测试";
            item.MouseDown += Item_MouseDown;
            item.BeginGroup = true;
            //item.Image = imageList1.Images[2];
            m_PopupFromCode.SubItems.Add(item);

            item = new DevComponents.DotNetBar.ButtonItem("capswitch");
            item.Text = "电容投切";
            item.MouseDown += Item_MouseDown;
            m_PopupFromCode.SubItems.Add(item);

            item = new DevComponents.DotNetBar.ButtonItem("accheck");
            item.Text = "交流采样";
            item.MouseDown += Item_MouseDown;
            item.BeginGroup = true;
            m_PopupFromCode.SubItems.Add(item);           
        }

        public void NetWorkStart()
        {
            if (CGlobalCtrl.Comms[CGlobalCtrl.CommNum - 1] == null) return;
            CNetWork network = new CNetWork(CGlobalCtrl.Comms[CGlobalCtrl.CommNum - 1]);
            network.StateEvent += Frmmain_StateEvent;
            network.StartNetWork();
        }
        //private System.Threading.Thread NetThread;

        private void Item_MouseDown(object sender, MouseEventArgs e)
        {
            DevComponents.DotNetBar.ButtonItem buttonitem = (DevComponents.DotNetBar.ButtonItem)sender;

            //System.Threading.Timer mTimer;
            if (buttonitem.Text.Contains("组网"))
            {
                NetWorkStart();

                // advtreeOperate.SelectedNode.Nodes[0].Text = "组网测试";
            }

            if (buttonitem.Text.Contains("自动测试"))
            {
                /*if (CGlobalCtrl.Comms[CGlobalCtrl.CommNum - 1] == null) return;
                CNetWork network = new CNetWork(CGlobalCtrl.Comms[CGlobalCtrl.CommNum - 1]);
                network.StateEvent += Frmmain_StateEvent;
                network.StartNetWork();*/
                // advtreeOperate.SelectedNode.Nodes[0].Text = "组网测试";
            }
        }

        private void frmmain_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < CGlobalCtrl.MAX_COMM_NUM; i++)
            {
                if(CGlobalCtrl.Comms[i] !=null)
                    CGlobalCtrl.Comms[i].Close();
            }

            System.Threading.Thread.Sleep(1000);
        }

        private void cmdNodeRightClick_Executed(object sender, EventArgs e)
        {

        }

        private void buttonItem2_Click(object sender, EventArgs e)
        {
            // 
            if (ShowChildrenForm("frmAutoTest")) return;

            frmAutoTest frm = new frmAutoTest();
            frm.MdiParent = this;
            frm.Show();
            frm.WindowState = FormWindowState.Maximized;
            frm.Refresh();
        }
    }
}
