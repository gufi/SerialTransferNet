using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SerialTransfer.net
{
    public class Packet
    {
        public Packet()
        {
            Buffer = new List<byte>(256);
        }
        
        public List<byte> Buffer { get; set; }

        public byte StartByte => Buffer[0];
        public byte PacketId => Buffer[1];
        public byte COBByte => Buffer[2];
        public byte Size => Buffer[3];
        public byte[] Body => Buffer.Skip(4).Take(Size).ToArray();
        public byte CRC => Buffer[Buffer.Count - 2];

        public byte EndByte => Buffer.Last();
        public bool PassCRC => Body.CalcCRC() == CRC;

        public static readonly byte STARTBYTE = 0b01111110;
        public static readonly byte ENDBYTE = 0b10000001;

        public bool IsPacked { get; set; }
        public void Unpack()
        {
            if (!IsPacked) return;
            var testIdx = (int)COBByte;
            var delta = 0;
            if (testIdx >= 255)
            {
                IsPacked = false;
                return;
            }

            while (Body[testIdx] > 0)
            {
                delta = Body[testIdx];
                Body[testIdx] = STARTBYTE;
                testIdx += delta;
            }

        }

        public static Packet Create<T>(T item, byte packetId = 0) where T : struct
        {
            var data = ToByte(item);
            return Create(packetId, data);
        }
        /*
         *  01111110 00000000 11111111 00000000 00000000 00000000 ... 00000000 10000001
            |      | |      | |      | |      | |      | |      | | | |      | |______|__Stop byte
            |      | |      | |      | |      | |      | |      | | | |______|___________8-bit CRC
            |      | |      | |      | |      | |      | |      | |_|____________________Rest of payload
            |      | |      | |      | |      | |      | |______|________________________2nd payload byte
            |      | |      | |      | |      | |______|_________________________________1st payload byte
            |      | |      | |      | |______|__________________________________________# of payload bytes
            |      | |      | |______|___________________________________________________COBS Overhead byte
            |      | |______|____________________________________________________________Packet ID (0 by default)
            |______|_____________________________________________________________________Start byte (constant)
         */
        
        private static Packet Create(byte packetId, byte[] data)
        {
            var size = data.Length;
            var dataList = data.ToList();
            var overhead = FindOverheadByte(dataList);
            COBStuff(dataList);

            var buffer = new List<byte>(3 + dataList.Count + 2);
            buffer.Add(STARTBYTE);
            buffer.Add(packetId);
            buffer.Add(overhead);
            buffer.Add((byte)size);
            buffer.AddRange(dataList);
            buffer.Add(dataList.ToArray().CalcCRC());
            buffer.Add(ENDBYTE);
            return new Packet()
            {
                Buffer = buffer,
                IsPacked = true
            };
        }

        private static byte FindOverheadByte(List<Byte> data)
        {
            byte overhead = 0xFF;
            for (byte i = 0; i < data.Count; i++)
            {
                if (data[i] == STARTBYTE)
                {
                    return i;
                }
            }

            return overhead;
        }

        private static void COBStuff(List<byte> data)
        {
            var sbIdx = data.LastIndexOf(STARTBYTE);
            if (sbIdx < 0) return;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                if (data[i] != STARTBYTE) continue;
                data[i] = (byte)sbIdx;
                sbIdx = i;
            }
        }

        public static Packet Create(byte item, byte packetId = 0)
        {
            var data = new byte[] {item};
            return Create(packetId, data);
        }

        private static byte[] ToByte<T>(T input) where T : struct
        {

            var size = Marshal.SizeOf(typeof(T));
            var array = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(input, ptr, true);
            Marshal.Copy(ptr, array, 0, size);
            Marshal.FreeHGlobal(ptr);
            return array;

        }

    }
}