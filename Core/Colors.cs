using System;
using System.Drawing;

namespace Colorissimo.Core
{

    public interface IColor3Transform
    {
        Color3 ToColor3(Color color);
        Color FromColor3(Color3 color);
    }

    public struct Color3 : IComparable<Color3>
    {
        public float A;
        public float B;
        public float C;
        public int Count;

        public Color3(float a, float b, float c)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.Count = 1;
        }

        public Color3(LabColor color)
        {
            this.A = color.L;
            this.B = color.A;
            this.C = color.B;
            this.Count = 1;
        }

        public Color3(XyzColor color)
        {
            this.A = color.X;
            this.B = color.Y;
            this.C = color.Z;
            this.Count = 1;
        }

        public Color3(HlsColor color)
        {
            this.A = (float)color.H / 100f;
            this.B = (float)color.L / 1000f;
            this.C = (float)color.S / 1000f;
            this.Count = 1;
        }

        public Color3(Color color)
        {
            this.A = (float)color.R / 255f;
            this.B = (float)color.G / 255f;
            this.C = (float)color.B / 255f;
            this.Count = 1;
        }

        public int CompareTo(Color3 x)
        {
            int r = this.A.CompareTo(x.A);
            if (r == 0) r = this.B.CompareTo(x.B);
            if (r == 0) r = this.C.CompareTo(x.C);
            return r;
        }

        public override string ToString()
        {
            return String.Format("({0},{1},{2})", this.A, this.B, this.C);
        }
    }

    public struct LabColor
    {
        public float L;
        public float A;
        public float B;

        public const double MaxHash = 200f;

        public LabColor(float l, float a, float b)
        {
            this.L = l;
            this.A = a;
            this.B = b;
        }

        public LabColor(double l, double a, double b)
        {
            this.L = (float)l;
            this.A = (float)a;
            this.B = (float)b;
        }

        public override string ToString()
        {
            return String.Format("L:{0} A:{1} B:{2}", this.L, this.A, this.B);
        }

        public int HashL
        {
            get
            {
                if ((this.L < 0f) || (this.L > 100f))
                    throw new ArgumentException("Invalid L value, must be 0..100");
                return (int)Math.Round(this.L / 100f * LabColor.MaxHash, 0);
            }
        }

        public int HashA
        {
            get
            {
                if ((this.A < -190f) || (this.A > 150f))
                    throw new ArgumentException("Invalid A value, must be -190..150");
                return (int)Math.Round((this.A - (-190f)) / (150f - (-190f)) * LabColor.MaxHash, 0);
            }
        }

        public int HashB
        {
            get
            {
                if ((this.B < -180f) || (this.B > 180f))
                    throw new ArgumentException("Invalid B value, must be -190..150");
                return (int)Math.Round((this.B - (-180f)) / (180f - (-180f)) * LabColor.MaxHash, 0);
            }
        }

        public double Distance(LabColor x)
        {
            float l = this.L - x.L;
            float a = this.A - x.A;
            float b = this.B - x.B;
            return Math.Sqrt((l * l) + (a * a) + (b * b));
        }
    }

    public struct XyzColor
    {
        public float X;
        public float Y;
        public float Z;

        public XyzColor(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public XyzColor(double x, double y, double z)
        {
            this.X = (float)x;
            this.Y = (float)y;
            this.Z = (float)z;
        }

        public override string ToString()
        {
            return String.Format("X:{0} Y:{1} Z:{2}", this.X, this.Y, this.Z);
        }
    }

    public struct HlsColor
    {
        public int H; // 0..36000 (1/100 degree)
        public short L; // 0..1000
        public short S; // 0..1000

        public HlsColor(double h, double l, double s)
        {
            this.H = (int)(h * 100f);
            this.L = (short)(l * 1000f);
            this.S = (short)(s * 1000f);
        }

        public HlsColor(int h, short l, short s)
        {
            this.H = h;
            this.L = l;
            this.S = s;
        }
    }

    public struct HsvColor
    {
        public int H; // 0..36000 (1/100 degree)
        public short S; // 0..1000
        public short V; // 0..1000

        public HsvColor(double h, double s, double v)
        {
            this.H = (int)(h * 100f);
            this.S = (short)(s * 1000f);
            this.V = (short)(v * 1000f);
        }

        public HsvColor(int h, int s, int v)
        {
            this.H = h;
            this.S = (short)s;
            this.V = (short)v;
        }
    }
}