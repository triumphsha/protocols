using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 智能电容器
{
    public class CDebugInfo
    {
        public CDebugInfo()
        {

        }

        /// <summary>
        /// 在控制输出中输出调试信息
        /// </summary>
        /// <param name="arg"></param>
        public static void writeconsole(params object[] arg)
        {
            Console.Write("{0}:{1}\r\n", arg);
        }

        /// <summary>
        /// 写入调试信息入日志文件
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="msg"></param>
        /// <param name="type"></param>
        public static void writelogfile(string filepath,string msg,int type)
        {
            // 
        }
        /// <summary>
        /// 写入调试信息入程序控件
        /// </summary>
        /// <param name="inputctrl"></param>
        /// <param name="msg"></param>
        /// <param name="type"></param>
        public static string  writelogctrl(string msg,int type)
        {
            //string result;

            return string.Format("{0:d} {1:c} {2:s}\r\n", DateTime.Now.Date, DateTime.Now.TimeOfDay, msg);
            //inputctrl

        }
    }
}
