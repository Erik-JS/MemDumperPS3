using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemDumperPS3
{
    class ByteSwapper
    {
        public static byte[] ByteSwap(byte[] data, int swapSize)
        {
            MemoryStream ms = new MemoryStream();
            var subarray = new byte[swapSize];
            for (int i = 0; i < data.Length; i += swapSize)
            {
                Array.Copy(data, i, subarray, 0, swapSize);
                Array.Reverse(subarray);
                ms.Write(subarray, 0, swapSize);
            }
            return ms.ToArray();
        }
    }
}
