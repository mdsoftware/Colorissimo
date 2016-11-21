using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data.SqlClient;
using System.Drawing;
using Colorissimo.Core.Clustering;

namespace Colorissimo.Core
{

    sealed class PaletteSortContext:IComparer<PaletteItem>
    {
        private SortedDictionary<int, PaletteItem> clusters;
        private ClusteringPoint colorBase;

        PaletteSortContext(Color baseColor, SortedDictionary<int, PaletteItem> clusters)
        {
            this.clusters = clusters;
            this.colorBase = PaletteSortContext.ToPoint(new Color[8] { Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black });
        }

        public int Compare(PaletteItem x, PaletteItem y)
        {
            return 0;
        }

        public static void Sort(List<PaletteItem> list, PaletteListSortMode mode, SortedDictionary<int, PaletteItem> clusters)
        {
            switch (mode)
            {
                case PaletteListSortMode.Title:
                    list.Sort(PaletteSortContext.CompareByTitle);
                    break;

                case PaletteListSortMode.ClusterId:
                    list.Sort(PaletteSortContext.CompareByClusterId);
                    break;

                case PaletteListSortMode.PaletteColor:
                    list.Sort(new PaletteSortContext(Color.Black, null).CompareByPaletteColor);
                    break;

                case PaletteListSortMode.ClusterColor:
                    list.Sort(new PaletteSortContext(Color.Black, clusters).CompareByClusterColor);
                    break;
            }
        }

        private int CompareByPaletteColor(PaletteItem x, PaletteItem y)
        {
            ClusteringPoint px = PaletteSortContext.ToPoint(x.Colors);
            ClusteringPoint py = PaletteSortContext.ToPoint(y.Colors);
            double dx = this.colorBase.Distance(px);
            double dy = this.colorBase.Distance(py);
            int r = dx.CompareTo(dy);
            if (r == 0)
                r = PaletteSortContext.CompareByTitle(x, y);
            return r;
        }

        private int CompareByClusterColor(PaletteItem x, PaletteItem y)
        {
            if (this.clusters == null)
                return PaletteSortContext.CompareByTitle(x, y);
            ClusteringPoint px = this.colorBase;
            if (this.clusters.ContainsKey(x.Cluster))
                px = PaletteSortContext.ToPoint(this.clusters[x.Cluster].Colors);
            ClusteringPoint py = this.colorBase;
            if (this.clusters.ContainsKey(y.Cluster))
                py = PaletteSortContext.ToPoint(this.clusters[y.Cluster].Colors);
            double dx = this.colorBase.Distance(px);
            double dy = this.colorBase.Distance(py);
            int r = dx.CompareTo(dy);
            if (r == 0)
                r = PaletteSortContext.CompareByTitle(x, y);
            return r;
        }

        private static ClusteringPoint ToPoint(Color[] colors)
        {
            ClusteringPoint cp = new ClusteringPoint(24);
            int i = 0;
            for (int j = 0; j < colors.Length; j++)
            {
                LabColor lc = ColorTransform.RgbToLab(colors[j]);
                cp[i++] = lc.L;
                cp[i++] = lc.A;
                cp[i++] = lc.B;
                if (i >= cp.Size) break;
            }
            return cp;
        }

        private static int CompareByTitle(PaletteItem x, PaletteItem y)
        {
            int r = String.Compare(x.Title, y.Title, true);
            if (r == 0)
                r = x.Id.CompareTo(y.Id);
            return r;
        }

        private static int CompareByClusterId(PaletteItem x, PaletteItem y)
        {
            int r = -x.Cluster.CompareTo(y.Cluster);
            if (r == 0)
                r = PaletteSortContext.CompareByTitle(x, y);
            return r;
        }
    }

}