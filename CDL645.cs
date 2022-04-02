using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace 智能电容器
{
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    public struct DL645HeadStr
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] address;// = new byte[6];
        public byte func;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] didt;
        public byte msglen;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public byte[] msgload;
    }
    class CDL645 : CProtocol
    {
        public override int parseframe(byte[] input_buf, int input_len, out byte[] output_buf, ref int handle_len)
        {
            int ret = 0;
            //int i = 0;
            int off_pos = 0;
            int data_pos = 0;
            int recv_frame_len = 0;

            byte func = 0;
            uint didt = 0;

            output_buf = null;

            if (input_len < 12)
                return ret;

            if (checkframe(input_buf, input_len, ref off_pos, ref recv_frame_len) == 0)
                return ret;

            ret = 0x88;  // 成功标志，优化为统一的标识编码

            handle_len = recv_frame_len + off_pos;

            func = input_buf[off_pos + 8];
            switch (func)
            {
                case 0xD1:
                    data_pos = off_pos + 10;
                    output_buf = new byte[1];
                    Array.Copy(input_buf, data_pos, output_buf, 0, 1);
                    break;
                case 0x91:
                    didt = GetDiDt(input_buf, off_pos + 10);// (uint)((input_buf[off_pos + 10]-0x33) + input_buf[off_pos + 11] << 8 + input_buf[off_pos + 12] << 16 + input_buf[off_pos + 13]<<24);
                    data_pos = off_pos + 14;
                    switch(didt)
                    {
                        case 0x02010100:
                        case 0x02010200:
                        case 0x02010300:
                            output_buf = new byte[3];
                            Array.Copy(input_buf, data_pos, output_buf, 0, 3);
                            Sub0x33(output_buf);
                            break;
                        case 0x0201FF00:
                            output_buf = new byte[9];
                            Array.Copy(input_buf, data_pos, output_buf, 0, 9);
                            Sub0x33(output_buf);
                            break;
                    }
                    break;



                default:
                    break;
            }
            
            return ret;
        }

        private void Sub0x33(byte[] output_buf)
        {
            for(int i=0;i<output_buf.Length;i++)
                output_buf[i] -= 0x33;
        }

        private uint GetDiDt(byte[] input_buf, int offset)
        {
            uint ret = 0;

            ret = (uint)((input_buf[offset] - 0x33) + ((input_buf[offset + 1] - 0x33) << 8) + ((input_buf[offset+2] - 0x33) << 16) + ((input_buf[offset+3] - 0x33) << 24));

            return ret;
        }

        public override int checkframe(byte[] input_buf, int input_len, ref int off_pos, ref int frame_len)
        {
            int ret = 0;
            off_pos = 0;

            while (input_len >= 12)
            {
                //判断645规约的帧头、帧尾、校验和
                if (input_buf[off_pos] == 0x68)
                {
                    if (input_buf[off_pos+7] == 0x68)
                    {
                        frame_len = input_buf[off_pos + 9];

                        if ((off_pos + 11 + frame_len) >= input_len)
                            return ret;
                        if (input_buf[off_pos+11+frame_len] == 0x16)
                        {
                            if (CFunc.CSCheck(input_buf,off_pos, frame_len+10) == input_buf[off_pos + 10 + frame_len])
                            {
                                ret = frame_len + 12;
                                if (ret > input_len)
                                {
                                    return 0;
                                }
                                else
                                {
                                    frame_len = ret;
                                    return ret;
                                }
                            }
                        }
                    }
                }
                off_pos++;
                input_len--;
            }

            return ret;
        }

        /// <summary>
        /// 645组帧函数，使用统一接口函数
        /// </summary>
        /// <param name="input_buf">组织内容，包括设备地址、数据项、参数等</param>
        /// <param name="input_len">组织内容长度</param>
        /// <param name="output_buf">组织完成后，数据缓存区</param>
        /// <param name="output_len">组织完成后，数据长度</param>
        /// <returns>正确返回数据长度；错误返回错误代码</returns>
        public override int makeframe(byte[] input_buf, int input_len, byte[] output_buf, ref int output_len)
        {
            int i = 0;
            int len = 0;
            DL645HeadStr frameHead;
            frameHead.address = new byte[6];
            frameHead.didt = new byte[4];
            frameHead.msgload = new byte[100];

            frameHead = (DL645HeadStr)CFunc.BytesToStruct(input_buf, typeof(DL645HeadStr));

            for (i = 0; i < 4; i++)
                output_buf[len++] = 0xFE;

            output_buf[len++] = 0x68;
            Array.Copy(frameHead.address, 0, output_buf, len, 6);
            len += 6;
            output_buf[len++] = 0x68;
            output_buf[len++] = frameHead.func;
            output_buf[len++] = (byte)(frameHead.msglen+4);
            for (i = 0; i < 4; i++)
                output_buf[len++] = (byte)(frameHead.didt[i]+0x33);
            if (frameHead.msglen > 0)
            {
                for(i=0;i< frameHead.msglen;i++)
                {
                    frameHead.msgload[i] += 0x33;
                }

                Array.Copy(frameHead.msgload, 0, output_buf, len, frameHead.msglen);
                len += frameHead.msglen;
            }
            output_buf[len++] = CFunc.CSCheck(output_buf, 4, len-4);
            output_buf[len++] = 0x16;
            return len;
        }

        internal static string DataTrans(byte[] source,string datatypestr, int datalen, int rate)
        {
            string showstr = "";

            switch (datatypestr)
            {
                case "BIN":
                    
                    break;
                case "BCD":
                    showstr = BCDToString(source,datalen, rate);//BitConverter.ToString(temp).Replace("-", null);
                    break;
                case "BIT":
                    //showstr = (temp[0] & (Byte)Math.Pow(2, i - checkstart)) > 0 ? "1" : "0";
                    break;
            }

            return showstr;

        }

        private static string BCDToString(byte[] source, int datalen, int rate)
        {
            string retstr = "";
            string[] bytestr = BitConverter.ToString(source).Split('-');
            int pos_decimal = 0;

            if (bytestr.Length == datalen)
            {
                if (rate == 10)
                    pos_decimal = 1;
                else if (rate == 100)
                    pos_decimal = 2;
                else if (rate == 1000)
                    pos_decimal = 3;
                else if (rate == 10000)
                    pos_decimal = 4;


                for (int i = datalen - 1; i >= 0; i--)
                {
                    retstr = retstr + bytestr[i];
                }

                retstr = retstr.Insert(retstr.Length- pos_decimal, ".");
            }
            return retstr;
        }

        internal static byte DataFill(byte[] msgload, string datatypestr, string datastr, ushort subdatalen)
        {
            throw new NotImplementedException();
        }
    }
}
