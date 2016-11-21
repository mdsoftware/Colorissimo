using System;
using System.Collections.Generic;
using Colorissimo.Core;

namespace Colorissimo.Core.Clustering
{
    class KMeansClustering : IClustering
    {
        private ClusteringPoint[] points;
        private List<ClusteringPoint> clusters;
        private int steps;

        public KMeansClustering(int steps)
        {
            this.points = null;
            this.clusters = null;
            this.steps = steps;
        }

        public ClusteringPoint[] Points
        {
            get { return this.points; }
        }

        public Color3[] Palette
        {
            get
            {
                if (this.clusters == null)
                    return null;
                Color3[] pal = new Color3[this.clusters.Count];
                for (int i = 0; i < this.clusters.Count; i++)
                {
                    ClusteringPoint p = this.clusters[i];
                    pal[i] =
                        new Color3((float)p[0], (float)p[1], (float)p[2]);
                }
                return pal;
            }
        }

        public Color3[] Clusters(int count)
        {
            Color3[] l = new Color3[count];
            int p = 0;
            for (int i = 0; i < this.clusters.Count; i++)
            {
                ClusteringPoint cp = this.clusters[i];
                l[p++] = new Color3((float)cp[0], (float)cp[1], (float)cp[2]);
                if (p >= count) break;
            }
            while (p < count)
                l[p++] = new Color3(0f, 0f, 0f);
            return l;
        }

        public void SetPoints(Color3[] list)
        {
            this.points = new ClusteringPoint[list.Length];
            this.clusters = null;
            for (int i = 0; i < this.points.Length; i++)
                this.points[i] = new ClusteringPoint((double)list[i].A, (double)list[i].B, (double)list[i].C);
        }

        public void AddCluster(Color3 cluster)
        {
            if (this.clusters == null)
                this.clusters = new List<ClusteringPoint>();
            this.clusters.Add(new ClusteringPoint((double)cluster.A, (double)cluster.B, (double)cluster.C));
        }

        public void Start()
        {
            this.Step();
        }

        public void Run()
        {
            for (int i = 0; i < this.steps; i++)
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