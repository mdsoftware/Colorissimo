using System;
using System.Collections.Generic;
using System.Threading;

namespace Colorissimo.Web
{

    public static class Uid
    {
        private static string machineName = Environment.MachineName;
        private static Random random = new Random();
        private static ulong hash = 0;
        private static ulong count = Uid.MaxCount;
        private static object sync = new Object();

        private const ulong MaxCount = 0x7fffffff;

        public static ulong NextHash()
        {
            long ticks = DateTime.Now.Ticks;
            ulong crc = Crc64.InitialCrc;
            for (int i = 0; i < 16; i++)
            {
                switch (Uid.random.Next(2))
                {
                    case 0:
                        crc = Crc64.Update(Uid.machineName, crc);
                        break;

                    case 1:
                        crc = Crc64.Update((ulong)ticks, crc);
                        break;

                    default:
                        crc = Crc64.Update(Uid.random.Next(), crc);
                        break;
                }
            }
            return crc;
        }

        public static string Next()
        {
            return Uid.ConvertBits(Uid.NextHash(), Uid.NextHash(), 32);
        }

        private const string HexChars = "0123456789abcdef";

        private static string ConvertBits(ulong high, ulong low, int len)
        {
            char[] c = new char[32];
            int p = 31;
            for (int i = 0; i < 16; i++)
            {
                c[p--] = Uid.HexChars[(int)(low & 0xf)];
                low = low >> 4;
            }
            for (int i = 0; i < 16; i++)
            {
                c[p--] = Uid.HexChars[(int)(high & 0xf)];
                high = high >> 4;
            }
            return new String(c, 0, len);
        }

        public static string NextSequental()
        {
            Monitor.Enter(Uid.sync);
            if (Uid.count == Uid.MaxCount)
            {
                Uid.count = 0;
                Uid.hash = Uid.NextHash();
            }
            else
            {
                Uid.count++;
            }
            ulong high = Uid.hash;
            ulong low = Uid.count;
            Monitor.Exit(Uid.sync);

            return Uid.ConvertBits(high, low, 23);
        }

    }

    public static class Crc64
    {

        private const ulong Poly64Rev = 0x95AC9329AC4BC9B5;
        public const ulong InitialCrc = 0xFFFFFFFFFFFFFFFF;

        private static ulong[] crc64Table = null;

        private static void FillCrcTable()
        {
            ulong[] table = new ulong[256];
            for (ulong i = 0; i < 256; i++)
            {
                ulong part = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((part & 1) > 0)
                    {
                        part = (part >> 1) ^ Crc64.Poly64Rev;
                    }
                    else
                    {
                        part >>= 1;
                    }
                }
                table[i] = part;
            }
            Crc64.crc64Table = table;
            table = null;
        }

        public static unsafe ulong Update(string s, ulong crc)
        {
            if (s == null) return crc;
            int l = s.Length;
            if (l == 0) return crc;
            if (Crc64.crc64Table == null) Crc64.FillCrcTable();
            fixed (char* p = s)
            {
                crc = Crc64.Add((byte*)p, l << 1, crc);
            }
            return crc;
        }

        public static unsafe ulong Update(ulong l, ulong crc)
        {
            if (Crc64.crc64Table == null) Crc64.FillCrcTable();
            return Crc64.Add((byte*)(&l), 8, crc);
        }

        public static unsafe ulong Update(int i, ulong crc)
        {
            if (Crc64.crc64Table == null) Crc64.FillCrcTable();
            return Crc64.Add((byte*)(&i), 4, crc);
        }

        public static unsafe ulong Calculate(string s)
        {
            return Crc64.Update(s, Crc64.InitialCrc);
        }

        public static unsafe ulong Calculate(byte[] buffer)
        {
            return Crc64.Update(buffer, 0, buffer.Length, Crc64.InitialCrc);
        }

        public static unsafe ulong Calculate(byte[] buffer, int offset, int count)
        {
            return Crc64.Update(buffer, offset, count, Crc64.InitialCrc);
        }

        public static unsafe ulong Update(byte[] buffer, ulong crc)
        {
            return Crc64.Update(buffer, 0, buffer.Length, crc);
        }

        public static unsafe ulong Update(byte[] buffer, int offset, int count, ulong crc)
        {
            if (Crc64.crc64Table == null) Crc64.FillCrcTable();
            fixed (byte* p = buffer)
            {
                crc = Crc64.Add(p + offset, count, crc);
            }
            return crc;
        }

        private static unsafe ulong Add(byte* p, int count, ulong crc)
        {
            for (int i = 0; i < count; i++)
            {
                ulong f = (crc >> 56) ^ ((ulong)(*(p++)));
                crc = Crc64.crc64Table[(byte)(f & 0xff)] ^ (crc << 8);
            }
            return crc;
        }

    }

}