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

namespace 智能电容器
{
    public partial class frmShow : DevComponents.DotNetBar.Metro.MetroForm
    {
        private XmlNodeList mGetNodeList;
        private XmlNode mChildNode;

        private int mCurIndex;
        private string mChooseSN = null;
        private string mChooseType = null;
        private string mFileName = null;
        public frmShow()
        {
            InitializeComponent();
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            DateTime st = dateTimePicker1.Value;

            mFileName = System.Environment.CurrentDirectory + "\\数据结构\\测试记录-" + (st.ToShortDateString().Replace("/", ".")) + ".xml";
            MsgFlash(mChooseType, mChooseSN, mCurIndex, mFileName);
        }

        public void MsgFlash(string type, string sn,int index,string filename=null)
        {
            int i = 0;
            int count=0;
            int lookIndex = 0;
            bool bflag = true;       
            XmlNode lastNode, childNode;

            mCurIndex = index;
            mChooseSN = sn;
            mChooseType = type;

            if (filename == null)
                filename = System.Environment.CurrentDirectory + "\\数据结构\\测试记录-" + (DateTime.Now.ToShortDateString().Replace("/", ".")) + ".xml";
            mFileName = filename;

            // 流程判断所需的标准值
            mGetNodeList = XMLHelper.GetXmlNodeListByXpath(mFileName, "//记录文件//测试记录");
            if (mGetNodeList == null) return;

            lookIndex = mGetNodeList.Count-1;
            lastNode = mGetNodeList.Item(lookIndex);
            while (count < index && lookIndex>=0)
            {
                lastNode = mGetNodeList.Item(lookIndex);

                if (type != null)
                {
                    bflag = false;
                    if (lastNode.ChildNodes[0].Name.Equals(type))
                    {
                        bflag = true;
                    }
                }
                if (sn != null)
                {
                    bflag = false;
                    if (lastNode.ChildNodes[0].Attributes["SN"].Value.Equals(sn))
                    {
                        bflag = true;
                    }
                }

                if (bflag) count++;

                lookIndex--;
            }


            
            dgvwModbus.Rows.Clear();
            mChildNode = lastNode;
            childNode = lastNode.ChildNodes[0];
            this.Text = childNode.Name + "-" + childNode.Attributes["SN"].Value.ToString() + "-" + childNode.Attributes["时间"].Value.ToString() + "-" + childNode.Attributes["结论"].Value.ToString();

            // 
            for (i = 0; i < lastNode.ChildNodes[0].ChildNodes.Count; i++)
            {
                // 
                childNode = lastNode.ChildNodes[0].ChildNodes[i];
                dgvwModbus.Rows.Insert(i, childNode.Attributes["名称"].Value.ToString(), childNode.ChildNodes[0].InnerXml.Trim(),
                                childNode.ChildNodes[1].InnerXml.Trim(), childNode.ChildNodes[2].InnerXml.Trim(), childNode.ChildNodes[3].InnerXml.Trim());
            }

        }

        private void dgvwModbus_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void buttonItem1_Click(object sender, EventArgs e)
        {
            mCurIndex++;
            MsgFlash(mChooseType, mChooseSN, mCurIndex);
        }

        private void buttonItem2_Click(object sender, EventArgs e)
        {
            if(mCurIndex>1)
            { 
                mCurIndex--;
                MsgFlash(mChooseType, mChooseSN, mCurIndex);
            }
        }
        private void buttonItem3_Click(object sender, EventArgs e)
        {
            if (textBoxItem1.Text.Trim().Contains("3f") || textBoxItem1.Text.Trim().Contains("3F") ||
                textBoxItem1.Text.Trim().Contains("8f") || textBoxItem1.Text.Trim().Contains("8F"))
            {
                mChooseSN = textBoxItem1.Text.Trim();
            }
            else
                mChooseType = textBoxItem1.Text.Trim();

            mCurIndex = 1;
            MsgFlash(mChooseType, mChooseSN, mCurIndex);

        }

        private void buttonItem4_Click(object sender, EventArgs e)
        {
            mCurIndex = 1;
            mChooseSN = null;
            mChooseType = null;
            MsgFlash(mChooseType, mChooseSN, mCurIndex);
        }

        private void buttonItem5_Click(object sender, EventArgs e)
        {         

            // 保持相应测试记录数据
            XMLHelper.CreateOrUpdateXmlNodeByXPath(System.Environment.CurrentDirectory + "\\数据结构\\print.xml", "//记录文件", "测试记录", mChildNode.InnerXml.Trim());

            System.Diagnostics.Process.Start("iexplore.exe", System.Environment.CurrentDirectory + "\\数据结构\\print.xml");


        }

        private void btnDate_Click(object sender, EventArgs e)
        {
            //calendarView1.Show();

                
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void calendarView1_ItemClick(object sender, EventArgs e)
        {
            //calendarView1.GetRenderer()
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAdvanceAnalyse_Click(object sender, EventArgs e)
        {

        }
    }
}