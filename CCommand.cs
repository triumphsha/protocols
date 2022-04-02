using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CCXml;
using System.Threading;
using System.Runtime.InteropServices;

namespace 智能电容器
{
    /// <summary>
    /// 流程控制状态
    /// </summary>
    enum EnumFlowCtrlState
    {
        FLOWCTRL_IDLE,
        FLOWCTRL_RUNNING,
        FLOWCTRL_STOP
    }
    /// <summary>
    /// 命令状态
    /// </summary>
    enum EnumCmdState
    {
        CMD_START,
        CMD_AFFIRM,
        CMD_OTHER
    }

    enum EnumJudgeResult
    {
        RESULT_SUCCESS,
        RESULT_FAIL,
        RESULT_NEEDADJUST
    }
    /// <summary>
    /// 测试流程控制
    /// </summary>
    class CFlowCtrl
    {
        /// <summary>
        /// 配置的路经
        /// </summary>
        private string mPath;

        /// <summary>
        /// 测试流程对象编号
        /// </summary>
        private byte mAddress = 0;
        /// <summary>
        /// 流程执行线程
        /// </summary>
        public System.Threading.Thread mFlowThread;
        /// <summary>
        /// 测试步骤列表
        /// </summary>
        private List<CFlowStepInfo> mFlowStepInof;
        /// <summary>
        /// 测试通道号
        /// </summary>
        private int mCommNum;
        /// <summary>
        /// 测试流程状态
        /// </summary>
        private EnumFlowCtrlState mFlowCtrlState;
        public EnumFlowCtrlState FlowCtrlState { get { return mFlowCtrlState; } }

        /// <summary>
        /// 测试流程名称
        /// </summary>
        private string mName;
        public string Name { get { return mPath; } }

        /// <summary>
        /// 流程缓存数据字典
        /// </summary>
        private Dictionary<string, string> mFlowData;

        /// 通信通道事件 
        public delegate void FlowCtrlStateHandle(object sender, StateEventArgs e);
        public event FlowCtrlStateHandle StateEvent;

        /// <summary>
        /// 流程执行结果
        /// </summary>
        private bool mFlowCtrlResult;
        private int mJudgeTimes;

        /// <summary>
        /// 
        /// </summary>
        
        public bool FlowCtrlResult { get { return mFlowCtrlResult; } }

        public int mFailExecuteTimes { get; private set; }

        // public bool mPutInFlag = true;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="path"></param>
        /// <param name="addr"></param>
        /// <param name="commnum"></param>
        /// <param name="name"></param>
        /// <param name="step"></param>
        public CFlowCtrl(string path, byte addr, int commnum, string name, int step)
        {
            mCommNum = commnum;
            mAddress = addr;
            mFlowData = new Dictionary<string, string>();
            mPath = path;
            mName = name;
            mFlowStepInof = new List<CFlowStepInfo>(step);
            mFlowThread = new System.Threading.Thread(FlowExecute);
            mFlowCtrlState = EnumFlowCtrlState.FLOWCTRL_IDLE;
            mFlowCtrlResult = true;
            mFailExecuteTimes = 0;
            //mPutInFlag = true;            
        }

        private void RaiseStateEvent(string msg)
        {
            if (StateEvent != null)
            {
                StateEvent(this, new 智能电容器.StateEventArgs(msg, mCommNum));
            }
        }

        /// <summary>
        /// 流程控制
        /// </summary>
        public void FlowExecute()
        {
            int index = 0;
            int recIndex = 0;
            string strGetContentKey = "";

            // 流程判断所需的标准值
            XmlNodeList GetNodeList = XMLHelper.GetXmlNodeListByXpath(System.Environment.CurrentDirectory + "\\数据结构\\测试流程配置.xml", mPath + "//condition");
            if (GetNodeList != null)
            {
                while (index < GetNodeList.Count)
                {
                    mFlowData.Add(GetNodeList[index].Attributes["name"].Value.Trim(), GetNodeList[index].InnerXml.Trim());
                    index++;
                }
            }

            // 测试流程步骤配置信息获取，并执行
            index = 0;
            GetNodeList = XMLHelper.GetXmlNodeListByXpath(System.Environment.CurrentDirectory + "\\数据结构\\测试流程配置.xml", mPath + "//flowstep");
            while (index >= 0 && index < GetNodeList.Count)
            {
                // 创建测试单步信息
                CFlowStepInfo newFlowStepInfo = new CFlowStepInfo(GetNodeList[index].Attributes["name"].Value.ToString(),
                    GetNodeList[index].Attributes["type"].Value.ToString().Trim(), Convert.ToInt32(GetNodeList[index].Attributes["id"].Value.ToString()),
                    GetNodeList[index].ChildNodes[1].InnerXml.ToString().Trim(), GetNodeList[index].ChildNodes[2].InnerXml.ToString(),
                     GetNodeList[index].ChildNodes[3].Attributes[0].Value.ToString().Trim(), GetNodeList[index].ChildNodes[3].InnerXml.ToString().Trim(),
                      GetNodeList[index].ChildNodes[4].InnerXml.ToString().Trim());

                // 本步骤所需的数据信息
                strGetContentKey = newFlowStepInfo.RequestContext;
                if (strGetContentKey != "/")
                {
                    if (!mFlowData.ContainsKey(strGetContentKey))
                    {
                        mFlowCtrlResult = mFlowCtrlResult & false;
                        break;
                    }
                }

                // 本步骤创建所需的命令
                if (!newFlowStepInfo.CreateCmd(GetNodeList[index].ChildNodes[0].InnerText.ToString(), mAddress,
                            strGetContentKey == "/" ? null : CFunc.StrToBytes(mFlowData[strGetContentKey].Trim(),
                            ((mFlowData[strGetContentKey].Trim().Length / 2) < 2) ? (ushort)2 : (ushort)(mFlowData[strGetContentKey].Trim().Length / 2), true), mCommNum))
                {
                    mFlowCtrlResult = mFlowCtrlResult & newFlowStepInfo.ResultFlag;
                    break;
                }
                // 变更为实际信息
                try
                {
                    if (strGetContentKey != "/")
                        newFlowStepInfo.RequestContext = Convert.ToInt32(mFlowData[strGetContentKey].Trim(), 16).ToString();
                }
                catch (Exception ex)
                {
                    newFlowStepInfo.RequestContext = mFlowData[strGetContentKey].Trim();
                }

                // 本步骤执行
                if (newFlowStepInfo.Execute())
                {
                    // 特殊处理 读取电容器状态
                    if (newFlowStepInfo.Name == "读取电容器状态")
                    {
                        mFailExecuteTimes = 0;
                    }

                            // 本步骤响应的数据
                            if (newFlowStepInfo.RespondKey != "/")
                    {
                        if (mFlowData.Keys.Contains<string>(newFlowStepInfo.RespondKey))
                        {
                            if (newFlowStepInfo.RespondContext != "/")
                                mFlowData[newFlowStepInfo.RespondKey] = newFlowStepInfo.RespondContext;
                        }
                        else
                            mFlowData.Add(newFlowStepInfo.RespondKey, newFlowStepInfo.RespondContext);
                    }

                    // 本步骤判断条件
                    EnumJudgeResult eJudgeResult = EnumJudgeResult.RESULT_FAIL;
                    if (newFlowStepInfo.JumpCondition != "/")
                    {
                        eJudgeResult = JudgeCondition(newFlowStepInfo.JumpCondition);
                        newFlowStepInfo.JudgeResult = eJudgeResult == EnumJudgeResult.RESULT_FAIL ? false : true;
                    }
                    // 本步骤插入流程列表
                    mFlowStepInof.Insert(recIndex++, newFlowStepInfo);

                    // 跳转步骤
                    if (newFlowStepInfo.JumpCondition != "/")
                    {
                        if (newFlowStepInfo.JudgeResult)
                        {
                            if (eJudgeResult == EnumJudgeResult.RESULT_SUCCESS)
                                index = GetIndexByJumpUpId(GetNodeList, newFlowStepInfo.JumpUpIndex);//newFlowStepInfo.JumpUpIndex - 1;
                            else if (eJudgeResult == EnumJudgeResult.RESULT_NEEDADJUST)
                                index = GetIndexByJumpUpId(GetNodeList, newFlowStepInfo.JumpDwIndex);
                        }
                        else
                        {
                            index = GetIndexByJumpUpId(GetNodeList, newFlowStepInfo.JumpDwIndex);// newFlowStepInfo.JumpDwIndex - 1;
                            if (newFlowStepInfo.JumpCondition != "电容类型")
                                newFlowStepInfo.ResultFlag = newFlowStepInfo.JudgeResult;
                            mFlowCtrlResult = mFlowCtrlResult & newFlowStepInfo.ResultFlag;
                        }
                    }
                    else
                        index = GetIndexByJumpUpId(GetNodeList, newFlowStepInfo.JumpUpIndex);// newFlowStepInfo.JumpUpIndex - 1;
                    RaiseStateEvent(newFlowStepInfo.Message + "," + mAddress.ToString());
                }
                else
                {
                    // 失败退出执行
                    mFlowCtrlResult = mFlowCtrlResult & newFlowStepInfo.ResultFlag;
                    mFlowStepInof.Insert(recIndex++, newFlowStepInfo);
                    index = GetIndexByJumpUpId(GetNodeList, newFlowStepInfo.JumpDwIndex);// newFlowStepInfo.JumpDwIndex - 1;
                    RaiseStateEvent(newFlowStepInfo.Message + "," + mAddress.ToString());
                    
                    // 特殊处理 读取电容器状态
                    if (newFlowStepInfo.Name == "读取电容器状态")
                    {
                        if (mFailExecuteTimes++ > 3)
                        {
                            mFailExecuteTimes = 0;
                            //mFlowCtrlResult = mFlowCtrlResult & newFlowStepInfo.ResultFlag;
                            index = -1;   // 结束
                        }
                    }
                }
            }

            // 结束测试，记录测试记录
            mFlowCtrlState = EnumFlowCtrlState.FLOWCTRL_STOP;
            CreateFlowCtrlResultXml();
            if (mFlowCtrlResult)
                RaiseStateEvent(mName + ",0,测试合格,测试合格" + "," + mAddress.ToString());
            else
                RaiseStateEvent(mName + ",0,测试不合格,测试不合格" + "," + mAddress.ToString());
        }

       
        private int GetIndexByJumpUpId(XmlNodeList lstNode, int jumpUpIndex)
        {
            //throw new NotImplementedException();
            int nresult = -1;

            if (jumpUpIndex < 0) return nresult;

            for (int i = 0; i < lstNode.Count; i++)
            {
                if (Convert.ToInt32(lstNode[i].Attributes["id"].Value) == jumpUpIndex)
                {
                    nresult = i;
                    break;
                }
            }

            return nresult;
        }

        internal void AddCondition(CPutInOutRec value)
        {
            mFlowData.Clear();
            int[] adjustintime = new int[4] { 5000, 5000, 5000, 5000 };
            int[] inrushcurrentmin = new int[4] { 0x0fffffff, 0x0fffffff, 0x0fffffff, 0x0fffffff };

            for (int i = 0; i < value.GetRecCount(); i++)
            {
                CPutInOutDetail recdetail = value.GetPutInOutDetail(i);
                if (recdetail.Result)
                {
                    if (recdetail.InrushCurrent1 >500 && inrushcurrentmin[0] > recdetail.InrushCurrent1)
                    {
                        inrushcurrentmin[0] = recdetail.InrushCurrent1;
                        adjustintime[0] = recdetail.SetInTime1;
                    }
                    if (recdetail.InrushCurrent2 > 500 && inrushcurrentmin[1] > recdetail.InrushCurrent2)
                    {
                        inrushcurrentmin[1] = recdetail.InrushCurrent2;
                        adjustintime[1] = recdetail.SetInTime2;
                    }
                    if (recdetail.InrushCurrent3 > 500 && inrushcurrentmin[2] > recdetail.InrushCurrent3)
                    {
                        inrushcurrentmin[2] = recdetail.InrushCurrent3;
                        adjustintime[2] = recdetail.SetInTime3;
                    }
                    if (recdetail.InrushCurrent4 > 500 && inrushcurrentmin[3] > recdetail.InrushCurrent4)
                    {
                        inrushcurrentmin[3] = recdetail.InrushCurrent4;
                        adjustintime[3] = recdetail.SetInTime4;
                    }
                }
            }

            mFlowData.Add("投入延时1", adjustintime[0].ToString("X"));
            mFlowData.Add("投入延时2", adjustintime[1].ToString("X"));
            mFlowData.Add("投入延时3", adjustintime[2].ToString("X"));
            mFlowData.Add("投入延时4", adjustintime[3].ToString("X"));
        }

        private void CreateFlowCtrlResultXml()
        {
            int i = 0;
            string xmlResult = "";

            // 需要保存的数据
            if (mName == "电容SN设置")
            {
                if (mFlowCtrlResult)
                {
                    CGlobalCtrl.SoleCommonSN = (Convert.ToInt64(CGlobalCtrl.SoleCommonSN) + 1).ToString();
                    XMLHelper.CreateOrUpdateXmlNodeByXPath(System.Environment.CurrentDirectory + "\\数据结构\\系统配置.xml", "//sysconfig//SN", "SetSN", CGlobalCtrl.SoleCommonSN);
                }
            }

            // 生产xml测试记录信息
            if (mFlowData.Count != 0 && mFlowData.ContainsKey("电容SN"))
            {
                //mFlowData["电容SN"] = "8F2018030075";
                xmlResult += "<" + mName + " SN ='" + mFlowData["电容SN"] + "' 时间='" +
                    DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "' 结论='";
                xmlResult += mFlowCtrlResult ? "合格" : "不合格";
                xmlResult += "'> ";
            }
            else
            {
                if (mAddress - 1 >= 0)
                {
                    xmlResult += "<" + mName + " SN='" + CFunc.BytesToHexStr(CGlobalCtrl.SnToAddressTable[mAddress - 1].serail, 6) + "' 时间='" +         // "8F0000000000"
                        DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "' 结论='不合格'>";
                }
                else
                {
                    xmlResult += "<" + mName + " SN='8FXXXXXXXX" + "' 时间='" +         // "8F0000000000"
                       DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "' 结论='不合格'>";
                }
            }
            for (i = 0; i < mFlowStepInof.Count; i++)
            {
                xmlResult += mFlowStepInof[i].CreateFlowStepRecord();
            }
            xmlResult += "</" + mName + ">";

            // 保持相应测试记录数据
            XMLHelper.CreateXmlNodeByXPath(System.Environment.CurrentDirectory + "\\数据结构\\测试记录-" + (DateTime.Now.ToShortDateString().Replace("/", ".")) + ".xml", "//记录文件", "测试记录", xmlResult, "", "");
        }

        public bool GetBit(byte v, byte index)
        {
            //Contract.Requires(index + 1 <= 8);
            byte c = Convert.ToByte(((v & (1 << index)) > 0) ? 1 : 0);
            bool b = c == 0x01 ? true : false;


            return b;
        }
        /// <summary>
        /// 测试流程判断条件
        /// </summary>
        /// <param name="jumpCondition"></param>
        /// <returns></returns>
        private EnumJudgeResult JudgeCondition(string jumpCondition)
        {
            float judgevalue = 0;
            int[] judgerange = new int[3];
            string[] tmpvalues;

            switch (jumpCondition)
            {
                case "电容SN判断":
                    return mFlowData["电容SN"].Equals(mFlowData["电容SNBACK"]) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;
                case "电容器电流A":
                case "电容器电流B":
                case "电容器电流C":
                    judgevalue = Convert.ToInt32(mFlowData["电流标准值"]) + Convert.ToInt32(mFlowData["电流标准值"]) * Convert.ToInt32(mFlowData["交采精度值"]) / 100;
                    if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) > judgevalue)
                        return EnumJudgeResult.RESULT_FAIL;
                    judgevalue = Convert.ToInt32(mFlowData["电流标准值"]) - Convert.ToInt32(mFlowData["电流标准值"]) * Convert.ToInt32(mFlowData["交采精度值"]) / 100;
                    if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) < judgevalue)
                        return EnumJudgeResult.RESULT_FAIL;
                    return EnumJudgeResult.RESULT_SUCCESS;
                case "A相电流过零偏差":
                case "B相电流过零偏差":
                case "C相电流过零偏差":
                    tmpvalues = mFlowData["过零偏差"].Split(',');
                    judgerange[0] = Convert.ToInt32(tmpvalues[0]);
                    judgerange[1] = Convert.ToInt32(tmpvalues[1]);
                    if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) > judgerange[1])
                        return EnumJudgeResult.RESULT_FAIL;
                    if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) < judgerange[0])
                        return EnumJudgeResult.RESULT_FAIL;
                    return EnumJudgeResult.RESULT_SUCCESS;
                case "电容器电压A":
                case "电容器电压B":
                case "电容器电压C":
                    if (GetCapType(mFlowData["电容SN"].Substring(0, 2)))
                        judgevalue = Convert.ToInt32(mFlowData["电压标准值2"]) + Convert.ToInt32(mFlowData["电压标准值2"]) * Convert.ToInt32(mFlowData["交采精度值"]) / 100f;
                    else
                        judgevalue = Convert.ToInt32(mFlowData["电压标准值1"]) + Convert.ToInt32(mFlowData["电压标准值1"]) * Convert.ToInt32(mFlowData["交采精度值"]) / 100f;
                    if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) > judgevalue)
                        return EnumJudgeResult.RESULT_FAIL;
                    if (GetCapType(mFlowData["电容SN"].Substring(0, 2)))
                        judgevalue = Convert.ToInt32(mFlowData["电压标准值2"]) - Convert.ToInt32(mFlowData["电压标准值2"]) * Convert.ToInt32(mFlowData["交采精度值"]) / 100f;
                    else
                        judgevalue = Convert.ToInt32(mFlowData["电压标准值1"]) - Convert.ToInt32(mFlowData["电压标准值1"]) * Convert.ToInt32(mFlowData["交采精度值"]) / 100f;
                    if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) < judgevalue)
                        return EnumJudgeResult.RESULT_FAIL;
                    return EnumJudgeResult.RESULT_SUCCESS;
                case "投入延时1":
                case "投入延时2":
                case "投入延时3":
                case "投入延时4":
                case "设置投入延时1":
                case "设置投入延时2":
                case "设置投入延时3":
                case "设置投入延时4":
                case "实时投入延时1":
                case "实时投入延时2":
                case "实时投入延时3":
                case "实时投入延时4":

                    tmpvalues = mFlowData["投入延时"].Split(',');
                    judgerange[0] = Convert.ToInt32(tmpvalues[0]);
                    judgerange[1] = Convert.ToInt32(tmpvalues[1]);

                    if (jumpCondition == "实时投入延时1" || jumpCondition == "实时投入延时2"|| jumpCondition == "实时投入延时3"|| jumpCondition == "实时投入延时4")
                    {
                        if (frmAutoTest.mTestCount > 0)
                        {
                            return EnumJudgeResult.RESULT_SUCCESS;
                        }
                        else
                        {
                            if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) > judgerange[1])
                            {
                                mFlowData[jumpCondition] = "5000";
                            }
                            if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) < judgerange[0])
                            {
                                mFlowData[jumpCondition] = "5000";
                            }
                            return EnumJudgeResult.RESULT_SUCCESS;
                        }
                    }
                    
                    if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) > judgerange[1])
                        return EnumJudgeResult.RESULT_FAIL;
                    if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) < judgerange[0])
                        return EnumJudgeResult.RESULT_FAIL;
                    return EnumJudgeResult.RESULT_SUCCESS;
                case "切除延时1":
                case "切除延时2":
                case "切除延时3":
                case "切除延时4":
                case "实时切除延时1":
                case "实时切除延时2":
                case "实时切除延时3":
                case "实时切除延时4":
                    tmpvalues = mFlowData["切除延时"].Split(',');
                    judgerange[0] = Convert.ToInt32(tmpvalues[0]);
                    judgerange[1] = Convert.ToInt32(tmpvalues[1]);
                    if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) > judgerange[1])
                        return EnumJudgeResult.RESULT_FAIL;
                    if (Convert.ToInt32(mFlowData[jumpCondition] == "/" ? "0" : mFlowData[jumpCondition]) < judgerange[0])
                        return EnumJudgeResult.RESULT_FAIL;
                    return EnumJudgeResult.RESULT_SUCCESS;
                case "电容类型":
                    return GetCapType(mFlowData["电容SN"].Substring(0, 2)) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;
                case "参数对比1":
                    return Convert.ToInt32(mFlowData["投入延时1"], 16).ToString().Equals(mFlowData["确认投入延时1"]) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;//Convert.ToInt32(mFlowData[strGetContentKey].Trim(),16).ToString()
                case "参数对比2":
                    return Convert.ToInt32(mFlowData["投入延时2"], 16).ToString().Equals(mFlowData["确认投入延时2"]) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;
                case "参数对比3":
                    return Convert.ToInt32(mFlowData["投入延时3"], 16).ToString().Equals(mFlowData["确认投入延时3"]) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;
                case "参数对比4":
                    return Convert.ToInt32(mFlowData["投入延时4"], 16).ToString().Equals(mFlowData["确认投入延时4"]) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;
                case "参数对比5":
                    return Convert.ToInt32(mFlowData["切除延时1"], 16).ToString().Equals(mFlowData["确认切除延时1"]) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;
                case "参数对比6":
                    return Convert.ToInt32(mFlowData["切除延时2"], 16).ToString().Equals(mFlowData["确认切除延时2"]) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;
                case "参数对比7":
                    return Convert.ToInt32(mFlowData["切除延时3"], 16).ToString().Equals(mFlowData["确认切除延时3"]) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;
                case "参数对比8":
                    return Convert.ToInt32(mFlowData["切除延时4"], 16).ToString().Equals(mFlowData["确认切除延时4"]) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;
                case "参数对比9":
                    return mFlowData["投入延时2"].Equals(mFlowData["确认投入延时2"]) ? EnumJudgeResult.RESULT_SUCCESS : EnumJudgeResult.RESULT_FAIL;
                case "电容容量1判断":
                case "电容容量2判断":
                    if (GetCapType(mFlowData["电容SN"].Substring(0, 2)))
                    {

                    }
                    else
                    {
                        judgerange[0] = judgerange[1] = Convert.ToInt32(mFlowData["电容器电流A"] == "/" ? "0" : mFlowData["电容器电流A"], 10);
                        if (judgerange[0] < Convert.ToInt32(mFlowData["电容器电流B"] == "/" ? "0" : mFlowData["电容器电流B"], 10))
                            judgerange[0] = Convert.ToInt32(mFlowData["电容器电流B"] == "/" ? "0" : mFlowData["电容器电流B"], 10);

                        if (judgerange[0] < Convert.ToInt32(mFlowData["电容器电流C"] == "/" ? "0" : mFlowData["电容器电流C"], 10))
                            judgerange[0] = Convert.ToInt32(mFlowData["电容器电流C"] == "/" ? "0" : mFlowData["电容器电流C"], 10);

                        if (judgerange[1] > Convert.ToInt32(mFlowData["电容器电流B"] == "/" ? "0" : mFlowData["电容器电流B"], 10))
                            judgerange[1] = Convert.ToInt32(mFlowData["电容器电流B"] == "/" ? "0" : mFlowData["电容器电流B"], 10);

                        if (judgerange[1] > Convert.ToInt32(mFlowData["电容器电流C"] == "/" ? "0" : mFlowData["电容器电流C"], 10))
                            judgerange[1] = Convert.ToInt32(mFlowData["电容器电流C"] == "/" ? "0" : mFlowData["电容器电流C"], 10);

                        if ((judgerange[1] < 2500) || ((judgerange[0] - judgerange[1]) > 5000))
                            return EnumJudgeResult.RESULT_FAIL;
                    }
                    try
                    {
                        if (GetCapType(mFlowData["电容SN"] == "/" ? "8F" : mFlowData["电容SN"].Substring(0, 2)))
                        {
                            if (jumpCondition == "电容容量1判断")
                            {
                                judgerange[0] = Convert.ToInt32(mFlowData["确认电容容量1"], 10);// + Convert.ToInt32(mFlowData["确认电容容量2"], 10);
                                judgerange[1] = Convert.ToInt32(mFlowData["电容器电流A"], 10);
                            }
                            else
                            {
                                judgerange[0] = Convert.ToInt32(mFlowData["确认电容容量2"], 10);// + Convert.ToInt32(mFlowData["确认电容容量2"], 10);
                                judgerange[1] = Convert.ToInt32(mFlowData["电容器电流B"], 10);
                            }

                            if ((judgerange[1] < (judgerange[0] * 1000)) || judgerange[1] > (judgerange[0] * 1500))
                                return EnumJudgeResult.RESULT_FAIL;
                        }
                        else
                        {
                            judgerange[0] = Convert.ToInt32(mFlowData["确认电容容量1"], 10);// + Convert.ToInt32(mFlowData["确认电容容量2"], 16);
                            if ((judgerange[1] < (judgerange[0] * 1100)) || judgerange[1] > (judgerange[0] * 1500))
                                return EnumJudgeResult.RESULT_FAIL;
                        }
                    }
                    catch (Exception ex)
                    {
                        return EnumJudgeResult.RESULT_FAIL;
                    }
                    return EnumJudgeResult.RESULT_SUCCESS;
                case "电容器涌流A":
                    if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流特大倍数"]) * Convert.ToInt32(mFlowData["电容器电流A"]) * 14)
                        || (Convert.ToInt32(mFlowData[jumpCondition]) < 500))
                    {
                        // > 涌流特大倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                        {
                            AdjustPutInTime(1, 0);
                            return EnumJudgeResult.RESULT_NEEDADJUST;
                        }
                        else
                        {
                            return EnumJudgeResult.RESULT_FAIL;
                        }
                        
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流A"]) * 14)
                        )
                    {
                        // > 涌流合格倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                        { 
                            AdjustPutInTime(1, 1);
                            return EnumJudgeResult.RESULT_NEEDADJUST;
                        }
                        else
                        {
                            return EnumJudgeResult.RESULT_FAIL;
                        }
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流阶段倍数"]) * Convert.ToInt32(mFlowData["电容器电流A"]) * 14)
                        )
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(1, 2);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流目标倍数"]) * Convert.ToInt32(mFlowData["电容器电流A"]) * 14)
                       )
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(1, 3);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                    else
                    {
                        // > 涌流优化 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(1, 4);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                //return EnumJudgeResult.RESULT_SUCCESS;
                case "电容器状态":
                    string[] strStatus = mFlowData["电容器状态"].Split(',');
                    byte[] btyStatus = new byte[10];
                    for (int i = 0; i < strStatus.Length; i++)
                    {
                        btyStatus[i] = Convert.ToByte(strStatus[i]);
                    }

                    if (GetBit(btyStatus[3], 7) || GetBit(btyStatus[5], 7) || GetBit(btyStatus[7], 7))
                    {
                        if (mJudgeTimes++ > 40)
                        {
                            mJudgeTimes = 0;
                            return EnumJudgeResult.RESULT_SUCCESS;
                        }
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }

                    mJudgeTimes = 0;
                    return EnumJudgeResult.RESULT_SUCCESS;

                case "电容器涌流B":
                    if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流特大倍数"]) * Convert.ToInt32(mFlowData["电容器电流B"]) * 14)
                        || (Convert.ToInt32(mFlowData[jumpCondition]) < 500))
                    {
                        // > 涌流合格倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                        { 
                            AdjustPutInTime(2, 0);
                            return EnumJudgeResult.RESULT_NEEDADJUST;
                        }
                        else
                        {
                            return EnumJudgeResult.RESULT_FAIL;
                        }
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流B"]) * 14))
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                        {
                            AdjustPutInTime(2, 1);
                            return EnumJudgeResult.RESULT_NEEDADJUST;
                        }
                        else
                        {
                            return EnumJudgeResult.RESULT_FAIL;
                        }
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流阶段倍数"]) * Convert.ToInt32(mFlowData["电容器电流B"]) * 14))
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(2, 2);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流目标倍数"]) * Convert.ToInt32(mFlowData["电容器电流B"]) * 14)
   )
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(2, 3);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                    else
                    {
                        // > 涌流优化 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(2, 4);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                //return EnumJudgeResult.RESULT_SUCCESS;
                case "电容器涌流C":
                    if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流特大倍数"]) * Convert.ToInt32(mFlowData["电容器电流C"]) * 14)
                        || (Convert.ToInt32(mFlowData[jumpCondition]) < 500))
                    {
                        // > 涌流合格倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                        {
                            AdjustPutInTime(3, 0);
                            return EnumJudgeResult.RESULT_NEEDADJUST;
                        }
                        else
                        {
                            return EnumJudgeResult.RESULT_FAIL;
                        }
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流C"]) * 14))
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                        {
                            AdjustPutInTime(3, 1);
                            return EnumJudgeResult.RESULT_NEEDADJUST;
                        }
                        else
                        {
                            return EnumJudgeResult.RESULT_FAIL;
                        }
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流阶段倍数"]) * Convert.ToInt32(mFlowData["电容器电流C"]) * 14))
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(3, 2);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流目标倍数"]) * Convert.ToInt32(mFlowData["电容器电流C"]) * 14)
)
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(3, 3);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                    else
                    {
                        // > 涌流优化 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(3, 4);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                //return EnumJudgeResult.RESULT_SUCCESS;

                case "电容器涌流A-2":
                    if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流特大倍数"]) * Convert.ToInt32(mFlowData["电容器电流A-2"]) * 14)
    || (Convert.ToInt32(mFlowData[jumpCondition]) < 500))
                    {
                        // > 涌流合格倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                        {
                            AdjustPutInTime(4, 0);
                            return EnumJudgeResult.RESULT_NEEDADJUST;
                        }
                        else
                        {
                            return EnumJudgeResult.RESULT_FAIL;
                        }
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流A-2"]) * 14))
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                        {
                            AdjustPutInTime(4, 1);
                            return EnumJudgeResult.RESULT_NEEDADJUST;
                        }
                        else
                        {
                            return EnumJudgeResult.RESULT_FAIL;
                        }
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流阶段倍数"]) * Convert.ToInt32(mFlowData["电容器电流A-2"]) * 14))
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(4, 2);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流目标倍数"]) * Convert.ToInt32(mFlowData["电容器电流A-2"]) * 14)
)
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(4, 3);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                    else
                    {
                        // > 涌流优化 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(4, 4);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                //return EnumJudgeResult.RESULT_SUCCESS;
                case "电容器涌流C-2":
                    if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流特大倍数"]) * Convert.ToInt32(mFlowData["电容器电流C-2"]) * 14)
    || (Convert.ToInt32(mFlowData[jumpCondition]) < 500))
                    {
                        // > 涌流合格倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                        {
                            AdjustPutInTime(5, 0);
                            return EnumJudgeResult.RESULT_NEEDADJUST;
                        }
                        else
                        {
                            return EnumJudgeResult.RESULT_FAIL;
                        }
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流C-2"]) * 14))
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                        {
                            AdjustPutInTime(5, 1);
                            return EnumJudgeResult.RESULT_NEEDADJUST;
                        }
                        else
                        {
                            return EnumJudgeResult.RESULT_FAIL;
                        }
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流阶段倍数"]) * Convert.ToInt32(mFlowData["电容器电流C-2"]) * 14))
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(5, 2);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                    else if ((Convert.ToInt32(mFlowData[jumpCondition]) * 1000 > Convert.ToInt32(mFlowData["涌流目标倍数"]) * Convert.ToInt32(mFlowData["电容器电流C-2"]) * 14)
)
                    {
                        // > 涌流目标倍数 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(5, 3);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                    else
                    {
                        // > 涌流优化 调整参数
                        if (!frmAutoTest.mCheckFlag)
                            AdjustPutInTime(5, 4);
                        return EnumJudgeResult.RESULT_NEEDADJUST;
                    }
                //return EnumJudgeResult.RESULT_SUCCESS;
                case "判断投入延时1":
                    if (Math.Abs(Convert.ToInt32(mFlowData["确认投入延时1"]) - Convert.ToInt32(mFlowData["投入延时1"], 16)) > 800) // 200
                        return EnumJudgeResult.RESULT_FAIL;
                    return EnumJudgeResult.RESULT_SUCCESS;
                case "判断投入延时2":
                    if (Math.Abs(Convert.ToInt32(mFlowData["确认投入延时2"]) - Convert.ToInt32(mFlowData["投入延时2"], 16)) > 800) // 200
                        return EnumJudgeResult.RESULT_FAIL;
                    return EnumJudgeResult.RESULT_SUCCESS;
                case "判断投入延时3":
                    if (Math.Abs(Convert.ToInt32(mFlowData["确认投入延时3"]) - Convert.ToInt32(mFlowData["投入延时3"], 16)) > 800) // 200
                        return EnumJudgeResult.RESULT_FAIL;
                    return EnumJudgeResult.RESULT_SUCCESS;
                case "判断投入延时4":
                    if (Math.Abs(Convert.ToInt32(mFlowData["确认投入延时4"]) - Convert.ToInt32(mFlowData["投入延时4"], 16)) > 800) // 200
                        return EnumJudgeResult.RESULT_FAIL;
                    return EnumJudgeResult.RESULT_SUCCESS;
                case "电容温度判断":
                    if (Convert.ToInt32(mFlowData["电容温度"]) < 0)
                        return EnumJudgeResult.RESULT_FAIL;
                    return EnumJudgeResult.RESULT_SUCCESS;
            }

            return EnumJudgeResult.RESULT_SUCCESS;
        }
        /*
        private void AdjustPutInTime(int v1, int v2, int n = 0)
        {
            int nputintime = 0;
            int nsetintime = 0;
            int nadjusttime = 0;
            string stradjustname = "";

            switch (v1)
            {
                case 1:
                    nputintime = Convert.ToInt32(mFlowData["实时投入延时1"]);
                    nsetintime = Convert.ToInt32(mFlowData["设置投入延时1"]);
                    stradjustname = "调整投入延时1";
                    break;
                case 2:
                    nputintime = Convert.ToInt32(mFlowData["实时投入延时2"]);
                    nsetintime = Convert.ToInt32(mFlowData["设置投入延时2"]);
                    stradjustname = "调整投入延时2";
                    break;
                case 3:
                    if (GetCapType(mFlowData["电容SN"].Substring(0, 2)))
                    {
                        //if (!mFlowData.ContainsKey("调整投入延时1"))
                        //{
                        //    mFlowData.Add("调整投入延时1", Convert.ToInt32(mFlowData["设置投入延时1"]).ToString("X"));
                        //}
                        nputintime = Convert.ToInt32(mFlowData["实时投入延时2"]);
                        nsetintime = Convert.ToInt32(mFlowData["设置投入延时2"]);
                        stradjustname = "调整投入延时2";
                    }
                    else
                    {
                        nputintime = Convert.ToInt32(mFlowData["实时投入延时3"]);
                        nsetintime = Convert.ToInt32(mFlowData["设置投入延时3"]);
                        stradjustname = "调整投入延时3";
                    }
                    break;
                case 4:
                    nputintime = Convert.ToInt32(mFlowData["实时投入延时3"]);
                    nsetintime = Convert.ToInt32(mFlowData["设置投入延时3"]);
                    stradjustname = "调整投入延时3";
                    break;
                case 5:
                    nputintime = Convert.ToInt32(mFlowData["实时投入延时4"]);
                    nsetintime = Convert.ToInt32(mFlowData["设置投入延时4"]);
                    stradjustname = "调整投入延时4";
                    break;
                default:
                    break;
            }

            // 获取校准的电容器信息
            //CPutInOutRec putrec = frmAutoTest.GetPutRecBySn(mFlowData["电容SN"]);
            //int nAdjust50Times = 0;
            //int nAdjust20Times = 0;

            //	如果涌流大于1.6倍，按[实时时间 +（设置时间 - 实时时间）*1 / 10]进行偏移，4500~6200之间
            if (v2 == 0)  // > 合格
            {
                //if (putrec == null)
                //    nAdjust50Times = 1;
                //else
                //{
                //    nAdjust50Times = putrec.Adjust50Times + 1;  //
                //}
                if(!mFlowData.ContainsKey("矫正类型50"))
                    mFlowData.Add("矫正类型50", "Adjust-50");
                nadjusttime = nsetintime -  50;// nputintime + (nsetintime - nputintime) / 10;nAdjust50Times *

                //if()
            }
            else if (v2 == 1)         // > 目标  如果涌流大于1.2倍，按[设置时间-（设置时间-实时时间）*1/10]进行偏移
            {
                //if (putrec == null)
                //    nAdjust20Times = 1;
                //else
                //{
                //    //putrec.Adjust20Times++;
                    
                //    nAdjust20Times = putrec.Adjust20Times + 1;
                //}
                if (!mFlowData.ContainsKey("矫正类型20"))
                    mFlowData.Add("矫正类型20", "Adjust-20");
                //putrec.Adjust20Times++;
                nadjusttime = nsetintime - 20;//(nsetintime - nputintime) / 10;nAdjust20Times * 
            }
            else     //4）	如果涌流小于1.2倍，按5us单位偏移；
            {
                if (!mFlowData.ContainsKey("矫正类型05"))
                    mFlowData.Add("矫正类型05", "Adjust-5");
                nadjusttime = nsetintime - 5;
            }

            int[] judgerange = new int[3];
            string[] tmpvalues;
            tmpvalues = mFlowData["投入延时"].Split(',');
            judgerange[0] = Convert.ToInt32(tmpvalues[0]);
            judgerange[1] = Convert.ToInt32(tmpvalues[1]);

            if (nadjusttime > judgerange[0] && nadjusttime < judgerange[1] )//&& nadjusttime > nputintime)   // 值得商榷
            {
                //nadjusttime = //Convert.ToInt32(nadjusttime.ToString(), 16);
                if (stradjustname != "")
                    mFlowData.Add(stradjustname, nadjusttime.ToString("X"));
            }

            if (!mFlowData.ContainsKey("需要矫正投入"))
                mFlowData.Add("需要矫正投入", "true");
        }*/
        private void AdjustPutInTime(int v1, int v2, int n = 0)
        {
            int nputintime = 0;
            int nsetintime = 0;
            int nbaseintime = 0;
            int nadjusttime = 0;
            string stradjustname = "";
            int relaynum = -1;

            switch (v1)
            {
                case 1:
                    nputintime = Convert.ToInt32(mFlowData["实时投入延时1"]);
                    nsetintime = Convert.ToInt32(mFlowData["设置投入延时1"]);
                    stradjustname = "调整投入延时1";
                    relaynum = 0;
                    break;
                case 2:
                    nputintime = Convert.ToInt32(mFlowData["实时投入延时2"]);
                    nsetintime = Convert.ToInt32(mFlowData["设置投入延时2"]);
                    stradjustname = "调整投入延时2";
                    relaynum = 1;
                    break;
                case 3:
                    if (GetCapType(mFlowData["电容SN"].Substring(0, 2)))
                    {
                        //if (!mFlowData.ContainsKey("调整投入延时1"))
                        //{
                        //    mFlowData.Add("调整投入延时1", Convert.ToInt32(mFlowData["设置投入延时1"]).ToString("X"));
                        //}
                        nputintime = Convert.ToInt32(mFlowData["实时投入延时2"]);
                        nsetintime = Convert.ToInt32(mFlowData["设置投入延时2"]);
                        stradjustname = "调整投入延时2";
                        relaynum = 1;
                    }
                    else
                    {
                        nputintime = Convert.ToInt32(mFlowData["实时投入延时3"]);
                        nsetintime = Convert.ToInt32(mFlowData["设置投入延时3"]);
                        stradjustname = "调整投入延时3";
                        relaynum = 2;
                    }
                    break;
                case 4:
                    nputintime = Convert.ToInt32(mFlowData["实时投入延时3"]);
                    nsetintime = Convert.ToInt32(mFlowData["设置投入延时3"]);
                    stradjustname = "调整投入延时3";
                    relaynum = 2;
                    break;
                case 5:
                    nputintime = Convert.ToInt32(mFlowData["实时投入延时4"]);
                    nsetintime = Convert.ToInt32(mFlowData["设置投入延时4"]);
                    stradjustname = "调整投入延时4";
                    relaynum = 3;
                    break;
                default:
                    break;
            }

            bool bfirstflag = false;
            // 获取校准的电容器信息
            //CPutInOutRec putrec = frmAutoTest.GetPutRecBySn(mFlowData["电容SN"]);
            // 临时记录首次投切标志
            if (CGlobalCtrl.gFirstPutInFlag.ContainsKey(mFlowData["电容SN"]))
            {
                bfirstflag = CGlobalCtrl.gFirstPutInFlag[mFlowData["电容SN"]][relaynum];
            }
            else
            {
                CGlobalCtrl.gFirstPutInFlag.Add(mFlowData["电容SN"], new bool[4] { true, true, true, true });
                bfirstflag = true;
            }

            CGlobalCtrl.gFirstPutInFlag[mFlowData["电容SN"]][relaynum] = false;

            //int nAdjust50Times = 0;
            //int nAdjust20Times = 0;

            if (bfirstflag)
            {
                nbaseintime = nputintime;
                nadjusttime = nbaseintime + 200;  // 无条件+200
            }
            else
            {
                nbaseintime = nsetintime;
            

            //	如果涌流大于2.3倍，按[实时时间 +（设置时间 - 实时时间）*1 / 10]进行偏移，4500~6200之间
            if (v2 == 0)  // > 合格
            {
                //if (putrec == null)
                //    nAdjust50Times = 1;
                //else
                //{
                //    nAdjust50Times = putrec.Adjust50Times + 1;  //
                //}
                //if (!mFlowData.ContainsKey("矫正类型50"))
                //    mFlowData.Add("矫正类型50", "Adjust-50");
                nadjusttime = nbaseintime + 200;// nputintime + (nsetintime - nputintime) / 10;nAdjust50Times *

                //if()
            }
            else if (v2 == 1)         // > 目标  如果涌流大于1.2倍，按[设置时间-（设置时间-实时时间）*1/10]进行偏移
            {
                //if (putrec == null)
                //    nAdjust20Times = 1;
                //else
                //{
                //    //putrec.Adjust20Times++;

                //    nAdjust20Times = putrec.Adjust20Times + 1;
                //}
                //if (!mFlowData.ContainsKey("矫正类型20"))
                //    mFlowData.Add("矫正类型20", "Adjust-20");
                //putrec.Adjust20Times++;
                nadjusttime = nbaseintime + 100;//(nsetintime - nputintime) / 10;nAdjust20Times * 
            }
            else if (v2 == 2)     //4）	如果涌流小于1.2倍，按5us单位偏移；
            {
                //if (!mFlowData.ContainsKey("矫正类型05"))
                //    mFlowData.Add("矫正类型05", "Adjust-5");
                nadjusttime = nbaseintime + 50;
            }
            else if (v2 == 3)
            {
                nadjusttime = nbaseintime + 20;
            }
            else { nadjusttime = nbaseintime + 10; }

            }

            int[] judgerange = new int[3];
            string[] tmpvalues;
            tmpvalues = mFlowData["投入延时"].Split(',');
            judgerange[0] = Convert.ToInt32(tmpvalues[0]);
            judgerange[1] = Convert.ToInt32(tmpvalues[1]);

            if (nadjusttime > judgerange[0] && nadjusttime < judgerange[1])//&& nadjusttime > nputintime)   // 值得商榷
            {
                //nadjusttime = //Convert.ToInt32(nadjusttime.ToString(), 16);
                if (stradjustname != "")
                    mFlowData.Add(stradjustname, nadjusttime.ToString("X"));
            }
            else
            {
                if (nsetintime > judgerange[0] && nsetintime < judgerange[1])
                { 
                    if (stradjustname != "")
                    mFlowData.Add(stradjustname, nsetintime.ToString("X"));
                }
            }

            if (!mFlowData.ContainsKey("需要矫正投入"))
                mFlowData.Add("需要矫正投入", "true");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool GetCapType(string v)
        {
            byte value = Convert.ToByte(v, 16);

            if ((value & 0x80) > 0)
                return true;
            else
                return false;
        }

        internal void Start()
        {
            try
            {
                if (mFlowThread.ThreadState == ThreadState.Stopped)
                    mFlowThread = new System.Threading.Thread(FlowExecute);
                mFlowThread.Start();
                mFlowCtrlState = EnumFlowCtrlState.FLOWCTRL_RUNNING;

            }
            catch (Exception ex)
            {
                mFlowCtrlState = EnumFlowCtrlState.FLOWCTRL_STOP;
                mFlowThread.Abort();
            }
        }

        internal void FillPutInOutRec(Dictionary<string, CPutInOutRec> dicPutInOutRec,int type = 1)
        {
            if (!mFlowData.ContainsKey("电容SN")) return;

            CPutInOutRec putInOutRec;
            //int nPassMultiple = Convert.ToInt32(mFlowData["涌流合格倍数"]);
            //int nTargetMultiple = Convert.ToInt32(mFlowData["涌流目标倍数"]);

            if (dicPutInOutRec.ContainsKey(mFlowData["电容SN"]))   // 需要异常处理
            {
                putInOutRec = dicPutInOutRec[mFlowData["电容SN"]];
            }
            else
            {
                putInOutRec = new CPutInOutRec();
                dicPutInOutRec.Add(mFlowData["电容SN"], putInOutRec);
            }

            //bool bResult = true;
            //if (GetCapType(mFlowData["电容SN"].Substring(0, 2)))
            //{
            //    if (!mFlowData.ContainsKey("电容器涌流A") || !mFlowData.ContainsKey("电容器涌流C") || !mFlowData.ContainsKey("电容器涌流A-2") || !mFlowData.ContainsKey("电容器涌流C-2"))
            //        bResult = false;
            //    else
            //    {
            //        bResult = bResult & (Convert.ToInt32(mFlowData["电容器涌流A"]) * 100 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流A"]) * 14);
            //        bResult = bResult & (Convert.ToInt32(mFlowData["电容器涌流C"]) * 100 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流C"]) * 14);
            //        bResult = bResult & (Convert.ToInt32(mFlowData["电容器涌流A-2"]) * 100 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流A-2"]) * 14);
            //        bResult = bResult & (Convert.ToInt32(mFlowData["电容器涌流C-2"]) * 100 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流C-2"]) * 14);
            //    }
            //}
            //else
            //{
            //    if (!mFlowData.ContainsKey("电容器涌流A") || !mFlowData.ContainsKey("电容器涌流C") || !mFlowData.ContainsKey("电容器涌流B") )
            //        bResult = false;
            //    else
            //    {
            //        bResult = bResult & (Convert.ToInt32(mFlowData["电容器涌流A"]) * 100 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流A"]) * 14);
            //        bResult = bResult & (Convert.ToInt32(mFlowData["电容器涌流C"]) * 100 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流C"]) * 14);
            //        bResult = bResult & (Convert.ToInt32(mFlowData["电容器涌流B"]) * 100 > Convert.ToInt32(mFlowData["涌流合格倍数"]) * Convert.ToInt32(mFlowData["电容器电流B"]) * 14);
            //    }
            //}
            if (type == 2)
            {
                //mPutInFlag = mPutInFlag & mFlowCtrlResult;
                putInOutRec.Result2 = putInOutRec.Result2 & mFlowCtrlResult;   // 目标合格
                putInOutRec.SN = mFlowData["电容SN"];     // 需要异常处理
                putInOutRec.Address = mAddress;
                putInOutRec.Times++;
                return;
            }

            putInOutRec.Result = putInOutRec.Result | mFlowCtrlResult;   // 目标合格
            putInOutRec.NeedAdjust = !mFlowCtrlResult | (mFlowData.ContainsKey("需要矫正投入") ? true : false);
            //if (putInOutRec.NeedAdjust)
            //{
            //    //if (mFlowData.ContainsKey("矫正类型50"))
            //    //    putInOutRec.Adjust50Times++;
            //    //else if(mFlowData.ContainsKey("矫正类型20"))
            //    //        putInOutRec.Adjust20Times++;
            //}

            putInOutRec.SN = mFlowData["电容SN"];     // 需要异常处理
            putInOutRec.Address = mAddress;
            putInOutRec.Times++;

            CPutInOutDetail recdetail = new 智能电容器.CPutInOutDetail();
            recdetail.Result = mFlowCtrlResult;

            if (!GetCapType(mFlowData["电容SN"].Substring(0, 2)))
            {
                recdetail.Current1 = Convert.ToInt32(mFlowData.ContainsKey("电容器电流A") ? mFlowData["电容器电流A"] : "0");
                recdetail.Current2 = Convert.ToInt32(mFlowData.ContainsKey("电容器电流B") ? mFlowData["电容器电流B"] : "0");// mFlowData["电容器电流B"] ? mFlowData["电容器电流C"] : "0");
                recdetail.Current3 = Convert.ToInt32(mFlowData.ContainsKey("电容器电流C") ? mFlowData["电容器电流C"] : "0");
            }
            else
            {
                recdetail.Current1 = Convert.ToInt32(mFlowData.ContainsKey("电容器电流A") ? mFlowData["电容器电流A"] : "0");
                recdetail.Current2 = Convert.ToInt32(mFlowData.ContainsKey("电容器电流C") ? mFlowData["电容器电流C"] : "0");
                recdetail.Current3 = Convert.ToInt32(mFlowData.ContainsKey("电容器电流A-2") ? mFlowData["电容器电流A-2"] : "0"); //mFlowData["电容器电流A-2"]
                recdetail.Current4 = Convert.ToInt32(mFlowData.ContainsKey("电容器电流C-2") ? mFlowData["电容器电流C-2"] : "0"); //Convert.ToInt32(mFlowData["电容器电流C-2"]);
            }

            if (!GetCapType(mFlowData["电容SN"].Substring(0, 2)))
            {
                recdetail.InrushCurrent1 = Convert.ToInt32(mFlowData.ContainsKey("电容器涌流A") ? mFlowData["电容器涌流A"] : "0"); //Convert.ToInt32(mFlowData["电容器涌流A"]);
                recdetail.InrushCurrent2 = Convert.ToInt32(mFlowData.ContainsKey("电容器涌流B") ? mFlowData["电容器涌流B"] : "0"); //Convert.ToInt32(mFlowData["电容器涌流B"]);
                recdetail.InrushCurrent3 = Convert.ToInt32(mFlowData.ContainsKey("电容器涌流C") ? mFlowData["电容器涌流C"] : "0"); //Convert.ToInt32(mFlowData["电容器涌流C"]);
            }
            else
            {
                recdetail.InrushCurrent1 = Convert.ToInt32(mFlowData.ContainsKey("电容器涌流A") ? mFlowData["电容器涌流A"] : "0"); //Convert.ToInt32(mFlowData["电容器涌流A"]);
                recdetail.InrushCurrent2 = Convert.ToInt32(mFlowData.ContainsKey("电容器涌流C") ? mFlowData["电容器涌流C"] : "0");
                recdetail.InrushCurrent3 = Convert.ToInt32(mFlowData.ContainsKey("电容器涌流A-2") ? mFlowData["电容器涌流A-2"] : "0"); //Convert.ToInt32(mFlowData["电容器涌流A-2"]);
                recdetail.InrushCurrent4 = Convert.ToInt32(mFlowData.ContainsKey("电容器涌流C-2") ? mFlowData["电容器涌流C-2"] : "0"); //Convert.ToInt32(mFlowData["电容器涌流C-2"]);
            }

            recdetail.PutInTime1 = Convert.ToInt32(mFlowData.ContainsKey("实时投入延时1") ? mFlowData["实时投入延时1"] : "0");
            recdetail.PutInTime2 = Convert.ToInt32(mFlowData.ContainsKey("实时投入延时2") ? mFlowData["实时投入延时2"] : "0");
            recdetail.PutInTime3 = Convert.ToInt32(mFlowData.ContainsKey("实时投入延时3") ? mFlowData["实时投入延时3"] : "0");
            if (GetCapType(mFlowData["电容SN"].Substring(0, 2)))
                recdetail.PutInTime4 = Convert.ToInt32(mFlowData.ContainsKey("实时投入延时4") ? mFlowData["实时投入延时4"] : "0");

            recdetail.SetInTime1 = Convert.ToInt32(mFlowData.ContainsKey("设置投入延时1") ? mFlowData["设置投入延时1"] : "0"); //Convert.ToInt32(mFlowData["设置投入延时1"]);
            recdetail.SetInTime2 = Convert.ToInt32(mFlowData.ContainsKey("设置投入延时2") ? mFlowData["设置投入延时2"] : "0"); //Convert.ToInt32(mFlowData["设置投入延时2"]);
            recdetail.SetInTime3 = Convert.ToInt32(mFlowData.ContainsKey("设置投入延时3") ? mFlowData["设置投入延时3"] : "0"); //Convert.ToInt32(mFlowData["设置投入延时3"]);
            if (GetCapType(mFlowData["电容SN"].Substring(0, 2)))
                recdetail.SetInTime4 = Convert.ToInt32(mFlowData.ContainsKey("设置投入延时4") ? mFlowData["设置投入延时4"] : "0"); //Convert.ToInt32(mFlowData["设置投入延时4"]);

            if (putInOutRec.NeedAdjust)
            {
                recdetail.AdjustInTime1 = Convert.ToInt32(mFlowData.ContainsKey("调整投入延时1") ? mFlowData["调整投入延时1"] : recdetail.SetInTime1.ToString("X"), 16);
                recdetail.AdjustInTime2 = Convert.ToInt32(mFlowData.ContainsKey("调整投入延时2") ? mFlowData["调整投入延时2"] : recdetail.SetInTime2.ToString("X"), 16); //Convert.ToInt32(mFlowData["调整投入延时2"]);
                recdetail.AdjustInTime3 = Convert.ToInt32(mFlowData.ContainsKey("调整投入延时3") ? mFlowData["调整投入延时3"] : recdetail.SetInTime3.ToString("X"), 16);
                if (GetCapType(mFlowData["电容SN"].Substring(0, 2)))
                    recdetail.AdjustInTime4 = Convert.ToInt32(mFlowData.ContainsKey("调整投入延时4") ? mFlowData["调整投入延时4"] : recdetail.SetInTime4.ToString("X"), 16);
            }

            recdetail.PutOutTime1 = Convert.ToInt32(mFlowData.ContainsKey("实时切除延时1") ? mFlowData["实时切除延时1"] : "0"); //Convert.ToInt32(mFlowData["实时切除延时1"]);
            recdetail.PutOutTime2 = Convert.ToInt32(mFlowData.ContainsKey("实时切除延时2") ? mFlowData["实时切除延时2"] : "0"); //Convert.ToInt32(mFlowData["实时切除延时2"]);
            recdetail.PutOutTime3 = Convert.ToInt32(mFlowData.ContainsKey("实时切除延时3") ? mFlowData["实时切除延时3"] : "0"); //Convert.ToInt32(mFlowData["实时切除延时3"]);
            if (GetCapType(mFlowData["电容SN"].Substring(0, 2)))
                recdetail.PutOutTime4 = Convert.ToInt32(mFlowData.ContainsKey("实时切除延时4") ? mFlowData["实时切除延时4"] : "0"); //Convert.ToInt32(mFlowData["实时切除延时4"]);

            putInOutRec.AddPutInOutDetail(recdetail);
        }
    }
    /// <summary>
    /// 流程步骤类
    /// </summary>
    class CFlowStepInfo
    {
        /// <summary>
        /// 获取内容
        /// </summary>
        private string mRequestContext;
        public string RequestContext { get { return mRequestContext; } set { mRequestContext = value; } }

        /// <summary>
        /// 响应信息关键键值
        /// </summary>
        private string mRespondKey;
        public string RespondKey { get { return mRespondKey; } set { mRespondKey = value; } }

        /// <summary>
        /// 响应信息内容
        /// </summary>
        private string mRespondContext;
        public string RespondContext
        {
            get
            {
                return mRespondContext;                
            }
        }

        /// <summary>
        /// 步骤类型
        /// </summary>
        private string mType;
        public string Type { set { mType = value; } }

        /// <summary>
        /// 步骤名称
        /// </summary>
        private string mName;
        public string Name { get { return mName; }  set { mName = value; } }

        /// <summary>
        /// 步骤索引
        /// </summary>
        private int mIndex;
        public int Index { set { mIndex = value; } }

        /// <summary>
        /// 执行结果
        /// </summary>
        private bool mResultFlag;
        public bool ResultFlag { get { return mResultFlag; }  set { mResultFlag = value; } }
        
        /// <summary>
        /// 判断条件
        /// </summary>
        private string mJumpCondition;
        public string JumpCondition { get { return mJumpCondition; } set { mJumpCondition = value; } }

        /// <summary>
        /// 跳转索引
        /// </summary>
        private int mJumpUpIndex;
        public int JumpUpIndex { get { return mJumpUpIndex; } set { mJumpUpIndex = value; } }
        /// <summary>
        /// 跳转索引
        /// </summary>
        private int mJumpDwIndex;
        public int JumpDwIndex { get { return mJumpDwIndex; } set { mJumpDwIndex = value; } }

        /// <summary>
        /// 允许失败次数
        /// </summary>
        private int mFailTimes;
        public int FailTimes { get { return mFailTimes; } set { mFailTimes = value; } }

        /// <summary>
        /// 步骤执行的命令
        /// </summary>
        private CCommand mNewCmd;

        /// <summary>
        /// 执行信息反馈
        /// </summary>
        public string Message {
            get {
                if (mResultFlag)
                {
                    return mName + "测试成功," + mIndex.ToString() +","+ RequestContext + "," + RespondKey+":"+ RespondContext;
                }

                return mName + "测试失败," + mIndex.ToString() + "," + RequestContext + "," + RespondKey + ":" + RespondContext;
            }
        }

        /// <summary>
        /// 判断结果
        /// </summary>
        private bool mJudgeResult;
        public bool JudgeResult { get { return mJudgeResult; }  set { mJudgeResult = value; } }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="type">类型</param>
        /// <param name="index">索引</param>
        /// <param name="request">请求</param>
        /// <param name="respond">响应</param>
        /// <param name="condition">条件</param>
        /// <param name="jumpinfo">跳转</param>
        /// <param name="failtimes">允许失败次数</param>
        public CFlowStepInfo(string name,string type,int index,string request,string respond,
            string condition,string jumpinfo,string failtimes)
        {           
            mResultFlag = false;
            mName = name;
            mType = type;
            mIndex = index;
            mRequestContext = request;
            mRespondKey = respond;
            mJumpCondition = condition;
            mJumpUpIndex = Convert.ToInt32(jumpinfo.Split(',')[0].Trim());
            mJumpDwIndex = Convert.ToInt32(jumpinfo.Split(',')[1].Trim());
            mFailTimes = Convert.ToInt32(failtimes);
            mNewCmd = null;
        }
        /// <summary>
        /// 生成测试信息
        /// </summary>
        /// <returns></returns>
        public string CreateFlowStepRecord()
        {
            string RecordMessage = "";

            RecordMessage += "<测试步骤 名称='" + mName + "'>";
            //if(mRequestContext == "/")
                RecordMessage += "<请求数据>" + mRequestContext + "</请求数据>";
            //else
            //    RecordMessage += "<请求数据>" + m mRequestContext + "</请求数据>";
            RecordMessage += "<响应数据>" + mRespondContext + "</响应数据>";
            if(mJumpCondition=="/")
                RecordMessage += "<判断数据>" + mJumpCondition +":/</判断数据><测试结论>";
            else
            {                
                RecordMessage += "<判断数据>" + mJumpCondition + ":";
                if (mJumpCondition == "电容类型")
                    RecordMessage += mJudgeResult ? "共补" : "分补";
                else
                    RecordMessage += mJudgeResult ? "正确" : "失败";

                RecordMessage += "</判断数据><测试结论>";
            }

            RecordMessage += mResultFlag ? "合格" : "不合格";
            RecordMessage += "</测试结论></测试步骤>";
            return RecordMessage;
        }
        /// <summary>
        /// 创建命令
        /// </summary>
        /// <param name="desc"></param>
        /// <param name="addr"></param>
        /// <param name="data"></param>
        /// <param name="comnum"></param>
        /// <returns></returns>
        internal bool CreateCmd(string desc, byte addr, byte[] data, int comnum)
        {
            if (mType == "Modbus")
            {
                // 创建数据信息
                mNewCmd = new CModbusCommand(desc, addr, data, comnum);
            }
            else if (mType == "WaitDelay")
            {
                mNewCmd = new CWaitDelay(desc);
            }
            else if (mType == "AutoNetWork")
            {
                mNewCmd = new CNetWorkCmd(comnum);
            }
            else if(mType == "Judge")
            {
                //mNewCmd = new CJudgeCmd(v1);
            }

            if (mNewCmd == null) return false;

            return true;
        }

        /// <summary>
        /// 步骤执行函数
        /// </summary>
        /// <returns></returns>
        internal bool Execute()
        {
            byte[] getvalues;
            mResultFlag =  mNewCmd.Execute();

            if (mRespondKey == "/")
                mRespondContext = "/";
            else
            {
            if (mNewCmd.CmdReturnValue() == null)
                {
                    if (mRespondKey == "生成电容SN")
                    {                        
                        mRespondKey = "电容SN";                       
                    }
                    
                    mRespondContext = "/";
                    mResultFlag = false;
                }
            else
            {
                getvalues = mNewCmd.CmdReturnValue();
                    if (mRespondKey == "生成电容SN")
                    {
                        mRespondKey = "电容SN";
                        mRespondContext = CFunc.CreateSNFromXml(getvalues[0].ToString("X2"));
                        if (mRespondContext == "/")
                            mResultFlag = false;
                    }
                    else if (mRespondKey == "电容SN")
                    {
                        if ((getvalues[1] == 0x3f || getvalues[1] == 0x8f) ||
                            (getvalues[3] == 0x3f || getvalues[3] == 0x8f))
                        {
                            mRespondContext = CFunc.BytesToHexStr(getvalues, 6);
                            mResultFlag = false;
                        }
                        else
                            mRespondContext = CFunc.BytesToHexStr(getvalues, 6);
                    }
                    else if (mRespondKey == "电容SNBACK")
                    {
                        if ((getvalues[1] == 0x3f || getvalues[1] == 0x8f) ||
                            (getvalues[3] == 0x3f || getvalues[3] == 0x8f))
                        {
                            mRespondContext = CFunc.BytesToHexStr(getvalues, 6);
                            mResultFlag = false;
                        }
                        else
                            mRespondContext = CFunc.BytesToHexStr(getvalues, 6);     // 
                    }
                    else if (mRespondKey == "电容器状态")
                    { 
                        for (int i = 0; i < getvalues.Length-1; i++)
                        {
                            mRespondContext += getvalues[i].ToString() + ",";
                        }
                        mRespondContext += getvalues[getvalues.Length - 1].ToString();
                    }
                    else
                        mRespondContext = (getvalues[0] * 256 + getvalues[1]).ToString();   
                }
            }

            return mResultFlag;
        }
    }
    /// <summary>
    /// 命令父类
    /// </summary>
    class CCommand
    {
        /// <summary>
        /// 命令类执行函数
        /// </summary>
        /// <returns></returns>
        virtual public bool  Execute()
        {
            return false;
        }
        /// <summary>
        /// 返回结果内容
        /// </summary>
        /// <returns></returns>
        virtual public byte[] CmdReturnValue()
        {
            return null;
        }

    }
    /// <summary>
    /// CJudgeCmd 判断命令
    /// </summary>
    class CJudgeCmd : CCommand
    {

        private string mName;

        private Dictionary<string, string> mDicValues;
        public CJudgeCmd(string name,Dictionary<string,string> dicValues)
        {
            mName = name;

            mDicValues = dicValues;

        }

        override public bool Execute()
        {
            switch (mName)
            {
                case "电容SN判断":
                    return mDicValues["电容SN"].Equals(mDicValues["电容SNBACK"]);
                    
                
            }

            return true;
        }
    }
    /// <summary>
    /// CNetWorkCmd 组网命令
    /// </summary>
    class CNetWorkCmd : CCommand
    {
        //private int mDelayTime;

        private CNetWork network;

        //private Timer mTimer;
        public CNetWorkCmd(int commnum)
        {
            network = new 智能电容器.CNetWork(CGlobalCtrl.Comms[commnum]);
        }

        override public bool Execute()
        {            
            network.autonetworking();
            return true;
        }
    }
    /// <summary>
    /// CWaitDelay 等待延时命令
    /// </summary>
    class CWaitDelay : CCommand
    {
        /// <summary>
        /// 延时时间
        /// </summary>
        private int mDelayTime;
        /// <summary>
        /// 计数器
        /// </summary>
        private Timer mTimer;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="desc"></param>
        public CWaitDelay(string desc)
        {
            mDelayTime = Convert.ToInt32(desc);
        }
        /// <summary>
        /// 执行函数
        /// </summary>
        /// <returns></returns>
        override public bool Execute()
        {
            mTimer = new Timer(DelayExecue, null, 0, 1000);

            while (true)
            {
                if (mDelayTime < 0) break;
            }

            mTimer.Dispose();
            return true;
        }

        /// <summary>
        /// 计数器函数
        /// </summary>
        /// <param name="obj"></param>
        public void DelayExecue(object obj)
        {
            mDelayTime--;           
        }
    }
    /// <summary>
    /// CModbusCommand Modbus命令
    /// </summary>
    class CModbusCommand : CCommand
    {
        /// <summary>
        /// 通讯帧
        /// </summary>
        private CCommFrame mCmdFrame;

        /// <summary>
        /// 规约类型
        /// </summary>
        private CProtocol mPrtl;

        /// <summary>
        /// 命令描述，用于构成命令的必要信息
        /// </summary>
        private string mDesc;

        /// <summary>
        /// 命令状态
        /// </summary>
        private EnumCmdState mCmdState;

        /// <summary>
        /// 通信规约的处理内容
        /// </summary>
        private FrameHeadStr mFrameHeadStr;

        /// <summary>
        /// 使用的通信通道
        /// </summary>
        private int mCommNum;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="desc">命令描述</param>
        /// <param name="addr">对象地址</param>
        /// <param name="values">帧数据</param>
        /// <param name="CommNum">通道号</param>
        public CModbusCommand(string desc, byte addr, byte[] values, int CommNum)
        {
            mDesc = desc;
            mCommNum = CommNum;
            mFrameHeadStr.address = addr;
            FillFrameHeadStr();
            if (values != null)
            {
                if (mFrameHeadStr.funcode == 0x06 && mFrameHeadStr.affirmstr.starthi == 0x10 &&
                mFrameHeadStr.affirmstr.startlw == 0x36)
                {
                    mFrameHeadStr.values = new byte[values.Length];
                    mFrameHeadStr.values[0] = values[1];
                    mFrameHeadStr.values[1] = addr;
                    //Array.Copy(values, mFrameHeadStr.values, values.Length);
                }
                else
                { 
                    mFrameHeadStr.values = new byte[values.Length];
                    Array.Copy(values, mFrameHeadStr.values, values.Length);
                }
            }
            mPrtl = new CModbushost();
            mCmdFrame = new CCommFrame();
        }
        /// <summary>
        /// 命令执行函数
        /// </summary>
        /// <returns></returns>
        override public bool Execute()
        {
            int sendlen=0;
            byte[] sendbuf = new byte[256];
            //
            while (true)
            {
                switch (mCmdState)
                {
                    case EnumCmdState.CMD_START:
                        // 获取命令描述
                        //sendlen = mPrtl.makeframe(ref mFrameHeadStr, sendbuf, false);
                        sendlen = mPrtl.makeframe(CFunc.StructToBytes(mFrameHeadStr), Marshal.SizeOf(mFrameHeadStr), sendbuf, ref sendlen);
                        //mCmdFrame = new CCommFrame();
                        mCmdFrame.FillContent(sendbuf, sendlen, mCommNum);
                        mCmdFrame.FrameRecvDelay = 3;
                        CGlobalCtrl.Comms[mCommNum].FillFrame(mCmdFrame);
                        mCmdState = EnumCmdState.CMD_AFFIRM;
                        break;
                    case EnumCmdState.CMD_AFFIRM:
                        // 帧接受成功  
                        if (mCmdFrame.CommState == EnumCommState.COMM_RECV_SUCC || mCmdFrame.CommState == EnumCommState.COMM_RECV_ERROR)
                        {
                            if (mCmdFrame.CommState == EnumCommState.COMM_RECV_SUCC)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        if (mCmdFrame.CommState == EnumCommState.COMM_TIME_OUT)
                        {
                            if (mCmdFrame.Address == 0)
                            {
                                return true;
                            }

                            // 
                            if (mCmdFrame.RetryTimes >= 2)
                            {
                                return false;
                            }
                            else
                            {
                                mCmdFrame.RetryTimes++;
                                mCmdFrame.CommState = EnumCommState.COMM_WAIT;
                                mCmdState = EnumCmdState.CMD_START;
                            }
                        }
                        break;
                }
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 通信帧装载
        /// </summary>
        private void FillFrameHeadStr()
        {
            string[] descs = mDesc.Split(',');

            mFrameHeadStr.funcode = Convert.ToByte(descs[0], 16);
            mFrameHeadStr.affirmstr.starthi = Convert.ToByte(descs[1], 16);
            mFrameHeadStr.affirmstr.startlw = Convert.ToByte(descs[2], 16);
            mFrameHeadStr.affirmstr.amounthi = Convert.ToByte(descs[3], 16);
            mFrameHeadStr.affirmstr.amountlw = Convert.ToByte(descs[4], 16);

            // 广播复归命令
            if (mFrameHeadStr.funcode == 0x0f && mFrameHeadStr.affirmstr.starthi == 0x20 &&
                mFrameHeadStr.affirmstr.startlw == 0x00 && mFrameHeadStr.affirmstr.amounthi == 0x00 &&
                mFrameHeadStr.affirmstr.amountlw == 0x06)
            {
                mFrameHeadStr.address = 0x00;
                mFrameHeadStr.values = new byte[1];
                mFrameHeadStr.values[0] = 0x3f;
            }           

        }

        /// <summary>
        /// 命令返回值
        /// </summary>
        /// <returns></returns>
        override public byte[] CmdReturnValue()
        {
            return mCmdFrame.GetRecvContent();
        }
    }
}
