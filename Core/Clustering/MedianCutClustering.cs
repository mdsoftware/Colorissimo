using System;
using System.Collections.Generic;
using Colorissimo.Core;

namespace Colorissimo.Core.Clustering
{

    class MedianCutClustering:IClustering
    {
        private List<ColorCube> cubes;
        private int colors;
        private LabColor min;
        private LabColor max;

        public MedianCutClustering()
        {
            this.cubes = null;
            this.colors = 0;
        }

        public void AddCluster(Color3 cluster)
        {
            ++this.colors;
        }

        public void SetPoints(Color3[] list)
        {
            ColorCube cube = new ColorCube(list.Length);
            for (int i = 0; i < list.Length; i++)
            {
                PixelInfo px = new PixelInfo();
                px.A = px.B = px.C = 0;
                px.Index = i;
                cube.Pixels.Add(px);
            }

            this.max = new LabColor();
            this.min = new LabColor();

            for (int i = 0; i < cube.Pixels.Count; i++)
            {
                Color3 c = list[cube.Pixels[i].Index];
                LabColor lab = new LabColor(c.A, c.B, c.C);
                if (i == 0)
                {
                    min.L = max.L = lab.L;
                    min.A = max.A = lab.A;
                    min.B = max.B = lab.B;
                    continue;
                }
                min.L = MedianCutClustering.Min(lab.L, min.L);
                min.A = MedianCutClustering.Min(lab.A, min.A);
                min.B = MedianCutClustering.Min(lab.B, min.B);
                max.L = MedianCutClustering.Max(lab.L, max.L);
                max.A = MedianCutClustering.Max(lab.A, max.A);
                max.B = MedianCutClustering.Max(lab.B, max.B);
            }

            for (int i = 0; i < cube.Pixels.Count; i++)
            {
                PixelInfo px = cube.Pixels[i];
                Color3 c = list[cube.Pixels[i].Index];
                LabColor lab = new LabColor(c.A, c.B, c.C);
                px.A = MedianCutClustering.Hash(lab.L, min.L, max.L);
                px.B = MedianCutClustering.Hash(lab.A, min.A, max.A);
                px.C = MedianCutClustering.Hash(lab.B, min.B, max.B);
            }

            this.cubes = new List<ColorCube>();
            this.cubes.Add(cube);
        }

        public void Start()
        {
        }

        public void Run()
        {
            while (this.cubes.Count < this.colors)
            {
                List<ColorCube> l = new List<ColorCube>(colors + 4);
                for (int i = 0; i < this.cubes.Count; i++)
                {
                    ColorCube a;
                    ColorCube b;
                    this.cubes[i].Split(out a, out b);
                    l.Add(a);
                    l.Add(b);
                }
                this.cubes = null;
                this.cubes = l;
                l = null;
            }
        }

        public Color3[] Clusters(int count)
        {
            Color3[] l = new Color3[count];
            int p = 0;
            for (int i = 0; i < this.cubes.Count; i++)
            {
                ColorCube cube = this.cubes[i];
                long a = 0;
                long b = 0;
                long c = 0;
                int cnt = cube.Pixels.Count;
                for (int j = 0; j < cnt; j++)
                {
                    PixelInfo px = cube.Pixels[j];
                    a += (long)px.A;
                    b += (long)px.B;
                    c += (long)px.C;
                }
                l[p++] = new Color3(
                    MedianCutClustering.UnHash((int)(a / (long)cnt), min.L, max.L),
                    MedianCutClustering.UnHash((int)(b / (long)cnt), min.A, max.A),
                    MedianCutClustering.UnHash((int)(c / (long)cnt), min.B, max.B));
                if (p >= count) break;
            }
            while (p < count)
                l[p++] = new Color3(0f, 0f, 0f);
            return l;
        }

        private static float UnHash(int value, float min, float max)
        {
            return min + (float)((double)(max - min) * ((double)value / 1000f));
        }

        private static short Hash(float value, float min, float max)
        {
            double h = (double)((value - min) / (max - min)) * 1000f;
            return (short)(int)h;
        }

        private static float Min(float v, float min)
        {
            if (v < min) return v;
            return min;
        }

        private static float Max(float v, float max)
        {
            if (v > max) return v;
            return max;
        }

    }

    class ColorCube
    {
        public List<PixelInfo> Pixels;

        public ColorCube(int size)
        {
            this.Pixels = new List<PixelInfo>(size);
        }

        public void Split(out ColorCube a, out ColorCube b)
        {
            a = null;
            b = null;

            int minA = 0;
            int minB = 0;
            int minC = 0;
            int maxA = 0;
            int maxB = 0;
            int maxC = 0;

            for (int i = 0; i < this.Pixels.Count; i++)
            {
                PixelInfo p = this.Pixels[i];
                if (i == 0)
                {
                    minA = maxA = p.A;
                    minB = maxB = p.B;
                    minC = maxC = p.C;
                    continue;
                }
                minA = ColorCube.Min(p.A, minA);
                minB = ColorCube.Min(p.B, minB);
                minC = ColorCube.Min(p.C, minC);
                maxA = ColorCube.Max(p.A, maxA);
                maxB = ColorCube.Max(p.B, maxB);
                maxC = ColorCube.Max(p.C, maxC);
            }

            int A = maxA - minA;
            int B = maxB - minB;
            int C = maxC - minC;

            if ((A >= B) && (A >= C))
            {
                this.Pixels.Sort(ColorCube.CompareA);
            }
            else if ((B >= A) && (B >= C))
            {
                this.Pixels.Sort(ColorCube.CompareB);
            }
            else
            {
                this.Pixels.Sort(ColorCube.CompareC);
            }

            {
                int h = (this.Pixels.Count >> 1);
                int p = 0;
                a = new ColorCube(h);
                for (int i = 0; i < h; i++)
                    a.Pixels.Add(this.Pixels[p++]);

                h = this.Pixels.Count - h;
                b = new ColorCube(h);
                for (int i = 0; i < h; i++)
                    b.Pixels.Add(this.Pixels[p++]);
            }

        }

        private static int Min(short v, int min)
        {
            if (v < min) return v;
            return min;
        }

        private static int Max(short v, int max)
        {
            if (v > max) return v;
            return max;
        }

        public static int CompareA(PixelInfo x, PixelInfo y)
        {
            return x.A.CompareTo(y.A);
        }

        public static int CompareB(PixelInfo x, PixelInfo y)
        {
            return x.B.CompareTo(y.B);
        }

        public static int CompareC(PixelInfo x, PixelInfo y)
        {
            return x.C.CompareTo(y.C);
        }
    }

    sealed class PixelInfo
    {
        public short A;
        public short B;
        public short C;
        public int Index;

        public override string ToString()
        {
            return String.Format("{0}:{1}:{2} ({3})", this.A, this.B, this.C, this.Index);
        }
    }
}