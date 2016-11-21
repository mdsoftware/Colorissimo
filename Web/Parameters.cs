using System;
using System.Text;
using System.Collections.Generic;

namespace Colorissimo.Web
{
    public sealed class Parameters
    {
        private SortedDictionary<string, string> items;

        private const ulong EncryptKey = 0x44656E69734D6430;

        public Parameters()
        {
            this.items = new SortedDictionary<string, string>();
        }

        public void Clear()
        {
            this.items.Clear();
        }

        public string[] AllNames
        {
            get
            {
                string[] n = new string[this.items.Count];
                int i = 0;
                foreach (KeyValuePair<string, string> p in this.items)
                    n[i++] = p.Key;
                return n;
            }
        }

        public long Get(string name, long defaultValue)
        {
            string v = this[name];
            if (v == null) return defaultValue;
            long l;
            if (Int64.TryParse(v, out l))
                return l;
            return defaultValue;
        }

        public void Set(string name, long value)
        {
            this[name] = value.ToString();
        }

        public double Get(string name, double defaultValue)
        {
            return Utils.LongToDouble(this.Get(name, Utils.DoubleToLong(defaultValue)));
        }

        public void Set(string name, double value)
        {
            this.Set(name, Utils.DoubleToLong(value));
        }

        public DateTime Get(string name, DateTime defaultValue)
        {
            return new DateTime(this.Get(name, defaultValue.Ticks));
        }

        public void Set(string name, DateTime value)
        {
            this.Set(name, value.Ticks);
        }

        public bool Get(string name, bool defaultValue)
        {
            return this.Get(name, defaultValue ? 1L : 0L) == 1L;
        }

        public void Set(string name, bool value)
        {
            this.Set(name, value ? 1L : 0L);
        }

        public string this[string name]
        {
            get
            {
                name = name.ToLower();
                if (this.items.ContainsKey(name))
                    return this.items[name];
                return null;
            }
            set
            {
                name = name.ToLower();
                if (value == null)
                {
                    if (this.items.ContainsKey(name)) this.items.Remove(name);
                    return;
                }
                if (this.items.ContainsKey(name))
                {
                    this.items[name] = value;
                }
                else
                {
                    this.items.Add(name, value);
                }
            }
        }

        public string this[string name, string defaultValue]
        {
            get
            {
                name = name.ToLower();
                if (this.items.ContainsKey(name))
                    return this.items[name];
                return defaultValue;
            }
        }

        public string Serialize()
        {
            return this.Serialize(true);
        }

        public string Serialize(bool ascii)
        {
            StringBuilder sb = new StringBuilder();
            int c = 0;
            foreach (KeyValuePair<string, string> p in this.items)
            {
                if (c > 0) sb.Append('\0');
                sb.Append(p.Key);
                sb.Append('\0');
                sb.Append(p.Value);
                c++;
            }
            byte[] buf = ascii ? Encoding.ASCII.GetBytes(sb.ToString()) : Encoding.Unicode.GetBytes(sb.ToString());
            Utils.XorBuffer(buf, 0, buf.Length, Parameters.EncryptKey);
            return Convert.ToBase64String(buf);
        }

        public static Parameters Deserialize(string base64)
        {
            return Parameters.Deserialize(base64, true);
        }

        public static Parameters Deserialize(string base64, bool ascii)
        {
            byte[] buf = Convert.FromBase64String(base64);
            Utils.XorBuffer(buf, 0, buf.Length, Parameters.EncryptKey);
            string s = ascii ? Encoding.ASCII.GetString(buf) : Encoding.Unicode.GetString(buf);
            buf = null;
            Parameters par = new Parameters();
            string name = null;
            int p = 0;
            int i = 0;

            for (i = 0; i < s.Length; i++)
            {
                if (s[i] == '\0')
                {
                    if (name == null)
                    {
                        name = s.Substring(p, i - p);
                    }
                    else
                    {
                        par.items.Add(name, s.Substring(p, i - p));
                        name = null;
                    }
                    p = i + 1;
                    continue;
                }
            }
            if (name != null)
                par.items.Add(name, s.Substring(p, i - p));

            s = null;
            name = null;

            return par;
        }
    }
}