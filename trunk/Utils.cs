using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
namespace ExporeMini_Test
{
    public class Utils
    {
        //将Byte转换为结构体类型
        public static byte[] StructToBytes<T>(T obj)
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr bufferPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(obj, bufferPtr, false);
                byte[] bytes = new byte[size];
                Marshal.Copy(bufferPtr, bytes, 0, size);

                return bytes;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in StructToBytes ! " + ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }

        //字节流转换成结构体

        public static T BytesToStruct<T>(byte[] bytes, int startIndex = 0)
        {
            if (bytes == null) return default(T);
            if (bytes.Length <= 0) return default(T);
            int objLength = Marshal.SizeOf(typeof(T));
            IntPtr bufferPtr = Marshal.AllocHGlobal(objLength);
            try//struct_bytes转换
            {
                Marshal.Copy(bytes, startIndex, bufferPtr, objLength);
                return (T)Marshal.PtrToStructure(bufferPtr, typeof(T));
            }
            catch (Exception ex)
            {
                throw new Exception("Error in BytesToStruct ! " + ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }

        public static byte Xiro_buildCheckBit(byte[] data, int len)
        {
            int checkBit = 0;
            if (data.Length <= 0)
            {
                return 0;
            }

            //check bit = 0-N & 0xff
            for (int i = 0; i < len; i++)
            {
                checkBit += data[i];
            }
          
            
            checkBit = checkBit & 0xff;
            //MessageBox.Show("Xiro_buildCheckBit checkBit" + (byte)checkBit);
            return (byte)checkBit;
        }

        public static bool Xiro_CheckBitEn(byte[] data, int len)
        {
            byte checkBit = data[len - 1];
            byte calcCheckBit = 0;

            calcCheckBit = Xiro_buildCheckBit(data, len - 1);
           // MessageBox.Show("Xiro_buildCheckBit checkBit" + (byte)checkBit);
           // MessageBox.Show("Xiro_buildCheckBit calcCheckBit" + (byte)calcCheckBit);
            if (calcCheckBit == 0)
            {
                return false;
            }

            if (calcCheckBit == checkBit)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
