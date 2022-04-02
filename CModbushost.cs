using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

namespace 智能电容器
{
    enum ENUMDATATYPE
    {
        BCD_TYPE,
        BIN_TYPE,
        ACSII_TYPE
    };

    enum ENUMPARAMITEM
    {
        // 参数
        PARAM_ADDRESS = 0,              //	设备逻辑地址即通信地址
        PARAM_SERIAL,                       //	设备物理地址即出厂编号
        PARAM_OVERVOL,                  //	过压门限
        PARAM_LOWVOL,                       //	欠压门限
        PARAM_OVERTEMPERATURE,  // 	过温门限
                                //PARAM_ONVAR,
                                //PARAM_OFFVAR,
        PARAM_ACTIONDELAYA,         // 	动作延时A
        PARAM_ACTIONDELAYB,         //	动作延时B
        PARAM_ACTIONDELAYC,         //	动作延时C
        PARAM_ACTIONDELAYD,         //	动作延时C
        PARAM_ITEM_NUM                  //  参数数量编号
    };

    enum ENUMSTATEITEM
    {
        // 遥信
        STATE_RELAY,                        //	继电器状态
        STATE_WORK,                         //	工作状态
        STATE_ITEM_NUM                  //
    };


    enum ENUMREALTIMEITEM
    {
        // 遥测
        REALTIME_VOL,                       //	实时电压
        REALTIME_CUR,                       //	实时电流
        REALTIME_TEMPERATURE,       //	实时温度
        REALTIME_ITEM_NUM
    }
       ;

    enum ENUMSTATITEM
    {
        // 统计
        STAT_RELAYACTIONNUMA,       // 继电器1动作次数
        STAT_RELAYACTIONNUMB,       // 继电器2动作次数
        STAT_RELAYACTIONNUMC,       // 继电器3动作次数
        STAT_RELAYACTIONNUMD,       // 继电器4动作次数
        STAT_PUTINVARAMOUNT,        // 投入容量
        STAT_PUTINVARTIME,          // 投入时间
        STAT_OFFVARTIME,                // 切除时间
        STAT_VARRUNTIME,                // 运行时间
        STAT_ITEM_NUM
    }
       ;

    enum ENUMCTRLITEM
    {
        CTRL_ONVAR,                         //	投入电容器
        CTRL_OFFVAR,                        //  切除电容器		
        CTRL_ITEM_NUM
    }
       ;

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    public struct RegistInfo
    {
        public Byte starthi;              // 起始地址高
        public Byte startlw;              // 起始地址低
        public Byte amounthi;             // 输出数量高
        public Byte amountlw;               // 输出数量低
    };
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    struct SubItemInfo
    {
        public UInt16 value;               // 输出值或输出数量
        public UInt16 subitemid;           // 规约子项编号,对应上面参数
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    public struct FrameHeadStr
    {

        public Byte address;          // 设备地址
        public Byte funcode;          // 规约功能
        //public SubItemInfo subitemstr;     // 响应组帧数据
        public RegistInfo affirmstr;           // 只在规约解析内部使用，调用者无需考虑
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]  //[StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
        public Byte[] values;             // 当写多个线圈或寄存器时使用该变量传递设置值
    };

    enum EnumCheckState
    {
        CHECK_START,
        CHECK_AFFIRMSEND,
        CHECK_AFFIRMWAIT,
        CHECK_NEXT,
        CHECK_OVER
    };

    enum EnumNetWorkState
    {
        NETWORK_START,
        NETWORK_REGISTWAIT,
        NETWORK_BUILDNEW,
        NETWORK_BUILDNEXT,
        //	NETWORK_BROADCASTWAIT,
        NETWORK_OVER
    };

    enum EnumAutoNetWorkState
    {
        NETWORK_INIT = 0,                               // 组网信息初始化
        CHECK_VAR_VALID,                                // 
        VARNOTE_NETWORK,
        CHECK_VAR_VALID_AGAIN,
        VARNOTE_DOCUPDATE,
        AUTONET_OVER
        //VAR_COMM_IDLE,
        //VAR_COMM_BISY
    };
    public struct SnToAddressStr
    {
        public int address;
        public Byte[] serail;
        public int validflag;        
    };


    public class CNetWork
    {
        /*********************************************************************************/
        public const int FUN_READSTATE = 0x01;        // 读遥信信息
        public const int FUN_READPARAM = 0x02;        // 读参数信息
        public const int FUN_READREALTIME = 0x03;     // 读实时数据
        public const int FUN_READSTATDATA = 0x04;     // 读统计数据
        public const int FUN_WRITEPARAM = 0x05;       // 写参数信息
        public const int FUN_WRITECTRL = 0x06;        // 写控制信息
        public const int FUN_NETWORKCTRL = 0x10;      // 组网命令				
        public const int FUN_NETWORKWRITE = 0x11;	  // 组网写入数据	

        
        public const int PARAM_ITEM_ADDRESS = 0x1000;           // 
        public const int STATE_ITEM_ADDRESS = 0x1100;           //
        public const int REALTIME_ITEM_ADDRESS = 0x1200;        //
        public const int STAT_ITEM_ADDRESS = 0x1300;            //
        public const int CTRL_ITEM_ADDRESS = 0x2000;            //

        /// <summary>
        /// 电容节点状态
        /// </summary>
        private const int MAX_RETRY_TIMES = 1;
        public const int SN_BUILDNEW = 0x11;
        public const int SN_NEEDAFFIRM = 0x22;
        public const int SN_NOVALID = 0x55;
        public const int SN_VALID = 0x88;

        private const int NETREGIST_COUNT_VALUE = 5;
        private const int COMMBROADCAST_COUNT_VALUE = 3;

        private int CurrItemIndex = 0;

        private int RetryTimes = 0;           //	放到全局

        // private SnToAddressStr[] CGlobalCtrl.SnToAddressTable = new SnToAddressStr[MAX_VARNODE_NUM];

        private EnumAutoNetWorkState CurrAutoNetWorkState;
        private EnumCheckState CurrCheckState;
        private EnumNetWorkState CurrNetWorkState;

        //int[] ParamItemLen = new int[(int)ENUMPARAMITEM.PARAM_ITEM_NUM] {
        //            2,      //	设备逻辑地址即通信地址
        //            6,      //	设备物理地址即出厂编号
        //            2,      //	过压门限
        //            2,      //	欠压门限
        //            2,      // 	过温门限
        //            2,      // 	动作延时A
        //            2,      // 	动作延时B
        //            2,      // 	动作延时C
        //            2 };        // 	动作延时D	


        //int[] StateItemLen = new int[(int)ENUMSTATEITEM.STATE_ITEM_NUM] {
        //        1,
        //        1
        //    };

        //int[] RealtimeItemLen = new int[(int)ENUMREALTIMEITEM.REALTIME_ITEM_NUM]{
        //        2,
        //        2,
        //        2
        //    };
        //int[] StatItemLen = new int[(int)ENUMSTATITEM.STAT_ITEM_NUM] {
        //        2,
        //        2,
        //        2,
        //        2,
        //        2,
        //        6,
        //        6,
        //        4
        //    };
        //int[] CtrlItemLen = new int[(int)ENUMCTRLITEM.CTRL_ITEM_NUM] {
        //        1,
        //        1
        //    };

        private CCommFrame CommFrame;

        private CComm mComm;

        public delegate void NetWorkStateHandle(object sender, StateEventArgs e);

        /// 通信通道事件 
        public event NetWorkStateHandle StateEvent;

        public System.Threading.Thread mNetThread;

        public CNetWork(CComm comm)
        {
            mComm = comm;
            mComm.CommEvent += MComm_CommEvent;

            for (int i = 0; i < CGlobalCtrl.MAX_VARNODE_NUM; i++)
            {               
                CGlobalCtrl.SnToAddressTable[i].validflag = SN_NEEDAFFIRM;
            }

            mNetThread = new System.Threading.Thread(autonetworking);
        }

        private void RaiseStateEvent(string msg)
        {
            if (StateEvent != null)
            {
                msg = "通道" + mComm.CommNum.ToString() + msg;
                StateEvent(this, new 智能电容器.StateEventArgs(msg, mComm.CommNum));
            }
        }

        private void MComm_CommEvent(object sender, CommEventArgs e)
        {
            Byte[] capsn = new Byte[6];
            int regIndex = 0;
            CCommFrame CommFrame = e.CommFrame;

            //Byte capsn_ys[6];

            // 按照映射地址直接存储，不是BCD码的转换为BCD码存储
            //	switch(framehead.funcode)
            //	{
            //		case FUN_READSTATE:
            //				switch()
            //		break;
            //		
            //	}

            SnToAddressStr CurrTableItem = new SnToAddressStr();

            // 处于组网状态
            if (CurrAutoNetWorkState == EnumAutoNetWorkState.CHECK_VAR_VALID || CurrAutoNetWorkState == EnumAutoNetWorkState.CHECK_VAR_VALID_AGAIN)
            {
                if (CommFrame.FrameIndex == CModbushost.FUN_READBC)//FUN_READPARAM)
                {
                    if (CurrItemIndex >= 32) return;
                    CurrTableItem = CGlobalCtrl.SnToAddressTable[CurrItemIndex];
                    if (CommFrame.GetRecvContent().Equals(CurrTableItem.serail))
                    {
                        CurrTableItem.validflag = SN_VALID;
                    }
                }
            }
            else if (CurrAutoNetWorkState == EnumAutoNetWorkState.VARNOTE_NETWORK)
            {
                CDebugInfo.writeconsole("regist1:", regIndex.ToString());
                if (CommFrame.FrameResope == CModbushost.SUC_REGISTRTU)
                {
                    
                    regIndex = GetAddSnToAddress(SN_NOVALID);
                    CDebugInfo.writeconsole("regist2:", regIndex.ToString());
                    if (CommFrame.FrameIndex == CModbushost.FUN_REGISTRTU && regIndex >= 0)
                    {
                        
                        CGlobalCtrl.SnToAddressTable[regIndex].address = regIndex + 1;
                        Array.Copy(CommFrame.GetRecvContent(),1, CGlobalCtrl.SnToAddressTable[regIndex].serail,0, 6);
                        CGlobalCtrl.SnToAddressTable[regIndex].validflag = SN_BUILDNEW;

                        CDebugInfo.writeconsole("regist:",regIndex.ToString());
                        
                    }
                }
            }
        }

        private int GetSnToAddressItemIndex()
        {
            throw new NotImplementedException();
        }

        /*********************************************************************************
        *	模块编号：
        *	名    称：
        *	功    能：获取需要处理的电容器节点
        *	输入参数：
        *	返 回 值：
        *	修改日志： 
        *	[2017-4-19 8:45]? Ver. 1.00
        *	开始编写；
        *			完成； 
        *********************************************************************************/
        int GetAddSnToAddress(Byte needtype)
        {
            int i = 0;
            for ( i = 0; i < CGlobalCtrl.MAX_VARNODE_NUM; i++)
            {      
                if (CGlobalCtrl.SnToAddressTable[i].validflag == needtype)
                {
                    return i;
                }
           }
            return -1;
        }
        bool GetNeedHandleVar(Byte needtype)
        {
            SnToAddressStr CurrTableItem;

            for (; CurrItemIndex < CGlobalCtrl.MAX_VARNODE_NUM; CurrItemIndex++)
            {
                CurrTableItem = CGlobalCtrl.SnToAddressTable[CurrItemIndex];                        //			
                if (CurrTableItem.address <= 0)
                {
                    CGlobalCtrl.SnToAddressTable[CurrItemIndex].validflag = SN_NOVALID;
                    //return false;
                }
                else if (BitConverter.ToString(CurrTableItem.serail).Equals("00-00-00-00-00-00"))
                {
                    CGlobalCtrl.SnToAddressTable[CurrItemIndex].validflag = SN_NOVALID;
                    //return false;
                }
                else if (CurrTableItem.validflag == needtype)                           //
                    return true;
            }

            //CGlobalCtrl.SnToAddressTable[CurrItemIndex].validflag = SN_NOVALID;
            return false;
        }

        private int mTimerNum = -1;
        /*********************************************************************************
        *	模块编号：
        *	名    称：
        *	功    能：网内ID,缓存区ID初始化
        *	输入参数：
        *	返 回 值：
        *	修改日志： 
        *	[2017-4-19 8:45]? Ver. 1.00
        *	开始编写；
        *			完成； 
        *********************************************************************************/
        void networking()
        {
            FrameHeadStr framehead = new FrameHeadStr();
            Byte[] sendbuff = new Byte[255];            
            Byte[] registBytes = new Byte[7];
            SnToAddressStr CurrTableItem;
            
            int sendlen=0;

            // 发送组网命令
            switch (CurrNetWorkState)
            {
                case EnumNetWorkState.NETWORK_START:
                    RaiseStateEvent("广播发送组网命令");
                    framehead.address = 0;                                              // 广播发送组网命令
                    framehead.funcode = CModbushost.FUN_REGISTRTU;
                    framehead.values = new Byte[10];
                    framehead.values[0] = 2;                                            // 上报
                    framehead.values[1] = 10;                                           // 在10S内完成
                    //sendlen = mComm.Protocol.makeframe(ref framehead, sendbuff,false);                         //  
                    sendlen = mComm.Protocol.makeframe(CFunc.StructToBytes(framehead), Marshal.SizeOf(framehead), sendbuff, ref sendlen);
                    CommFrame = new CCommFrame();
                    CommFrame.FrameRecvDelay = NETREGIST_COUNT_VALUE;
                    CommFrame.FillContent(sendbuff, sendlen, mComm.CommNum);
                    mComm.FillFrame(CommFrame);
                    
                    //mTimerNum = mComm.AddTimerTick(NETREGIST_COUNT_VALUE);//NETREGIST_COUNT_VALUE
                    
                    CurrNetWorkState = EnumNetWorkState.NETWORK_REGISTWAIT;
                    break;
                case EnumNetWorkState.NETWORK_REGISTWAIT:
                    if (CommFrame.FrameRecvDelay == 0)
                    {
                        CurrItemIndex = 0;
                        if (GetNeedHandleVar(SN_BUILDNEW))
                        {
                            CurrNetWorkState = EnumNetWorkState.NETWORK_BUILDNEW;            //	
                        }
                        else
                            CurrNetWorkState = EnumNetWorkState.NETWORK_OVER;
                    }
                    break;
                case EnumNetWorkState.NETWORK_BUILDNEW:
                    RaiseStateEvent("组网分配地址命令:" + CurrItemIndex.ToString());
                    CurrTableItem = CGlobalCtrl.SnToAddressTable[CurrItemIndex];
                    CGlobalCtrl.SnToAddressTable[CurrItemIndex].validflag = SN_NEEDAFFIRM;
                    framehead.address = (Byte)CurrTableItem.address;             // 分配地址   triumph.sha temp handle
                    framehead.funcode = CModbushost.FUN_WRITEVARADDR;
                    framehead.affirmstr.amountlw = 7;
                    registBytes[0] = (Byte)CurrTableItem.address;
                    Array.Copy(CurrTableItem.serail, 0,registBytes, 1,  6);                    
                    framehead.values = new Byte[10];
                    Array.Copy(registBytes, 0, framehead.values, 0,  7);
                    //sendlen=mComm.Protocol.makeframe(ref framehead, sendbuff, false);
                    sendlen = mComm.Protocol.makeframe(CFunc.StructToBytes(framehead), Marshal.SizeOf(framehead), sendbuff, ref sendlen);
                    CommFrame = new CCommFrame();
                    CommFrame.FrameRecvDelay = COMMBROADCAST_COUNT_VALUE;
                    CommFrame.FillContent(sendbuff, sendlen, mComm.CommNum);
                    mComm.FillFrame(CommFrame);
                    //mTimerNum = mComm.AddTimerTick(COMMBROADCAST_COUNT_VALUE);
                    CurrNetWorkState = EnumNetWorkState.NETWORK_BUILDNEXT;
                    break;        
                case EnumNetWorkState.NETWORK_BUILDNEXT:
                    if (CommFrame.FrameRecvDelay == 0)
                    {
                        CurrItemIndex++;
                        if (GetNeedHandleVar(SN_BUILDNEW))
                        {
                            CurrNetWorkState = EnumNetWorkState.NETWORK_BUILDNEW;            //	
                        }
                        else
                            CurrNetWorkState = EnumNetWorkState.NETWORK_OVER;
                    }
                    break;
                case EnumNetWorkState.NETWORK_OVER:
                    CurrItemIndex = 0;
                    CurrCheckState = EnumCheckState.CHECK_START;
                    CurrAutoNetWorkState = EnumAutoNetWorkState.CHECK_VAR_VALID_AGAIN;
                    break;
            }
        }

        /// <summary>
        ///  
        /// </summary>
        public void StartNetWork()
        {
            mNetThread.Start();
        }

        /*********************************************************************************
        *	模块编号：
        *	名    称：
        *	功    能：
        *	输入参数：
        *	返 回 值：
        *	修改日志： 
        *	[2017-4-19 8:45]? Ver. 1.00
        *	开始编写；
        *			完成； 
        *********************************************************************************/
        void checkvarvalid()
        {
            Byte[] SendBuf = new Byte[240];             //	放到全局	
            	
            int sendlen = 0;
                                           //int setvarnodenum = 5;  // 电容配置参数读取
            FrameHeadStr framehead=new FrameHeadStr();
            SnToAddressStr CurrTableItem;  //CurrTableItem = CGlobalCtrl.SnToAddressTable[CurrItemIndex];
            
            switch (CurrCheckState)
            {
                case EnumCheckState.CHECK_START:
                    RaiseStateEvent("CHECK_START");
                    CurrItemIndex = 0;
                    if (GetNeedHandleVar(SN_NEEDAFFIRM))
                    {
                        CurrCheckState = EnumCheckState.CHECK_AFFIRMSEND;
                    }
                    else
                        CurrCheckState = EnumCheckState.CHECK_OVER;
                    break;
                case EnumCheckState.CHECK_AFFIRMSEND:
                    RaiseStateEvent("CHECK_AFFIRMSEND:"+ CurrItemIndex.ToString());
                    CurrTableItem = CGlobalCtrl.SnToAddressTable[CurrItemIndex];
                    framehead.address = (Byte)CurrTableItem.address; 
                    framehead.funcode = CModbushost.FUN_READBC;                                                                 //  
                    framehead.affirmstr.startlw =0x01;
                    framehead.affirmstr.starthi =0x10;
                    framehead.affirmstr.amountlw = 0x03;
                    framehead.affirmstr.amounthi = 0x00;
                    //sendlen = mComm.Protocol.makeframe(ref framehead, SendBuf, false);
                    sendlen = mComm.Protocol.makeframe(CFunc.StructToBytes(framehead), Marshal.SizeOf(framehead), SendBuf, ref sendlen);
                    CommFrame = new CCommFrame();
                    CommFrame.FillContent(SendBuf, sendlen, mComm.CommNum);
                    mComm.FillFrame(CommFrame);
                    CurrCheckState = EnumCheckState.CHECK_AFFIRMWAIT;
                    break;
                case EnumCheckState.CHECK_AFFIRMWAIT:
                    if (CommFrame.CommState == EnumCommState.COMM_RECV_SUCC)
                    { 
                        CurrTableItem = CGlobalCtrl.SnToAddressTable[CurrItemIndex];

                        if (CommFrame.GetRecvContent()!=null && CFunc.EqualArray(CommFrame.GetRecvContent(),CurrTableItem.serail))
                        {
                            CGlobalCtrl.SnToAddressTable[CurrItemIndex].validflag = SN_VALID;
                            RaiseStateEvent("CHECK_AFFIRMWAIT:有效");
                            CurrCheckState = EnumCheckState.CHECK_NEXT;
                        }
                        else
                        {
                            CGlobalCtrl.SnToAddressTable[CurrItemIndex].validflag = SN_NOVALID;
                            // 更新映射信息表	
                            RaiseStateEvent("CHECK_AFFIRMWAIT:无效");
                            CurrCheckState = EnumCheckState.CHECK_NEXT;
                        }
                    }
                    if(CommFrame.CommState == EnumCommState.COMM_RECV_ERROR || CommFrame.CommState == EnumCommState.COMM_TIME_OUT)
                    {
                        if (RetryTimes < MAX_RETRY_TIMES)
                        {
                            RetryTimes++;
                            CurrCheckState = EnumCheckState.CHECK_AFFIRMSEND;
                        }
                        else
                        {
                            RetryTimes = 0;
                            CGlobalCtrl.SnToAddressTable[CurrItemIndex].validflag = SN_NOVALID;
                            // 更新映射信息表	
                            RaiseStateEvent("CHECK_AFFIRMWAIT:无效");
                            CurrCheckState = EnumCheckState.CHECK_NEXT;
                        }
                    }                   
                    break;
                case EnumCheckState.CHECK_NEXT:
                    CurrItemIndex++;
                    RetryTimes = 0;
                    if (GetNeedHandleVar(SN_NEEDAFFIRM))
                    {
                        CurrCheckState = EnumCheckState.CHECK_AFFIRMSEND;
                    }
                    else
                        CurrCheckState = EnumCheckState.CHECK_OVER;
                    break;
                case EnumCheckState.CHECK_OVER:  // 进入组网状态
                    RaiseStateEvent("CHECK_OVER:进入组网状态" );
                    if (CurrAutoNetWorkState == EnumAutoNetWorkState.CHECK_VAR_VALID)
                    {
                        CurrNetWorkState = EnumNetWorkState.NETWORK_START;
                        CurrAutoNetWorkState = EnumAutoNetWorkState.VARNOTE_NETWORK;
                    }
                    else
                    {
                        CurrAutoNetWorkState = EnumAutoNetWorkState.AUTONET_OVER;
                    }
                    break;
            }
        }

        private bool EqualArray(byte[] v, byte[] serail)
        {
            throw new NotImplementedException();
        }

        /*********************************************************************************
        *	模块编号：
        *	名    称：
        *	功    能：网内ID,缓存区ID初始化
        *	输入参数：
        *	返 回 值：
        *	修改日志： 
        *	[2017-4-19 8:45]? Ver. 1.00
        *	开始编写；
        *			完成； 
        *********************************************************************************/
        void networkinit()
        {
            Byte i;
            Byte[] sendbuff = new Byte[240];   // 通道缓存区，临时使用
            FrameHeadStr framehead = new FrameHeadStr();
            CommFrame = new CCommFrame();
            int sendlen=0;

            for (i = 0; i < CGlobalCtrl.MAX_VARNODE_NUM; i++)
            {
                CGlobalCtrl.SnToAddressTable[i].validflag = SN_NEEDAFFIRM;
            }

            // 通知节点进入组网状态
            framehead.address = 0;                                              // 广播发送组网命令
            framehead.funcode = CModbushost.FUN_REGISTRTU; 
            framehead.values = new Byte[10];
            framehead.values[0] = 1;     // 通知
            framehead.values[1] = 0;

            
            sendlen = mComm.Protocol.makeframe(CFunc.StructToBytes(framehead),Marshal.SizeOf(framehead), sendbuff, ref sendlen);
            CommFrame.FillContent(sendbuff, sendlen, CGlobalCtrl.CommNum);
            CommFrame.FrameRecvDelay = 3;
            mComm.FillFrame(CommFrame);

            RaiseStateEvent("通知节点进入组网状态");
            //mTimerNum = mComm.AddTimerTick(5);

            // 初始化结束，进入电容器确认
            CurrItemIndex = 0;
            CurrCheckState = EnumCheckState.CHECK_START;
            CurrAutoNetWorkState = EnumAutoNetWorkState.CHECK_VAR_VALID;
        }

        /*********************************************************************************
        *	模块编号：
        *	名    称：
        *	功    能：电容器节点档案更新
        *	输入参数：
        *	返 回 值：
        *	修改日志： 
        *	[2017-4-19 8:45]? Ver. 1.00
        *	开始编写；
        *			完成； 
        *********************************************************************************/
        void varnodeupdate()
        {
            RaiseStateEvent("更新映射表");
        }

        /*********************************************************************************
        *	模块编号：
        *	名    称：
        *	功    能：电容器节点自组网
        *	输入参数：
        *	返 回 值：
        *	修改日志： 
        *	[2017-4-19 8:45] Ver. 1.00
        *	开始编写；
        *			完成； 
        *********************************************************************************/
        public void autonetworking()
        {
            while(CurrAutoNetWorkState!= EnumAutoNetWorkState.AUTONET_OVER)
            { 
                switch (CurrAutoNetWorkState)
                {
                    case EnumAutoNetWorkState.NETWORK_INIT:
                        networkinit();
                        break;
                    case EnumAutoNetWorkState.CHECK_VAR_VALID:                        
                            checkvarvalid();
                        break;
                    case EnumAutoNetWorkState.VARNOTE_NETWORK:
                        networking();
                        break;
                    case EnumAutoNetWorkState.CHECK_VAR_VALID_AGAIN:
                        checkvarvalid();
                        break;
                    case EnumAutoNetWorkState.VARNOTE_DOCUPDATE:
                        // 进入电容节点档案更新
                        varnodeupdate();
                        CurrAutoNetWorkState = EnumAutoNetWorkState.AUTONET_OVER;
                        break;
                    case EnumAutoNetWorkState.AUTONET_OVER:
                        // 进入正常工作状态     
                        
                                           
                        break;
                }
            }


            RaiseStateEvent("Network 线程安全退出！");
        }
    }

    public class CProtocol
    {
        protected Dictionary<int, string> ErrDictionary;
        public CProtocol()
        {
            ErrDictionary = new Dictionary<int, string>();
            InitProtocol();
        }

        public virtual void InitProtocol()
        {

        }
        public virtual int parseframe(Byte[] input_buf, int input_len, out Byte[] output_buf, ref int handle_len)
        {
            output_buf = null;
            return 0;
        }
        public virtual int checkframe(Byte[] input_buf, int input_len,ref int off_pos,ref int frame_len)
        {
            return 0;
        }
        public virtual int makeframe(Byte[] input_buf, int input_len ,Byte[] output_buf,ref int output_len)
        {
            return 0;
        }

        public String GetErrorStr(int errno)
        {
            return ErrDictionary[errno];
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CModbushost : CProtocol
    {
        public const int FUN_READXQ = 0x01;        // 读单线圈
        public const int FUN_READLS = 0x02;        // 读离散
        public const int FUN_READBC = 0x03;        // 读保持寄存器
        public const int FUN_READSR = 0x04;        // 读输入寄存器
        public const int FUN_WRITEXQ = 0x05;       // 写单线圈
        public const int FUN_WRITEBC = 0x06;       // 写寄存器
        public const int FUN_WRITEMXQ = 0x0f;      // 写多线圈
        public const int FUN_WRITEMBC = 0x10;      // 写多寄存器
        public const int FUN_REGISTRTU = 0x65;     // RTU注册		 address func option value crc	 option 1:通知，2：上报
        public const int FUN_WRITEVARADDR = 0x66;  // 写入地址  address func sn address crc

        private const int MAX_MODBUS_LEN = 240;     // modbus 帧长度
        private const int MAX_DEVICE_ADDR = 247;    // modbus 有效地址

        public const int ERR_LENGTHWRONG = 0x49;          // 地址域错误
        public const int ERR_ADDRESSWRONG = 0x50;          // 地址域错误
        public const int ERR_CHECKSUM = 0x51;              // 帧效验错误
        public const int ERR_FUNCODE = 0x53;               // 功能码错误
        public const int SUC_REGISTRTU = 0x55;
        public const int SUC_ERRREPONSE = 0x66;            // 成功标志
        public const int SUC_FUNCODE = 0x88;               // 成功标志

        // 规约初始化
        public override void InitProtocol()
        {
            ErrDictionary.Clear();
            ErrDictionary.Add(1, "非法功能");
            ErrDictionary.Add(2, "非法数据地址");
            ErrDictionary.Add(3, "非法数据值");
            ErrDictionary.Add(4, "从站设备故障");
            ErrDictionary.Add(5, "正在处理");
            ErrDictionary.Add(6, "从属设备忙");
            ErrDictionary.Add(8, "存储奇偶性差错");
            ErrDictionary.Add(10, "不可用网关路径");
            ErrDictionary.Add(11, "网关目标设备响应失败");

        }
        /*********************************************************************************/
        /*函数名称: GetCRC16()                            
        *输入参数：  共  个参数；  
        *输出参数：  共  个参数；  
        *返回值：    
        *需储存的参数： 共  个参数；      
        *功能介绍：    
                (1)CRC16校验； 返回校验码；                
        *修改日志：  
        *[2005-11-28 16:40]    Ver. 1.00  
                开始编写；  
                完成；                                      
        */
        /*********************************************************************************/

        UInt16 GetCRC16(Byte[] puchMsg, Byte usDataLen, Byte usStartIndex)
        {
            /* CRC 高位字节值表 */
            Byte[] auchCRCHi = new Byte[256] {
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
                0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
                0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
                0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
                0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
                0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
                0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
                0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
                0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
                0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
                0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
                0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
                0x80, 0x41, 0x00, 0xC1, 0x81, 0x40
                };

            Byte[] auchCRCLo = new Byte[256]{
                0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06,
                0x07, 0xC7, 0x05, 0xC5, 0xC4, 0x04, 0xCC, 0x0C, 0x0D, 0xCD,
                0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
                0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A,
                0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC, 0x14, 0xD4,
                0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
                0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3,
                0xF2, 0x32, 0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4,
                0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
                0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29,
                0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF, 0x2D, 0xED,
                0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
                0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60,
                0x61, 0xA1, 0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67,
                0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
                0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68,
                0x78, 0xB8, 0xB9, 0x79, 0xBB, 0x7B, 0x7A, 0xBA, 0xBE, 0x7E,
                0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
                0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71,
                0x70, 0xB0, 0x50, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92,
                0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
                0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B,
                0x99, 0x59, 0x58, 0x98, 0x88, 0x48, 0x49, 0x89, 0x4B, 0x8B,
                0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
                0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42,
                0x43, 0x83, 0x41, 0x81, 0x80, 0x40
                };


            Byte uchCRCHi = 0xFF;      /* 高CRC字节初始化 */
            Byte uchCRCLo = 0xFF;      /* 低CRC 字节初始化 */
            UInt16 uIndex = 0;  /* CRC循环中的索引 */
            Byte usIndex = usStartIndex;

            while (usDataLen-- > 0)                 /* 传输消息缓冲区 */
            {
                uIndex = (UInt16)(uchCRCHi ^ puchMsg[usIndex++]); /* 计算CRC */
                uchCRCHi = (Byte)(uchCRCLo ^ auchCRCHi[uIndex]);
                uchCRCLo = auchCRCLo[uIndex];
            }
            if (CGlobalCtrl.CommLSB)
                return (UInt16)((UInt16)uchCRCLo << 8 | uchCRCHi);      // 更换
            else
                return (UInt16)((UInt16)uchCRCHi << 8 | uchCRCLo);      // 更换
        }


        /*********************************************************************************/
        //函数名称: checkframe(Byte* ptinput,Byte len)
        //输入参数：共2 个参数；
        //输出参数：共0 个参数；
        //返回值：? 检验结果，0表出错，>0 表示实际长度 
        //需储存的参数： 共? 个参数；
        //功能介绍：
        // (1)modbus规约帧解析，确保接受缓存和长度正确；
        //修改日志：
        //[2017-4-19 8:45]? Ver. 1.00
        //开始编写；
        //完成；
        /*********************************************************************************/
        int checkframe(Byte[] ptinput, int lenin, ref int pstart)
        {
            int checklen = 0;
            UInt16 crcrecv, crccalc;
            int datalen = 0;

            

            // 大多数情况
            crccalc = GetCRC16(ptinput, (Byte)(lenin - 2), 0);
            crcrecv = (UInt16)(ptinput[lenin - 2] * 256 | ptinput[lenin - 1]);
            pstart = 0;
            if (crccalc != crcrecv)
            {
                while (lenin >= 5)
                {
                    switch (ptinput[pstart + 1])
                    {
                        case FUN_READXQ:
                        case FUN_READLS:
                        case FUN_READBC:
                        case FUN_READSR:                    // 读数据，报文回带长度
                            datalen = ptinput[pstart + 2];
                            if (datalen > (lenin - 5))
                                break;
                            crccalc = GetCRC16(ptinput, (Byte)(datalen + 3), (Byte)pstart);   // subitemstr   triumph.sha want to modify;
                            crcrecv = (UInt16)(ptinput[datalen + 3 + pstart] * 256 | ptinput[datalen + 4 + pstart]);
                            if (crccalc == crcrecv)
                            {
                                checklen = (Byte)(datalen + 5 + pstart);

                                //pstart = ;
                                return checklen;
                            }
                            break;
                        case FUN_WRITEXQ:
                        case FUN_WRITEBC:
                        case FUN_WRITEMXQ:
                        case FUN_WRITEMBC:              // 写数据
                            if ((7 + pstart) >= ptinput.Length) return 0;
                            crccalc = GetCRC16(ptinput, 6, (Byte)pstart);
                            crcrecv = (UInt16)((ptinput[6 + pstart] * 256) | ptinput[7 + pstart]);
                            if (crccalc == crcrecv)
                            {
                                checklen = 8 + pstart;
                                return checklen;
                            }
                            break;
                        case FUN_REGISTRTU:
                            if ((10 + pstart) >= ptinput.Length) return 0;
                            crccalc = GetCRC16(ptinput, 9, (Byte)pstart);
                            crcrecv = (UInt16)((ptinput[9 + pstart] * 256) | ptinput[10 + pstart]);
                            if (crccalc == crcrecv)
                            {
                                checklen = 11 + pstart;
                                //*pstart = offset;
                                return checklen;
                            }
                            break;
                        //default:  // 错误代码
                        //    checklen = 0;
                        //    return checklen;
                    }
                    pstart++;
                    --lenin;
                }
            }
            else
                checklen = lenin;

            return checklen;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subitem"></param>
        /// <param name="affirmstr"></param>
        private void GetSubItem(ref SubItemInfo subitem, RegistInfo affirmstr)
        {
            subitem.value = (UInt16)(affirmstr.amounthi * 256 + affirmstr.amountlw);
            subitem.subitemid = (UInt16)(affirmstr.starthi * 256 + affirmstr.startlw);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ptinput"></param>
        /// <param name="leninput"></param>
        /// <param name="output_buf"></param>
        /// <param name="lenhandle"></param>
        /// <returns></returns>
        public override int parseframe(Byte[] ptinput, int leninput, out Byte[] output_buf, ref int lenhandle)
        {
            int startindex = 0, result = SUC_FUNCODE;
            UInt16 readnum = 0;
            output_buf = null;

            if (leninput < 5) return ERR_LENGTHWRONG;

            lenhandle = checkframe(ptinput, leninput, ref startindex);
            if (lenhandle == 0)
            {
                result = ERR_CHECKSUM;                                                         // 校验非法	
                return result;                                                                      // 帧的合法性
            }

            //memcopytostruct(ref framehead,typeof(FrameHeadStr), ptinput, length);

            if (ptinput[startindex] < 0 || ptinput[startindex] > MAX_DEVICE_ADDR)                              // 帧地址域
            {
                result = ERR_ADDRESSWRONG;     // 地址非法	                
                return result;
            }

            switch (ptinput[startindex + 1])                                                                                  // 帧功能码
            {
                case FUN_READXQ:                    // 读数据，回数据处理,是否可以考虑直接存储
                case FUN_READLS:
                case FUN_READBC:
                case FUN_READSR:
                    readnum = ptinput[startindex + 2];       // 数据长度	//memcpy((unsigned char *) & output_buf,(unsigned char*)(ptinput + 3),readnum);		
                    output_buf = new Byte[readnum];
                    Array.Copy(ptinput, startindex + 3, output_buf, 0, readnum);
                    // 直接根据存储保存数据
                    // SaveModbusDataInfo(ptinput + 3, readnum);
                    break;
                case FUN_WRITEXQ:                   // 写数据，回确认
                case FUN_WRITEBC:
                case FUN_WRITEMXQ:
                case FUN_WRITEMBC:
                    readnum = 4;
                    output_buf = new Byte[readnum];
                    Array.Copy(ptinput, startindex + 2, output_buf, 0, 4);
                    break;
                case FUN_REGISTRTU:
                    readnum = 7;
                    output_buf = new Byte[readnum];
                    Array.Copy(ptinput, startindex + 2, output_buf, 0, 7);
                    result = SUC_REGISTRTU;
                    break;
                case FUN_WRITEVARADDR:
                    readnum = 7;
                    output_buf = new Byte[readnum];
                    Array.Copy(ptinput, startindex + 2, output_buf, 0, 7);
                    // 设置注册地址确认，可以在此处理，应该不需要处理
                    break;
                default:
                    if ((ptinput[startindex + 1] & 0x80) > 0)           // 错误代码
                    {
                        readnum = 1;
                        output_buf = new Byte[readnum];
                        output_buf[0] = ptinput[startindex + 2];
                        result = SUC_ERRREPONSE;
                    }
                    break;
            }

            return result;
        }



        /*********************************************************************************
        *	模块编号：
        *	名    称：makeframe(FrameHeadStr* framehead,Byte* output_buf,Byte multwrite)
        *	功    能：modbus规约组帧函数
        *	输入参数：
                                framehead：组帧数据装载,字段意义参考FrameHeadStr结构说明
                                output_buf： 通道设备发送缓存区
                                multwrite：多线圈或寄存器发送标志
        *	返 回 值：长度，如为零表示错误，output_buf[0]内为错误代码
        *	修改日志： 
        *	[2017-4-19 8:45]? Ver. 1.00
        *	开始编写；
        *			完成； 
        *********************************************************************************/
        public override int makeframe(byte[] input_buf, int input_len, byte[] output_buf, ref int output_len)
        {
            
            int result = 0;
            UInt16 outputnum = 0;
            int writeBytenum = 0;
            UInt16 crccalc = 0;
            Byte btemp;
            Boolean multwrite = false;

            FrameHeadStr framehead;
            framehead.values = new byte[10];
            framehead = (FrameHeadStr)CFunc.BytesToStruct(input_buf, typeof(FrameHeadStr));
            

            outputnum = (UInt16)(framehead.affirmstr.amounthi * 256 + framehead.affirmstr.amountlw);
            // 地址
            if (framehead.address > MAX_DEVICE_ADDR)
            {
                output_buf[0] = ERR_CHECKSUM;         // 无效地址
                return result;
            }

            // 功能码
            switch (framehead.funcode)
            {
                case FUN_WRITEBC:
                    framehead.affirmstr.amounthi = framehead.values[0];
                    framehead.affirmstr.amountlw = framehead.values[1];
                    break;
                case FUN_WRITEMBC:
                    multwrite = true;
                    writeBytenum = (outputnum * 2);
                    break;
                case FUN_WRITEXQ:
                    try
                    {
                        if (framehead.values[0] > 0)
                        {
                            framehead.affirmstr.amountlw = 0x00;
                            framehead.affirmstr.amounthi = 0xff;
                        }
                        else
                        {
                            framehead.affirmstr.amountlw = 0x00;
                            framehead.affirmstr.amounthi = 0x00;
                        }
                    }
                    catch (Exception ex)
                    {
                        break;
                    }
                    break;
                case FUN_WRITEMXQ:
                    multwrite = true;
                    if ((outputnum % 8) > 0)
                        writeBytenum = outputnum / 8 + 1;
                    else
                        writeBytenum = outputnum / 8;
                    break;
                case FUN_WRITEVARADDR:          // address func 6 字节 SN 码 逻辑地址 crc  
                    output_buf[result++] = framehead.address;
                    output_buf[result++] = FUN_WRITEVARADDR;
                    Array.Copy(framehead.values, 0, output_buf, result, 7);
                    result += 7;
                    crccalc = GetCRC16(output_buf, (Byte)result, 0);
                    output_buf[result++] = (byte)((crccalc >> 8) & 0xff);
                    output_buf[result++] = (byte)(crccalc & 0xff);
                    //Array.Copy(CFunc.u16memcpy(crccalc), 0, output_buf, result, 2);
                    //result += 2;
                    return result;
                case FUN_REGISTRTU:             // address func option value crc	 option 1:通知，2：上报
                    output_buf[result++] = framehead.address;
                    output_buf[result++] = FUN_REGISTRTU;
                    Array.Copy(framehead.values, 0, output_buf, result, 2);
                    result += 2;
                    crccalc = GetCRC16(output_buf, (Byte)result, 0);
                    output_buf[result++] = (byte)((crccalc >> 8) & 0xff);
                    output_buf[result++] = (byte)(crccalc & 0xff);
                    //Array.Copy(CFunc.u16memcpy(crccalc), 0, output_buf, result, 2);
                    //result += 2;
                    return result;
            }

            result = 6;

            if (CGlobalCtrl.CommLSB)
            {
                btemp = framehead.affirmstr.starthi;
                framehead.affirmstr.starthi = framehead.affirmstr.startlw;
                framehead.affirmstr.startlw = btemp;
                btemp = framehead.affirmstr.amounthi;
                framehead.affirmstr.amounthi = framehead.affirmstr.amountlw;
                framehead.affirmstr.amountlw = btemp;
            }

            CFunc.structcopytomem(framehead, output_buf, 6);

            if (multwrite)    // 多线圈、多寄存器
            {
                output_buf[result++] = (Byte)writeBytenum;
                if (CGlobalCtrl.CommLSB)
                {
                    Array.Reverse(framehead.values);
                }

                Array.Copy(framehead.values, 0, output_buf, result, writeBytenum);
                result += writeBytenum;
            }
            crccalc = GetCRC16(output_buf, (Byte)result, 0);
            output_buf[result++] = (byte)((crccalc >> 8) & 0xff);
            output_buf[result++] = (byte)(crccalc  & 0xff);            

            return result;
        }
    }

}
