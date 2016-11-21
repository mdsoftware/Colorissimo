using System;
using System.Collections.Generic;
using Colorissimo.Core;

namespace Colorissimo.Core.Clustering
{

    public sealed class PaletteClustering
    {

        private ClusteringPoint[] points;
        private List<ClusteringPoint> clusters;
        private PaletteManager manager;

        public PaletteClustering(PaletteManager manager)
        {
            this.points = null;
            this.clusters = null;
            this.manager = manager;
        }

        public ClusteringPoint[] Points
        {
            get { return this.points; }
        }

        public ClusteringPoint[] Clusters
        {
            get { return this.clusters.ToArray(); }
        }

        public void AddCluster(float x, float y, float z)
        {
            if (this.clusters == null)
                this.clusters = new List<ClusteringPoint>();
            this.clusters.Add(new ClusteringPoint((double)x, (double)y, (double)z));
        }

        public void AddCluster(int count)
        {
            if (this.points == null)
                return;
            if (this.clusters == null)
                this.clusters = new List<ClusteringPoint>();
            Random rnd = new Random();
            for (int i = 0; i < count; i++)
            {
                ClusteringPoint p = this.points[rnd.Next(this.points.Length)];
                this.clusters.Add(new ClusteringPoint(p));
            }
        }

        public void Update()
        {
            if (this.clusters == null)
                return;
            this.manager.UpdatePaletteClusters(this.clusters.ToArray(), this.points);
        }

        public void Load()
        {
            this.points = this.manager.LoadPaletteColors();
            this.clusters = null;
        }

        public void Start()
        {
            this.Step();
        }

        public void Run()
        {
            for (int i = 0; i < 100; i++)
                this.Step();
        }

        public void Step()
        {

            List<int>[] clusterPoints = new List<int>[this.clusters.Count];
            for (int i = 0; i < clusterPoints.Length; i++)
                clusterPoints[i] = new List<int>();

            int cc = this.clusters.Count;
            for (int i = 0; i < this.points.Length; i++)
            {
                int c = -1;
                double dist = 0f;
                ClusteringPoint p = this.points[i];
                for (int j = 0; j < cc; j++)
                {
                    double x = p.Distance(this.clusters[j]);
                    if ((j == 0) || (x < dist))
                    {
                        c = j;
                        dist = x;
                    }
                }
                clusterPoints[c].Add(i);
                this.points[i].Index = c;
            }

            int size = this.points[0].Size;

            for (int i = 0; i < cc; i++)
            {
                List<int> l = clusterPoints[i];
                if (l.Count == 0)
                    continue;


                double[] v = new double[size];
                for (int k = 0; k < size; k++)
                    v[k] = 0f;
                for (int j = 0; j < l.Count; j++)
                {
                    ClusteringPoint p0 = this.points[l[j]];
                    for (int k = 0; k < size; k++)
                        v[k] += p0[k];
                }

                ClusteringPoint c = this.clusters[i];
                double f = (double)l.Count;
                for (int k = 0; k < size; k++)
                    c[k] = v[k] / f;
                this.clusters[i] = c;
            }
        }
    }
}