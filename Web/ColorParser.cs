using System;
using System.Collections.Generic;
using System.Drawing;

namespace Colorissimo.Web
{
    public static class ColorParser
    {

        public const int MaxStringSize = 128;

        public const int InvalidColor = -1;

        public static int Parse(string s)
        {
            ColorItem[] parsed = ColorParser.Split(s);
            if (parsed == null)
                return ColorParser.InvalidColor;

            if (ColorParser.Match(parsed, "x"))
            {
                string h = parsed[0].Token;
                if (String.IsNullOrEmpty(h)) return ColorParser.InvalidColor;
                if (h.Length == 3)
                {
                    h = String.Empty + h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
                }
                else if (h.Length != 6)
                {
                    return ColorParser.InvalidColor;
                }

                int c;
                if (!Int32.TryParse(h, System.Globalization.NumberStyles.HexNumber, null, out c)) 
                    return ColorParser.InvalidColor;
                if ((c < 0) || (c > 0xffffff)) 
                    return ColorParser.InvalidColor;
                return c;
            }

            if (ColorParser.Match(parsed, "n"))
            {
                int c;
                if (!Int32.TryParse(parsed[0].Token, out c))
                    return ColorParser.InvalidColor;
                if ((c < 0) || (c > 0xffffff))
                    return ColorParser.InvalidColor;
                return c;
            }

            if (ColorParser.Match(parsed, "n,n,n"))
            {
                double r;
                if (!ColorParser.FloatFrom(parsed, 0, 0f, 255f, out r)) 
                    return ColorParser.InvalidColor;
                double g;
                if (!ColorParser.FloatFrom(parsed, 2, 0f, 255f, out g)) 
                    return ColorParser.InvalidColor;
                double b;
                if (!ColorParser.FloatFrom(parsed, 4, 0f, 255f, out b)) 
                    return ColorParser.InvalidColor;
                return Color.FromArgb((int)r, (int)g, (int)b).ToArgb() & 0xffffff;
            }

            if (ColorParser.Match(parsed, "nnn"))
            {
                double r;
                if (!ColorParser.FloatFrom(parsed, 0, 0f, 255f, out r)) return ColorParser.InvalidColor;
                double g;
                if (!ColorParser.FloatFrom(parsed, 1, 0f, 255f, out g)) return ColorParser.InvalidColor;
                double b;
                if (!ColorParser.FloatFrom(parsed, 2, 0f, 255f, out b)) return ColorParser.InvalidColor;
                return Color.FromArgb((int)r, (int)g, (int)b).ToArgb() & 0xffffff;
            }

            if (ColorParser.Match(parsed, "f,f,f"))
            {
                double r;
                if (!ColorParser.FloatFrom(parsed, 0, 0f, 1f, out r)) return ColorParser.InvalidColor;
                double g;
                if (!ColorParser.FloatFrom(parsed, 2, 0f, 1f, out g)) return ColorParser.InvalidColor;
                double b;
                if (!ColorParser.FloatFrom(parsed, 4, 0f, 1f, out b)) return ColorParser.InvalidColor;
                return Color.FromArgb((int)(r * 255f), (int)(g * 255f), (int)(b * 255f)).ToArgb() & 0xffffff;
            }

            if (ColorParser.Match(parsed, "fff"))
            {
                double r;
                if (!ColorParser.FloatFrom(parsed, 0, 0f, 1f, out r)) return ColorParser.InvalidColor;
                double g;
                if (!ColorParser.FloatFrom(parsed, 1, 0f, 1f, out g)) return ColorParser.InvalidColor;
                double b;
                if (!ColorParser.FloatFrom(parsed, 2, 0f, 1f, out b)) return ColorParser.InvalidColor;
                return Color.FromArgb((int)(r * 255f), (int)(g * 255f), (int)(b * 255f)).ToArgb() & 0xffffff;
            }

            if (ColorParser.Match(parsed, "r:fg:fb:f") || ColorParser.Match(parsed, "r=fg=fb=f"))
            {
                double r;
                if (!ColorParser.FloatFrom(parsed, 2, 0f, 1f, out r)) return ColorParser.InvalidColor;
                double g;
                if (!ColorParser.FloatFrom(parsed, 5, 0f, 1f, out g)) return ColorParser.InvalidColor;
                double b;
                if (!ColorParser.FloatFrom(parsed, 8, 0f, 1f, out b)) return ColorParser.InvalidColor;
                return Color.FromArgb((int)(r * 255f), (int)(g * 255f), (int)(b * 255f)).ToArgb() & 0xffffff;
            }

            if (ColorParser.Match(parsed, "rfgfbf"))
            {
                double r;
                if (!ColorParser.FloatFrom(parsed, 1, 0f, 1f, out r)) return ColorParser.InvalidColor;
                double g;
                if (!ColorParser.FloatFrom(parsed, 3, 0f, 1f, out g)) return ColorParser.InvalidColor;
                double b;
                if (!ColorParser.FloatFrom(parsed, 5, 0f, 1f, out b)) return ColorParser.InvalidColor;
                return Color.FromArgb((int)(r * 255f), (int)(g * 255f), (int)(b * 255f)).ToArgb() & 0xffffff;
            }

            return ColorParser.InvalidColor;
        }

        private static bool FloatFrom(ColorItem[] parsed, int p, double min, double max, out double v)
        {
            v = Double.NaN;
            if ((p < 0) || (p >= parsed.Length)) return false;
            if ((parsed[p].Type & ParserState.FloatNumber) != 0)
            {
                if (!Double.TryParse(parsed[p].Token, out v))
                    return false;
            }
            else if ((parsed[p].Type & ParserState.Number) != 0)
            {
                int i;
                if (!Int32.TryParse(parsed[p].Token, out i))
                    return false;
                v = (double)i;
            }
            else
            {
                return false;
            }
            if ((v < min) || (v > max)) return false;
            return true;
        }

        /// <summary>
        /// r,g,b,l,a,h,s - letter
        /// delimiter - delimiter (, = :)
        /// x - hex number
        /// n - integer number
        /// f - integer or float number
        /// </summary>
        /// <param name="parsed"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        private static bool Match(ColorItem[] parsed, string template)
        {
            if (parsed.Length != template.Length) return false;
            for (int i = 0; i < template.Length; i++)
            {
                char c = Char.ToUpper(template[i]); 
                switch (c)
                {
                    case 'R':
                    case 'G':
                    case 'B':
                    case 'L':
                    case 'A':
                    case 'H':
                    case 'S':
                        if ((parsed[i].Type & ParserState.Letter) == 0)
                            return false;
                        if (c != parsed[i].Token[0])
                            return false;
                        break;

                    case 'X':
                        if ((parsed[i].Type & ParserState.HexNumber) == 0)
                            return false;
                        break;

                    case 'N':
                        if ((parsed[i].Type & ParserState.Number) == 0)
                            return false;
                        break;

                    case 'F':
                        if ((parsed[i].Type & (ParserState.Number | ParserState.FloatNumber)) == 0)
                            return false;
                        break;

                    case ',':
                    case ':':
                    case '=':
                        if ((parsed[i].Type & ParserState.Delimiter) == 0)
                            return false;
                        if (c != parsed[i].Token[0])
                            return false;
                        break;

                    default:
                        return false;
                }
            }
            return true;
        }

        private static ColorItem[] Split(string s)
        {
            if (s == null) return null;
            if (s.Length > ColorParser.MaxStringSize) return null;
            ParserState state = ParserState.Space;
            List<ColorItem> l = new List<ColorItem>();
            char[] buf = new char[ColorParser.MaxStringSize];
            int p = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = Char.ToUpper(s[i]);
                switch (c)
                {
                    case '#':
                        if (state != ParserState.Space)
                        {
                            if (p > 0)
                            {
                                l.Add(new ColorItem(new String(buf, 0, p), state));
                                p = 0;
                            }
                        }
                        state = ParserState.HexNumber;
                        break;

                    case ',':
                    case ':':
                    case '=':
                        if (state != ParserState.Space)
                        {
                            if (p > 0)
                            {
                                l.Add(new ColorItem(new String(buf, 0, p), state));
                                p = 0;
                            }
                            state = ParserState.Space;
                        }
                        l.Add(new ColorItem(c.ToString(), ParserState.Delimiter));
                        break;

                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        if (state != ParserState.Space)
                        {
                            if (p > 0)
                            {
                                l.Add(new ColorItem(new String(buf, 0, p), state));
                                p = 0;
                            }
                            state = ParserState.Space;
                        }
                        break;

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        if ((state & (ParserState.Number | ParserState.FloatNumber | ParserState.HexNumber)) == 0)
                        {
                            if (p > 0)
                            {
                                l.Add(new ColorItem(new String(buf, 0, p), state));
                                p = 0;
                            }
                            state = ParserState.Number;
                        }
                        buf[p++] = c;
                        break;

                    case 'A':
                    case 'B':
                        if (state == ParserState.HexNumber)
                        {
                            buf[p++] = c;
                        }
                        else
                        {
                            if (state != ParserState.Space)
                            {
                                if (p > 0)
                                {
                                    l.Add(new ColorItem(new String(buf, 0, p), state));
                                    p = 0;
                                }
                                state = ParserState.Space;
                            }
                            l.Add(new ColorItem(c.ToString().ToUpper(), ParserState.Letter));
                        }
                        break;

                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                        if (state != ParserState.HexNumber)
                            return null;
                        buf[p++] = c;
                        break;

                    case 'R':
                    case 'G':
                    case 'L':
                    case 'H':
                    case 'S':
                    case 'V':
                        if (state != ParserState.Space)
                        {
                            if (p > 0)
                            {
                                l.Add(new ColorItem(new String(buf, 0, p), state));
                                p = 0;
                            }
                            state = ParserState.Space;
                        }
                        l.Add(new ColorItem(s[i].ToString().ToUpper(), ParserState.Letter));
                        break;


                    case '.':
                        if (state != ParserState.Number)
                            return null;
                        buf[p++] = s[i];
                        state = ParserState.FloatNumber;
                        break;

                    default:
                        return null;
                }
            }
            if (state != ParserState.Space)
                if (p > 0)
                    l.Add(new ColorItem(new String(buf, 0, p), state));
            return l.ToArray();
        }

        struct ColorItem
        {
            public string Token;
            public ParserState Type;

            public ColorItem(string token, ParserState type)
            {
                this.Token = token;
                this.Type = type;
            }

            public override string ToString()
            {
                return String.Format("{0} '{1}'", this.Type, this.Token);
            }

        }
    }

    [Flags]
    enum ParserState : byte
    {
        Space = 0x00,
        Number = 0x01,
        FloatNumber = 0x02,
        HexNumber = 0x04,
        Delimiter = 0x10,
        Letter = 0x20
    }
}