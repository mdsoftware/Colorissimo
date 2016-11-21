using System;
using System.Collections.Generic;
using System.Drawing;

namespace Colorissimo.Core
{

    public static class ColorTransform
    {

        public static Color IntToRgb(int c)
        {
            return Color.FromArgb((byte)((c >> 16) & 0xff), (byte)((c >> 8) & 0xff), (byte)(c & 0xff));
        }

        public static double LabDistance(Color x, Color y)
        {
            return ColorTransform.RgbToLab(x).Distance(ColorTransform.RgbToLab(y));
        }

        public static Color DarkColor(Color color, int percent)
        {
            HlsColor hls = ColorTransform.RgbToHls(color);

            float l = (float)hls.L / 1000f;
            l = l - ((l * (float)percent) / 100f);
            if (l < 0f) l = 0f;
            if (l > 1f) l = 1f;
            hls.L = (short)(l * 1000f);

            return ColorTransform.HlsToRgb(hls);
        }

        public static Color HsvToRgb(HsvColor hsv)
        {

            double h = (double)hsv.H / 100f;
            double s = (double)hsv.S / 1000f;
            double v = (double)hsv.V / 1000f;

            double chroma = s * v;
            double hdash = h / 60.0;
            double x = chroma * (1.0 - Math.Abs((hdash % 2.0) - 1.0));

            double r = 0f;
            double g = 0f;
            double b = 0f;
            if (hdash < 1.0)
            {
                r = chroma;
                g = x;
            }
            else if (hdash < 2.0)
            {
                r = x;
                g = chroma;
            }
            else if (hdash < 3.0)
            {
                g = chroma;
                b = x;
            }
            else if (hdash < 4.0)
            {
                g = x;
                b = chroma;
            }
            else if (hdash < 5.0)
            {
                r = x;
                b = chroma;
            }
            else if (hdash < 6.0)
            {
                r = chroma;
                b = x;
            }

            double min = v - chroma;

            r += min;
            g += min;
            b += min;

            return Color.FromArgb(
                    (int)(r * 255f),
                    (int)(g * 255f),
                    (int)(b * 255f)
                  );
        }

        public static HsvColor RgbToHsv(Color rgb)
        {

            double r = (double)rgb.R / 255f;
            double g = (double)rgb.G / 255f;
            double b = (double)rgb.B / 255f;

            double min = ColorTransform.FMin(ColorTransform.FMin(r, g), b);
            double max = ColorTransform.FMax(ColorTransform.FMax(r, g), b);
            double chroma = max - min;

            double h = 0f;
            double s = 0f;

            //If Chroma is 0, then S is 0 by definition, and H is undefined but 0 by convention.
            if (chroma != 0f)
            {
                if (r == max)
                {
                    h = (g - b) / chroma;

                    if (h < 0.0)
                        h += 6.0;
                }
                else if (g == max)
                {
                    h = ((b - r) / chroma) + 2.0f;
                }
                else
                {
                    h = ((r - g) / chroma) + 4.0f;
                }

                h *= 60.0f;
                s = chroma / max;
            }

            return new HsvColor(h, s, max);
        }

        private static double FMin(double x, double y)
        {
            return (x < y) ? x : y;
        }

        private static double FMax(double x, double y)
        {
            return (x > y) ? x : y;
        }


        public static Color HlsToRgb(HlsColor hls)
        {
            double h = (double)hls.H / 100f;
            double l = (double)hls.L / 1000f;
            double s = (double)hls.S / 1000f;

            double p2;
            if (l <= 0.5f)
            {
                p2 = l * (1f + s);
            }
            else
            {
                p2 = l + s - l * s;
            }
            double p1 = 2f * l - p2;
            if (s == 0f)
            {
                int c = (int)(l * 255f);
                return Color.FromArgb(c, c, c);
            }
            return Color.FromArgb(
                (int)(ColorTransform.QqhToRgb(p1, p2, h + 120f) * 255f),
                (int)(ColorTransform.QqhToRgb(p1, p2, h) * 255f),
                (int)(ColorTransform.QqhToRgb(p1, p2, h - 120f) * 255f)
            );
        }

        private static double QqhToRgb(double q1, double q2, double hue)
        {
            if (hue > 360f)
            {
                hue = hue - 360f;
            }
            else if (hue < 0f)
            {
                hue = hue + 360f;
            }
            if (hue < 60f)
                return q1 + (q2 - q1) * hue / 60f;
            if (hue < 180f)
                return q2;
            if (hue < 240f)
                return q1 + (q2 - q1) * (240f - hue) / 60f;
            return q1;
        }

        public static HlsColor RgbToHls(Color rgb)
        {
            double r = (double)rgb.R / 255f;
            double g = (double)rgb.G / 255f;
            double b = (double)rgb.B / 255f;
            double h = 0f;
            double l = 0f;
            double s = 0f;
            // Get the maximum and minimum RGB components.
            double max = r;
            if (max < g) max = g;
            if (max < b) max = b;

            double min = r;
            if (min > g) min = g;
            if (min > b) min = b;

            double diff = max - min;
            l = (max + min) / 2f;
            if (Math.Abs(diff) < 0.00001f)
            {
                s = 0f;
                h = 0f;   // H is really undefined.
            }
            else
            {
                if (l <= 0.5f)
                {
                    s = diff / (max + min);
                }
                else
                {
                    s = diff / (2f - max - min);
                }

                double r_dist = (max - r) / diff;
                double g_dist = (max - g) / diff;
                double b_dist = (max - b) / diff;

                if (r == max)
                {
                    h = b_dist - g_dist;
                }
                else if (g == max)
                {
                    h = 2f + r_dist - b_dist;
                }
                else
                {

                    h = 4 + g_dist - r_dist;
                }

                h = h * 60f;
                if (h < 0f) h = h + 360f;
            }

            return new HlsColor(h, l, s);
        }

        public static List<Color> CreateGradient(Color start, Color end, int steps, ColorGradientFlags flags, double saturation)
        {
            HsvColor c0 = ColorTransform.RgbToHsv(start);
            HsvColor c1 = ColorTransform.RgbToHsv(end);

            int value = 0;
            if ((flags & ColorGradientFlags.MinValue) != 0)
            {
                value = (c0.V < c1.V) ? c0.V : c1.V;
            }
            else if ((flags & ColorGradientFlags.MaxValue) != 0)
            {
                value = (c0.V < c1.V) ? c1.V : c0.V;
            }
            else
            {
                value = (c0.V + c1.V) >> 1;
            }

            int h = c0.H;
            int a = c1.H - h;
            if (a < 0)
                a = a + 36000;
            int step = 0;
            if ((flags & ColorGradientFlags.CounterClockwise) == 0)
            {
                step = (int)((double)(a) / (double)(steps - 1));
            }
            else
            {
                step = -(int)((double)(36000 - a) / (double)(steps - 1));
            }

            int s = c0.S;
            int sstep = 0;
            if ((flags & ColorGradientFlags.ReplaceSaturation) != 0)
            {
                s = (int)(saturation * 1000f);
            }
            else if ((flags & ColorGradientFlags.AlignSaturation) == 0)
            {
                sstep = (int)((double)(c1.S - s) / (double)steps);
            }

            List<Color> l = new List<Color>();

            for (int i = 0; i < steps; i++)
            {
                l.Add(ColorTransform.HsvToRgb(new HsvColor(h, s, value)));
                s += sstep;
                h += step;
                if (h < 0)
                {
                    h = h + 36000;
                }
                else if (h > 36000)
                {
                    h = h - 36000;
                }
            }

            return l;
        }

        public static string ToWebColor(Color color)
        {
            return "#" + color.R.ToString("X2") +
                color.G.ToString("X2") +
                color.B.ToString("X2");
        }

        public static XyzColor RgbToXyz(Color rgb)
        {
            return LabColorTransform.RgbToXyz(rgb);
        }

        public static LabColor XyzToLab(XyzColor xyz)
        {
            return LabColorTransform.XyzToLab(xyz);
        }

        public static XyzColor LabToXyz(LabColor lab)
        {

            return LabColorTransform.LabToXyz(lab);
        }

        public static Color XyzToRgb(XyzColor xyz)
        {
            return LabColorTransform.XyzToRgb(xyz);
        }

        public static LabColor RgbToLab(Color rgb)
        {
            return LabColorTransform.RgbToLab(rgb);
        }

        public static Color LabToRgb(LabColor lab)
        {
            return LabColorTransform.LabToRgb(lab);
        }

        public static double ColorDistance(Color x, Color y)
        {
            return ColorTransform.RgbToLab(x).Distance(ColorTransform.RgbToLab(y));
        }

        public static Color BlendColors4W16IP(Color c1, int w1, Color c2, int w2, Color c3, int w3, Color c4, int w4)
        {

            if ((w1 + w2 + w3 + w4) != 65536)
                throw new ArgumentOutOfRangeException("Sum of w factors must equal 65536");

            int ww = 32768;
            int af = (c1.A * w1) + (c2.A * w2) + (c3.A * w3) + (c4.A * w4);
            int a = (af + ww) >> 16;

            int b;
            int g;
            int r;

            if (a == 0)
            {
                b = 0;
                g = 0;
                r = 0;
            }
            else
            {
                b = (int)(((c1.A * c1.B * w1) + (c2.A * c2.B * w2) + (c3.A * c3.B * w3) + (c4.A * c4.B * w4)) / af);
                g = (int)(((c1.A * c1.G * w1) + (c2.A * c2.G * w2) + (c3.A * c3.G * w3) + (c4.A * c4.G * w4)) / af);
                r = (int)(((c1.A * c1.R * w1) + (c2.A * c2.R * w2) + (c3.A * c3.R * w3) + (c4.A * c4.R * w4)) / af);
            }

            return Color.FromArgb(a, r, g, b);
        }
    }

    [Flags]
    public enum ColorGradientFlags : ushort
    {
        Empty = 0x0000,
        MinValue = 0x0001,
        MaxValue = 0x0002,
        CounterClockwise = 0x0004,
        AlignSaturation = 0x0008,
        ReplaceSaturation = 0x0010
    }

}