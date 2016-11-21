using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Colorissimo.Web
{

    public static class Utils
    {

        private static SortedDictionary<string, long> keys = new SortedDictionary<string, long>();
        private static object sync = new Object();
        private static long startTicks = new DateTime(1900, 1, 1).Ticks;
        private static string connStr =
            "data source=localhost\\SQL2008;Initial Catalog=PALETTE;user id=sa;Password=sasa;Max Pool Size=10;Min Pool Size=10;Connect Timeout=300"; // local

        private static MD5 md5 = MD5.Create();

        public static string ConnectionString
        {
            get
            {
                Monitor.Enter(Utils.sync);
                string s = Utils.connStr;
                Monitor.Exit(Utils.sync);
                return s;
            }
            set
            {
                Monitor.Enter(Utils.sync);
                Utils.connStr = value;
                Monitor.Exit(Utils.sync);
            }
        }


        public static string GetLogonHash(string login, string password)
        {
            if (String.IsNullOrEmpty(login)) login = "?";
            if (String.IsNullOrEmpty(password)) password = "?";
            StringBuilder sb = new StringBuilder(512);
            for (int i = 0; i < 20; i++)
            {
                sb.Append(login);
                sb.Append(password);
            }
            byte[] hash = Utils.md5.ComputeHash(Encoding.Unicode.GetBytes(sb.ToString()));
            sb.Length = 0;
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }

        public static string PackEvent(string s, Parameters param)
        {
            if (param == null) return s;
            return s + "_" + param.Serialize();
        }

        public static string UnpackEvent(string s, out Parameters param)
        {
            param = null;
            if (String.IsNullOrEmpty(s)) return null;
            if ((s.Length < 4) || (s.Length > 512)) return null;
            int i;
            for (i = 0; i < s.Length; i++)
                if (s[i] == '_') break;
            if (i >= s.Length) return s.ToLower();
            if (i < 4) return null;
            string p = s.Substring(i + 1, s.Length - i - 1);
            s = s.Substring(0, i);
            try
            {
                param = Parameters.Deserialize(p);
            }
            catch
            {
                param = null;
                return null;
            }
            return s.ToLower();
        }

        public static string GetTimeKey(int expirationSec)
        {
            string k = null;
            long sec = Utils.NowSeconds() + expirationSec;
            lock (Utils.sync)
            {
                k = Uid.Next();
                Utils.keys.Add(k.ToLower(), sec);
            }
            return k;
        }

        public static bool CheckTimeKey(string key)
        {
            if (key == null) return false;
            bool ok = false;
            long sec = Utils.NowSeconds();
            lock (Utils.sync)
            {

                string[] remove = new string[100];
                int p = 0;
                foreach (KeyValuePair<string, long> k in Utils.keys)
                {
                    if (k.Value < sec)
                    {
                        remove[p++] = k.Key;
                        if (p >= remove.Length) break;
                    }
                }
                for (int i = 0; i < p; i++)
                    Utils.keys.Remove(remove[i]);

                long s;
                if (Utils.keys.TryGetValue(key.ToLower(), out s))
                {
                    ok = (s >= sec);
                }
            }
            return ok;
        }

        public static long NowSeconds()
        {
            return (DateTime.Now.Ticks - Utils.startTicks) / TimeSpan.TicksPerSecond;
        }

        public static string SecureString(string s, int maxLength)
        {
            if (s == null) return String.Empty;
            int l = s.Length;
            if (l == 0) return String.Empty;
            if (l > maxLength) l = maxLength;
            char[] c = new char[l];

            int p = 0;
            bool lws = true;
            for (int i = 0; i < l; i++)
            {
                switch (s[i])
                {

                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        if (!lws)
                        {
                            c[p++] = ' ';
                            lws = true;
                        }
                        break;

                    case '&':
                    case '<':
                    case '>':
                    case '"':
                    case '?':
                    case '%':
                    case '[':
                    case ']':
                    case '@':
                    case '=':
                    case '/':
                        c[p++] = '?';
                        lws = false;
                        break;

                    default:
                        c[p++] = s[i];
                        lws = false;
                        break;
                }
            }

            return new String(c, 0, p);
        }

        public static unsafe void XorBuffer(byte[] buf, int offset, int count, ulong key)
        {
            fixed (byte* p = buf)
            {
                ulong* pp = (ulong*)(p + offset);
                while (count > 7)
                {
                    *pp = *pp ^ key;
                    pp++;
                    count -= 8;
                    ulong mask = (key & 0x1) == 0 ? 0x0 : 0x8000000000000000;
                    key = (key >> 1) | mask;
                }
                byte* ppb = (byte*)pp;
                byte* ppk = (byte*)&key;
                while (count > 0)
                {
                    *ppb = (byte)((int)(*ppb) ^ (int)(*ppk));
                    ppb++;
                    ppk++;
                    count--;
                }
            }
        }

        public static unsafe double LongToDouble(long v)
        {
            return *((double*)&v);
        }

        public static unsafe long DoubleToLong(double v)
        {
            return *((long*)&v);
        }
    }
}