using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using DevComponents.DotNetBar.SuperGrid;

namespace 智能电容器
{
    #region 全局控制对象
    /// <summary>
    /// 全局控制对象
    /// </summary>
    public class CGlobalCtrl
    {
        public const int MAX_VARNODE_NUM = 10;

        public const int MAX_COMM_NUM = 5;

        public static CComm[] Comms = new CComm[MAX_COMM_NUM] { null, null, null, null, null };

        public static int ProtocolAddress;

        public static byte[] CommAddress = new byte[6];  // 断路器通信地址

        public static int CommNum;

        public static CModbushost Modbushost = new CModbushost();

        public static SnToAddressStr[] SnToAddressTable = new SnToAddressStr[MAX_VARNODE_NUM];

        public static bool CommLSB = false;             //

        public static string SoleCommonSN = null;       //

        public static string SolePartSN = null;         //

        public static frmmain MainForm {get;set;}

        public static frmAutoTest AuotTestFrom { get; set; }

        public static Int32 Authority { get; set; }     //

        public static Dictionary<string,bool[]> gFirstPutInFlag = new Dictionary<string, bool[]>();
    }
    #endregion
    #region 辅助结构信息
    /// <summary>
    /// 通道类型
    /// </summary>
    public enum ENUMCOMMTYPE {
        RS485_COMM,      // 电容器通道  

    };

    /// <summary>
    /// 通道状态
    /// </summary>
    public enum ENUMCOMMSTATE
    {
        COMM_IDLE,
        COMM_SEND,
        COMM_RECV,
    };

    public enum ENUMDEVSTATE
    {
        COMM_ERROR,
        COMM_OPEN,
        COMM_CLOSE
    };

    struct StrTimer {
        TimeSpan startTimeSpan;
        int DelayCount;
    };
    #endregion
    #region 帧结构
    public enum EnumCommState
    {
         COMM_WAIT,
         COMM_BE_SEND,         
         COMM_TIME_OUT,
         COMM_RECV_ERROR,
         COMM_RECV_SUCC
    }
    /// <summary>
    /// 规约帧处理类
    /// </summary>
    public class CCommFrame
    {
        /// <summary>
        /// 计时器
        /// </summary>
        private Timer mTimer = null;
        /// <summary>
        /// 属于通道
        /// </summary>
        private int mFrameBelongToComm;
        public int FrameBelongToComm { get { return mFrameBelongToComm; } set { mFrameBelongToComm = value; } }
        /// <summary>
        /// 帧接受延时
        /// </summary>
        private int mFrameRecvDelay;
        public int FrameRecvDelay { get { return mFrameRecvDelay; } set { mFrameRecvDelay = value; } }
        /// <summary>
        /// 帧所处状态
        /// </summary>
        EnumCommState mCommState;
        public EnumCommState CommState
        {
            get { return mCommState; }
            set { mCommState = value;
                if (mCommState == EnumCommState.COMM_BE_SEND)
                {
                    
                }
            }
        }
        /// <summary>
        /// 尝试次数
        /// </summary>
        /// 
        private int mRetryTimes;
        public int RetryTimes { get { return mRetryTimes; } set { mRetryTimes = value; } }
        /// <summary>
        /// 显示序号
        /// </summary>
        private int mShowIndex;
        public int ShowIndex { get { return mShowIndex; } set { mShowIndex = value; } }
        /// <summary>
        /// 帧序号
        /// </summary>
        private int mFrameIndex;
        public int FrameIndex { get { return mSendContent[1]; }  }               
        /// <summary>
        /// 发送内容
        /// </summary>
        private Byte[] mSendContent;
        /// <summary>
        /// 接受内容
        /// </summary>
        private Byte[] mRecvContent;
        /// <summary>
        /// 通信帧响应
        /// </summary>
        public int FrameResope { get; set; }
        public int Address { get { return mSendContent[0]; } }

        public GridRow ShowRow { get; internal set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public CCommFrame()
        {
            mFrameRecvDelay = 3;
            mTimer = new Timer(TimerTick, null, 0, 1000);            
        }

        ~CCommFrame()
        {
            if (mTimer != null)
                mTimer.Dispose();
        }
        /// <summary>
        /// 计数器
        /// </summary>
        /// <param name="obj"></param>
        private void TimerTick(object obj)
        {
            if (mCommState == EnumCommState.COMM_BE_SEND)
            { 
                mFrameRecvDelay--;
                if (mFrameRecvDelay <= 0)
                {
                    mCommState = EnumCommState.COMM_TIME_OUT;                                  
                }
            }
        }
        /// <summary>
        /// 填写帧内容
        /// </summary>
        /// <param name="content"></param>
        /// <param name="commno"></param>
        public void FillContent(byte[] content, int len, int commno)
        {
            mSendContent = new Byte[len];
            Array.Copy(content, 0, mSendContent, 0, len);
            FrameBelongToComm = commno;
            //FrameRecvDelay = 6;   // 通过外部赋值
        }
        /// <summary>
        /// 更新接受内容
        /// </summary>
        /// <param name="content"></param>
        /// <param name="len"></param>
        public void FillRecvContent(byte[] content, int len)
        {
            mRecvContent = new Byte[len];
            Array.Copy(content, 0, mRecvContent, 0, len);
            //mCommState = EnumCommState.COMM_RECV_SUCC;
        }
        
        /// <summary>
        /// 获取帧内容
        /// </summary>
        /// <returns></returns>
        public Byte[] GetSendContent()
        {
            return mSendContent;
        }
        /// <summary>
        /// 获取接受内容
        /// </summary>
        /// <returns></returns>
        public Byte[] GetRecvContent()
        {
            return mRecvContent;
        }
    }
    #endregion
    #region 缓存处理类
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CBufHandle
    {
        // 规约帧缓存 
        List<CCommFrame> m_CommFrames;
        /// <summary>
        /// 
        /// </summary>
        public CBufHandle()
        {
            m_CommFrames = new List<CCommFrame>();
        }
    }

    public class CCommQueue<T>
    {
        Queue<T> m_CommQueue;

        public CCommQueue()
        {
            m_CommQueue = new Queue<T>();
        }

        /// <summary>
        /// 清除队列元素
        /// </summary>
        public void clear()
        {
            m_CommQueue.Clear();
        }

        /// <summary>
        /// 队列元素数量
        /// </summary>
        /// <returns></returns>
        public int count()
        {
            return m_CommQueue.Count;
        }

        /// <summary>
        /// 写队列元素
        /// </summary>
        /// <param name="val"></param>
        public void push(T val)
        {
            // m_CommQueue.
            m_CommQueue.Enqueue(val);
        }

        /// <summary>
        /// 写队列元素
        /// </summary>
        /// <param name="val"></param>
        /// <param name="len"></param>
        public void push(T[] val, int len)
        {
            for (int i = 0; i < len; i++)
                m_CommQueue.Enqueue(val[i]);
        }

        /// <summary>
        /// 取队列元素
        /// </summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public T pop()
        {
            if (m_CommQueue.Count == 0)
                return default(T);

            return m_CommQueue.Dequeue();

        }

        public T[] pop(int len)
        {
            if (m_CommQueue.Count < len)
                return null;

            T[] result = new T[len];

            for (int i = 0; i < len; i++)
            {
                result[i] = m_CommQueue.Dequeue();
            }

            return result;
        }

        /// <summary>
        /// 读队列元素
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public T[] peek(int len)
        {
            if (m_CommQueue.Count < len)
                return null;

            T[] result = new T[len];

            for (int i = 0; i < len; i++)
            {
                result[i] = m_CommQueue.ElementAt<T>(i);
            }

            return result;
        }

    }


    #endregion
    #region 通信制造类
    /// <summary>
    /// 通信通道创建工厂类
    /// </summary>
    public class CCommFactory
    {
        /// <summary>
        /// 创建通信通道函数：根据通道类型创建通信设备并通过设备描述信息初始化通信设备
        /// </summary>
        /// <param name="CommType">通道类型</param>
        public static CComm CreateComm(string CommType, string CommDescStr)
        {
            //string CommDescStr = "";
            CComm Comm = null;
            CCommDev CommDev = null;
            CCommDsc CommDsc = null;
            CProtocol Protocol = null;

            if (CommType.Contains("RS485"))
            {
                CommDsc = new CRS232Dsc(CommDescStr);
                CommDev = new CRS232Dev(CommDsc);
                Protocol = new CModbushost();
                Comm = new CRS232Comm(CommDev, CommDsc, Protocol);
            }

            return Comm;
        }
    }
    #endregion
    #region 通信描述类
    /// <summary>
    /// CCommDsc 通信设备描述父类
    /// </summary>
    public class CCommDsc
    {
        protected string mCommDevDescStr;
        public CCommDsc(string strParam)
        {
            mCommDevDescStr = strParam;
        }

        /// <summary>
        /// 设备参数解析
        /// </summary>
        /// <param name="values"></param>
        public virtual void CommDevDesc(params string[] values)
        {

        }
    }

    /// <summary>
    /// CRS232Dsc 串口描述类，表示串口初始化或构造的信息
    /// </summary>
    public class CRS232Dsc : CCommDsc
    {
        public CRS232Dsc(string strParam) : base(strParam)
        {

        }

        public override void CommDevDesc(params string[] values)
        {
            base.CommDevDesc(values);
            string[] DescParams = null;
            if (mCommDevDescStr.Trim().Length != 0)
            {
                DescParams = mCommDevDescStr.Trim().Split(',');
            }

            if (DescParams != null)
            {
                if (DescParams.Length > 4)
                {
                    for (int i = 0; i < DescParams.Length; i++)
                        values[i] = DescParams[i];
                }
            }
        }
    }
    #endregion
    #region 通信设备类
    /// <summary>
    /// CCommDev 通信设备父类
    /// </summary>
    public class CCommDev
    {
        /// <summary>
        /// 通信设备描述
        /// </summary>
        protected CCommDsc mCommDsc;
        /// <summary>
        /// 通信设备状态
        /// </summary>
        protected ENUMDEVSTATE mDevState;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dsc">设备描述</param>
        public CCommDev(CCommDsc dsc)
        {
            mCommDsc = dsc;
            mDevState = ENUMDEVSTATE.COMM_CLOSE;
        }
        /// <summary>
        /// 设备关闭
        /// </summary>
        public virtual void close()
        {

        }
        /// <summary>
        /// 获取设备状态
        /// </summary>
        /// <returns>设备状态</returns>
        public ENUMDEVSTATE GetDevState()
        {
            return mDevState;
        }
        /// <summary>
        /// 初始化设备
        /// </summary>
        /// <returns></returns>
        public virtual bool initialize()
        {
            return false;
        }
        /// <summary>
        /// 发送函数
        /// </summary>
        /// <param name="sndbuf">发送缓存</param>
        /// <param name="len">发送长度</param>
        /// <returns></returns>
        public virtual int send(byte[] sndbuf, int offset, int len)
        {
            return 0;
        }

        /// <summary>
        /// 接收函数
        /// </summary>
        /// <param name="recvbuf">接收缓存</param>
        /// <param name="len">接收长度</param>
        /// <returns></returns>
        public virtual int recv(byte[] recvbuf, int offset, int len)
        {
            return 0;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class CRS232Dev : CCommDev
    {
        /// <summary>
        /// 接收标志
        /// </summary>
        private bool mRecvflag = false;

        /// <summary>
        /// 串口设备变量
        /// </summary>
        private SerialPort mSerialPort;

        /// <summary>
        ///  构造函数
        /// </summary>
        public CRS232Dev(CCommDsc dsc) : base(dsc)
        {
            mSerialPort = new SerialPort();
        }
        /// <summary>
        /// 接受数据事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            mRecvflag = true;
        }
        public override void close()
        {
            mSerialPort.Close();
        }

        /// <summary>
        /// 初始化函数
        /// </summary>
        /// <returns></returns>
        public override bool initialize()
        {
            string[] Desc = new string[6];
            mCommDsc.CommDevDesc(Desc);

            try
            {
                mSerialPort.Close();

                mSerialPort.PortName = Desc[0].Trim();
                mSerialPort.BaudRate = Convert.ToInt16(Desc[1].Trim());
                mSerialPort.Parity = (Parity)Enum.Parse(typeof(Parity), Desc[2].Trim());
                mSerialPort.DataBits = Convert.ToInt16(Desc[3].Trim());
                mSerialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), Desc[4].Trim());
                if(Desc[5] !=null)
                    mSerialPort.RtsEnable = Desc[5].Contains("RTS") ? true : false;

                mSerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                mSerialPort.WriteTimeout = 4000;
                mSerialPort.ReadTimeout = 5000;

                mSerialPort.WriteBufferSize = 1024;
                mSerialPort.ReadBufferSize = 1024;

                mSerialPort.Open();

                mDevState = ENUMDEVSTATE.COMM_OPEN;                
            }
            catch (Exception ex)
            {
                mDevState = ENUMDEVSTATE.COMM_ERROR;
                CDebugInfo.writeconsole("232串口初始化失败：", ex.Message);
            }

            return mSerialPort.IsOpen;
        }
        /// <summary>
        /// 发送数据函数
        /// </summary>
        /// <param name="sndbuf"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public override int send(byte[] sndbuf, int offset, int len)
        {
            try
            {
                mSerialPort.Write(sndbuf, offset, len);
            }
            catch (Exception ex)
            {
                CDebugInfo.writeconsole("232串口发送失败：", ex.Message);
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// 串口接受函数
        /// </summary>
        /// <param name="recvbuf"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public override int recv(byte[] recvbuf, int offset, int len)
        {
            int ReadLen = 0;

            if (!mRecvflag)
            {
                return 0;
            }

            mRecvflag = false;

            try
            {
                ReadLen = mSerialPort.Read(recvbuf, offset, len);
            }
            catch (Exception ex)
            {
                CDebugInfo.writeconsole("232串口接受失败：", ex.Message);
                return 0;
            }
            return ReadLen;
        }
    }
    #endregion
    #region 通道类父类
    /// <summary>
    /// 通道事件参数
    /// </summary>
    public class CommEventArgs: EventArgs
    {
        public CommEventArgs(CCommFrame frame)
        {
            //CommFrame = new CCommFrame();
            CommFrame = frame;
        }

        /// <summary>
        /// 帧结构信息
        /// </summary>
        public CCommFrame CommFrame { get; private set; }
    }
    /// <summary>
    /// 状态事件参数
    /// </summary>
    public class StateEventArgs : EventArgs
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="commnum"></param>
        public StateEventArgs(string msg,int commnum)
        {
            Message = msg;
            CommNum = commnum;
        }
        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 通道编号
        /// </summary>
        public int CommNum { get; set; }
    }
    /// <summary>
    /// CComm 通道类父类
    /// </summary>
    public class CComm
    {
        /// <summary>
        /// 通道规约
        /// </summary>
        public CProtocol Protocol { get; set; }
        /// <summary>
        /// 通道标志
        /// </summary>
        protected string mCommMark;

        /// <summary>
        /// 通信设备
        /// </summary>
        protected CCommDev mCommDev;

        /// <summary>
        /// 通信状态
        /// </summary>
        protected ENUMCOMMSTATE mCommState;

        /// <summary>
        /// 通道工作线程
        /// </summary>
        private Thread mWorkThread;
        /// <summary>
        /// 工作标志
        /// </summary>
        protected bool mWorkFlag = false;

        /// <summary>
        /// 创建一个委托，返回类型为void，两个参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void CommEventHandle(object sender, CommEventArgs e);
        public delegate void CommStateHandle(object sender, StateEventArgs e);
        
        /// 通信通道事件     
        public event CommEventHandle CommEvent;
        public event CommStateHandle StateEvent;

        /// <summary>
        /// 收发缓存区,对于收发缓存可以考虑统一算法处理
        /// </summary>
        protected CCommQueue<CCommFrame> mSendQueue;      // 发送队列
        /// <summary>
        /// 接收缓存区
        /// </summary>
        protected CCommQueue<byte> mRecvQueue;            // 接受队列
        /// <summary>
        /// 通道编号
        /// </summary>
        public int CommNum { get; set; }        

        /// <summary>
        /// 构造函数
        /// </summary>
        public CComm(CCommDev dev, CCommDsc dsc,CProtocol prtc)
        {            
            // 通信状态
            mCommState = ENUMCOMMSTATE.COMM_IDLE;
            // 通信规约
            Protocol = prtc;
            // 缓存区
            mSendQueue = new CCommQueue<CCommFrame>();
            // 接收缓存
            mRecvQueue = new CCommQueue<byte>();
            // 通信设备
            mCommDev = dev;
            // 工作线程
            mWorkThread = new Thread(CommProcess);                      
            // 工作标志
            mWorkFlag = true;
            // 启动工作线程
            mWorkThread.Start();
        }
        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="frame"></param>
        protected virtual void RaiseCommEvent(CCommFrame frame)
        {
            if (CommEvent != null)
            {
                CommEvent(this, new 智能电容器.CommEventArgs(frame));
            }
        }
        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void RaiseStateEvent(string msg)
        {
            if (StateEvent != null)
            {
                msg = "通道" + (CommNum+1).ToString() + msg;
                StateEvent(this,new 智能电容器.StateEventArgs( msg, CommNum));
            }
        }        

        /// <summary>
        /// 通道处理函数
        /// </summary>
        /// <param name="Temp">临时参数</param>
        public virtual  void CommProcess(Object Temp)
        {
            return;              
        }

        /// <summary>
        /// 通道接受函数
        /// </summary>
        /// <returns></returns>
        protected virtual int CommRecv()
        {
            return 0;
        }
        /// <summary>
        /// 通道发送函数
        /// </summary>
        /// <returns></returns>
        protected virtual int CommSend()
        {
            return 0;
        }
        /// <summary>
        /// 填充通信帧
        /// </summary>
        /// <param name="CommFrame"></param>
        internal void FillFrame(CCommFrame CommFrame)
        {
            lock (mSendQueue)
            {
                mSendQueue.push(CommFrame);
            }
        }
        /// <summary>
        /// 获取需处理通信帧
        /// </summary>
        /// <returns></returns>
        protected virtual CCommFrame PopFrame()
        {
            CCommFrame GetFrame;
            lock (mSendQueue)
            {
                GetFrame = mSendQueue.pop();
            }

            return GetFrame;
        }
        /// <summary>
        /// 获取处理帧，但不弹出帧
        /// </summary>
        /// <returns></returns>
        public virtual CCommFrame GetFrame()
        {
            CCommFrame GetFrame;
            lock (mSendQueue)
            {
                GetFrame = mSendQueue.peek(1)[0];
            }

            return GetFrame;
        }
        /// <summary>
        /// 关闭设备函数
        /// </summary>
        internal void Close()
        {
            mCommDev.close();
            mWorkFlag = false;
        }
        /// <summary>
        /// 获取当前帧信息
        /// </summary>
        /// <returns></returns>
        public virtual CCommFrame GetCurFrame()
        {
            return null;
        }
    }

    /// <summary>
    /// CRS232 RS232通道,不支持异步接受处理
    /// </summary>
    public class CRS232Comm : CComm
    {
        private CCommFrame mCurCommFrame;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CRS232Comm(CCommDev dev, CCommDsc dsc,CProtocol prtc):base(dev,dsc, prtc)
        {
            
        }
        /// <summary>
        /// 获取通信帧
        /// </summary>
        /// <returns></returns>
        public override CCommFrame GetCurFrame()
        {
            return mCurCommFrame;
        }

        /// <summary>
        /// 通道处理函数
        /// </summary>
        /// <param name="Temp">临时参数</param>
        public override void CommProcess(Object Temp)
        {            
            while (mWorkFlag)
            {
                // 初始化设备
                if (mCommDev.GetDevState() != ENUMDEVSTATE.COMM_OPEN )
                {
                    if(!mCommDev.initialize())
                    {
                        RaiseStateEvent("初始化失败!");
                        Thread.Sleep(10000);
                        continue;
                    }
                    RaiseStateEvent("初始化成功!");
                }

                // 通道状态维护
                if (mCommState == ENUMCOMMSTATE.COMM_IDLE || mCommState == ENUMCOMMSTATE.COMM_RECV)
                {
                    // 接受处理
                    CommRecv();

                    if (mCommState == ENUMCOMMSTATE.COMM_RECV)
                    {
                        if (mCurCommFrame.CommState == EnumCommState.COMM_RECV_SUCC || mCurCommFrame.CommState == EnumCommState.COMM_RECV_ERROR || 
                            mCurCommFrame.CommState == EnumCommState.COMM_TIME_OUT || mCurCommFrame.CommState == EnumCommState.COMM_WAIT)
                        {
                            // if (mCurCommFrame.FrameRecvDelay > 0) mCurCommFrame.FrameRecvDelay = 0;
                            mCommState = ENUMCOMMSTATE.COMM_IDLE;
                        }
                    }
                }

                // 规约处理
                ProtocolProcess();

                // 通道状态维护
                if (mCommState == ENUMCOMMSTATE.COMM_IDLE)
                {
                    if (mSendQueue.count() > 0)
                        mCommState = ENUMCOMMSTATE.COMM_SEND;
                }
                if (mCommState == ENUMCOMMSTATE.COMM_SEND)
                {
                    CommSend();
                    mCommState = ENUMCOMMSTATE.COMM_RECV;                   
                }               

                Thread.Sleep(10);
            }

            CDebugInfo.writeconsole("线程退出：", "RS232工作线程结束");
        }

        /// <summary>
        /// 规约解析处理函数
        /// </summary>
        private void ProtocolProcess()
        {
            Byte[] gettemp = new Byte[256];
            Byte[] rsttemp;
            int datalen = 0;
            int handlelen = 0;
            int parseResult = 0;
            // 获取数据
            lock (mRecvQueue)
            {
                if (mRecvQueue.count() > 0)
                {
                    if (mRecvQueue.count() > 512) mRecvQueue.pop(255);
                    datalen = mRecvQueue.count() > 255 ? 255 : mRecvQueue.count();

                    gettemp = mRecvQueue.peek(datalen);                   
                }
            }

            if (datalen > 0)
            {
                parseResult = Protocol.parseframe(gettemp, (Byte)datalen, out rsttemp,ref handlelen);
                
                if (handlelen > 0 && parseResult>= CModbushost.SUC_REGISTRTU && rsttemp!=null)
                {
                    //CurCommFrame = GetFrame();
                    mCurCommFrame.FrameResope = parseResult;
                    if (parseResult != CModbushost.SUC_REGISTRTU)
                    {
                        if (parseResult == CModbushost.SUC_ERRREPONSE)
                        {
                            mCurCommFrame.CommState = EnumCommState.COMM_RECV_ERROR;
                        }
                        else
                        {
                            mCurCommFrame.CommState = EnumCommState.COMM_RECV_SUCC;
                        }

                        mCommState = ENUMCOMMSTATE.COMM_IDLE;
                    }

                    mCurCommFrame.FillRecvContent(rsttemp, rsttemp.Length);                    
                    RaiseCommEvent(mCurCommFrame);
                    RaiseStateEvent("解析成功：" + BitConverter.ToString(rsttemp, 0, rsttemp.Length));
                }
            }

            lock (mRecvQueue)
            {
                if (handlelen > 0)
                {
                    mRecvQueue.pop(handlelen);                    
                }
            }
        }

        /// <summary>
        /// 通道接受函数
        /// </summary>
        /// <returns></returns>
        protected override int CommRecv()
        {
            int RecvLen = 0;
            byte[] RecvBuf = new byte[256];

            // 数据接受
            RecvLen = mCommDev.recv(RecvBuf, 0, 256);
            // 接受长度
            if (RecvLen >0)
            {   
                mRecvQueue.push(RecvBuf, RecvLen);      // 在此处解析还是在解析，在规约处理过程中统一处理
                RaiseStateEvent("接受数据：" + BitConverter.ToString(RecvBuf, 0,RecvLen));
            }
            return RecvLen;
        }

        /// <summary>
        /// 通道发送函数
        /// </summary>
        /// <returns></returns>
        protected override int CommSend()
        {
            int result = 0;

            mCurCommFrame = PopFrame();
            if (mCurCommFrame != null)
            {
                mCurCommFrame.CommState = EnumCommState.COMM_BE_SEND;                
                result = mCurCommFrame.GetSendContent().Length;
                mCommDev.send(mCurCommFrame.GetSendContent(), 0, result);                
            }
            RaiseStateEvent("发送数据：" + BitConverter.ToString(mCurCommFrame.GetSendContent(), 0,result));
            return result;
        }

    }

    #endregion
}
