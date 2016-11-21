using System;
using System.Drawing;

namespace Colorissimo.Core.Clustering
{

    public static class Clustering
    {
        public static IClustering KMeansClustering(int steps)
        {
            return new KMeansClustering(steps);
        }

        public static IClustering MedianCutClustering()
        {
            return new MedianCutClustering();
        }

        public static void ClusterPalettes(PaletteManager manager, int factor, int maxCluster)
        {
            PaletteClustering cl = new PaletteClustering(manager);

            cl.Load();

            int c = cl.Points.Length / factor;
            if (c <= 1) c = 5;
            if (c > cl.Points.Length) c = cl.Points.Length;
            if (maxCluster > 0)
                if (c > maxCluster) c = maxCluster;

            cl.AddCluster(c);
            cl.Start();
            cl.Run();
            cl.Update();

        }
    }

    public struct ClusteringPoint
    {
        private double[] values;
        public int Index;
        public int Id;

        public ClusteringPoint(int size)
        {
            this.values = new double[size];
            for (int i = 0; i < this.values.Length; i++) this.values[i] = 0f;
            this.Index = -1;
            this.Id = 0;
        }

        public ClusteringPoint(ClusteringPoint p)
        {
            this.values = new double[p.values.Length];
            for (int i = 0; i < p.values.Length; i++)
                this.values[i] = p.values[i];
            this.Index = p.Index;
            this.Id = p.Id;
        }

        public ClusteringPoint(int id, double[] v, int count)
        {
            this.values = new double[count];
            for (int i = 0; i < count; i++)
                this.values[i] = v[i];
            this.Index = -1;
            this.Id = id;
        }

        public ClusteringPoint(double x, double y, double z)
        {
            this.values = new double[3];
            this.values[0] = x;
            this.values[1] = y;
            this.values[2] = z;
            this.Index = -1;
            this.Id = 0;
        }

        public int Size
        {
            get { return this.values.Length; }
        }

        public double this[int i]
        {
            get { return this.values[i]; }
            set { this.values[i] = value; }
        }

        public double Distance(ClusteringPoint p)
        {
            int l = this.values.Length;
            if (l != p.values.Length)
                throw new Exception("Point dimensions must be the same");
            double[] v = new double[l];
            for (int i = 0; i < l; i++)
                v[i] = this.values[i] - p.values[i];
            double f = 0f;
            for (int i = 0; i < l; i++)
                f += (v[i] * v[i]);
            return Math.Sqrt(f);
        }

        public override string ToString()
        {
            string s = String.Empty;
            for (int i = 0; i < this.values.Length; i++)
            {
                if (i > 0) s += ",";
                s += this.values[i].ToString();
            }
            return s;
        }
    }

}