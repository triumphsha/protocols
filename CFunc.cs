using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

namespace 智能电容器
{
    public class CFunc
    {
        public static byte[] u16memcpy(UInt16 val)
        {
            byte[] result = new byte[2];

            MemoryStream memx = new MemoryStream(2);
            BinaryWriter binwritex = new BinaryWriter(memx);
            BinaryReader binreadx = new BinaryReader(memx);

            binwritex.Write(val);
            memx.Position = 0;
            result = binreadx.ReadBytes(2);

            return result;
        }

        public static void bytestobin(Byte[] val,ref object result)
        {
            int datalen = val.Length;
            //object result;
            
            switch(datalen)
            {
                case 2:
                    //result = ;
                    if (CGlobalCtrl.CommLSB)
                        result = BitConverter.ToInt16(val, 0);
                    else
                        result = System.Net.IPAddress.HostToNetworkOrder(BitConverter.ToInt16(val, 0));
                    break;
                case 4:
                    if (CGlobalCtrl.CommLSB)
                        result = BitConverter.ToInt32(val, 0);
                    else               
                        result = System.Net.IPAddress.HostToNetworkOrder(BitConverter.ToInt32(val, 0));
                    break;
                case 6:                   
                case 8:
                    byte[] temp = new Byte[8];
                    if (datalen == 6)
                        val.CopyTo(temp, 2);
                    else
                        val.CopyTo(temp, 0);
                    if (CGlobalCtrl.CommLSB)
                        result = BitConverter.ToInt64(val, 0);
                    else
                        result = System.Net.IPAddress.HostToNetworkOrder(BitConverter.ToInt64(temp, 0));
                    break;
                default:
                    if (CGlobalCtrl.CommLSB)
                        Array.Reverse(val);
                    result =   BytesToStr(val, datalen, false);

                    break;
            }
            //result = System.Net.IPAddress.HostToNetworkOrder(result);
        }

        internal static bool DataTrans(byte[] input_buf, int data_pos, byte type, byte[] output_buf)
        {
            throw new NotImplementedException();
        }

        internal static byte CSCheck(byte[] sendbuf, int offset, int len)
        {
            byte checksum = 0;

            for(int i=0;i<len;i++)
            {
                checksum += sendbuf[offset+i];
            }

            return checksum;
        }

        internal static string CreateSNFromXml(string v)
        {
            string result = "";
            if (Convert.ToByte(v, 16) != 0x3f && Convert.ToByte(v, 16) != 0x8f)
            {
                return "/";
            }

            result = v + CGlobalCtrl.SoleCommonSN.Substring(0, 6) + (Convert.ToInt32(CGlobalCtrl.SoleCommonSN.Substring(6, 4)) + 1).ToString("D4");
            return result;
        }

        internal static byte[] StrToBytes(string datastr, ushort datalen,bool BCDflag)
        {
            Byte temp;
            byte[] result = new byte[datalen];
            int index = 0;

            //if(datalen)
            try
            {
                if (!BCDflag)
                    datastr = Convert.ToInt64(datastr).ToString("X");


                while (datastr.Length < datalen * 2)
                {
                    datastr = "0" + datastr;
                }

                for (int i = 0; i < datalen * 2; i = i + 2)
                {
                    temp = Convert.ToByte(datastr.Substring(i, 2), 16);
                    result[index++] = temp;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            return result;
        }

        internal static string BytesToStr(byte[] val, int datalen, bool BCDflag)
        {
            //Byte  temp;
            string result = "";            

            for (int i = 0; i < datalen ; i++)
            {
                if(BCDflag)
                    result += BCD2HEX(val[i]).ToString("D2");
                else
                    result += val[i].ToString("D2");
            }       

            return result;
        }

        internal static string BytesToHexStr(byte[] val, int datalen)
        {
            //Byte  temp;
            string result = "";

            for (int i = 0; i < datalen; i++)
            {           
                result += val[i].ToString("X2");
            }

            return result;
        }

        public static int bitstobytes(byte[] src, byte[] tgr)
        {
            Byte tempval = 0;
            int index = 0;
            int regnum = src.Length;
            for (int i = 0; i < regnum; i++)
            {                
                if (i % 8 == 0)
                {
                    tempval = 0;
                    index++;                    
                }
                tempval += (Byte)(src[i] > 0 ? Math.Pow(2, i) : 0);
                tgr[index-1] = tempval;
            }
            
            return (index);
        }

        //internal static byte[] StrToBins(string datastr, ushort datalen)
        //{
        //    Byte temp;
        //    byte[] result = new byte[datalen];
        //    int index = 0;

        //    while (datastr.Length < datalen * 2)
        //    {
        //        datastr = "0" + datastr;
        //    }

        //    for (int i = 0; i < datalen * 2; i = i + 2)
        //    {
        //        temp = Convert.ToByte(datastr.Substring(i, 2), 10);
        //        result[index++] = temp;
        //    }


        //    return result;
        //}

        /*-********************************************************************
*模块编号：
*名    称：
*功    能：
*输入参数：
*返 回 值：
********************************************************************-*/

        internal static Byte[] BCD2HEXs(Byte[] bcd_data)
        {
            Byte[] temp = new Byte[bcd_data.Length];
            for (int i = 0; i < bcd_data.Length; i++)
            {
                temp[i] = BCD2HEX(bcd_data[i]);
            }
            return temp;
        }
        internal static Byte BCD2HEX(Byte bcd_data)
        {
            Byte temp;
            if ((bcd_data / 16) > 9 || (bcd_data % 16) > 9) throw new Exception("非法数据");
            temp = (Byte)(bcd_data / 16 * 10 + bcd_data % 16);
            return temp;
        }

        /*-********************************************************************
*模块编号：
*名    称：
*功    能：
*输入参数：
*返 回 值：
********************************************************************-*/
        internal static Byte[] HEX2BCDs(Byte[] hex_data)
        {
            Byte[] temp = new Byte[hex_data.Length];
            for (int i = 0; i < hex_data.Length; i++)
            {
                temp[i] = HEX2BCD(hex_data[i]);
            }
            return temp;
        }
        internal static Byte HEX2BCD(Byte hex_data)
        {
            Byte temp;
            if ((hex_data / 10) > 9 || (hex_data % 10) > 9) throw new Exception("非法数据") ;
            temp = (Byte)(hex_data / 10 * 16 + hex_data % 10);
            return temp;
        }

        //public static void typememcpy(object val,Byte[] get)
        //{
        //    byte[] result = new byte[2];

        //    //typeof(val);

        //    MemoryStream memx = new MemoryStream(2);
        //    BinaryWriter binwritex = new BinaryWriter(memx);
        //    BinaryReader binreadx = new BinaryReader(memx);

        //    binwritex.Write(val);
        //    memx.Position = 0;
        //    result = binreadx.ReadBytes(2);

        //    return result;
        //}
        //// <summary>
        /// 结构体转byte数组
        /// </summary>
        /// <param name="structObj">要转换的结构体</param>
        /// <returns>转换后的byte数组</returns>
        public static byte[] StructToBytes(object structObj)
        {
            //得到结构体的大小
            int size = Marshal.SizeOf(structObj);
            //创建byte数组
            byte[] bytes = new byte[size];
            //分配结构体大小的内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将结构体拷到分配好的内存空间
            Marshal.StructureToPtr(structObj, structPtr, false);
            //从内存空间拷到byte数组
            Marshal.Copy(structPtr, bytes, 0, size);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            //返回byte数组
            return bytes;
        }


        /// <summary>
        /// byte数组转结构体
        /// </summary>
        /// <param name="bytes">byte数组</param>
        /// <param name="type">结构体类型</param>
        /// <returns>转换后的结构体</returns>
        public static object BytesToStruct(byte[] bytes, Type type)
        {
            //得到结构体的大小
            int size = Marshal.SizeOf(type);
            //byte数组长度小于结构体的大小
            if (size > bytes.Length)
            {
                //返回空
                return null;
            }
            //分配结构体大小的内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将byte数组拷到分配好的内存空间
            Marshal.Copy(bytes, 0, structPtr, size);
            //将内存空间转换为目标结构体
            object obj = Marshal.PtrToStructure(structPtr, type);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            //返回结构体
            return obj;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="source"></param>
        internal static void memcopytostruct(ref object obj, Type t, Byte[] source, int srclen)
        {
            int stcLen = Marshal.SizeOf(obj);
            IntPtr ptrTemp = IntPtr.Zero;

            ptrTemp = Marshal.AllocHGlobal(Marshal.SizeOf(obj));
            Marshal.Copy(source, 0, ptrTemp, srclen);
            obj = Marshal.PtrToStructure(ptrTemp, t);     // 有可能len比结构长度小
            Marshal.FreeHGlobal(ptrTemp);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="target"></param>
        internal static void structcopytomem(object obj, Byte[] target, int srclen)
        {
            int len = Marshal.SizeOf(obj);
            IntPtr ptrTemp = IntPtr.Zero;

            ptrTemp = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(obj, ptrTemp, false);
            Marshal.Copy(ptrTemp, target, 0, srclen);
            Marshal.FreeHGlobal(ptrTemp);
        }

        internal static bool EqualArray(byte[] v, byte[] serail)
        {
            int i = 0;
            if (v.Length != serail.Length) return false;

            for (i = 0; i < v.Length; i++)
            {
                if (!v[i].Equals(serail[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
