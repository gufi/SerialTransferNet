using System;
using System.Collections.Generic;
using System.Linq;

namespace SerialTransfer.net
{
    public static class CRC8
    {
        private static byte[] table;
        const byte poly = 0x9B;

        public static byte CalcCRC(this IEnumerable<byte> bytes)
        {
            byte crc = 0;
            if (bytes == null || !bytes.Any()) return crc;
            foreach (byte b in bytes)
            {
                crc = table[crc ^ b];
            }
            return crc;
        }

        static CRC8()
        {
            var tableLen_ = (int)Math.Pow(2, 8);
            table = new byte[tableLen_];
            for (int i = 0; i < tableLen_; ++i)
            {
                int curr = i;

                for (int j = 0; j < 8; ++j)
                {
                    if ((curr & 0x80) != 0)
                        curr = (curr << 1) ^ (int)poly;
                    else
                        curr <<= 1;
                }

                table[i] = (byte)curr;
            }
        }
    }
}