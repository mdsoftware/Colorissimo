using System;
using System.Drawing;

namespace Colorissimo.Core
{

    public enum ColorSearchWidth : int
    {
        Exact = 0,
        Narrow = 1,
        Wide = 2,
        VeryWide = 3,
        Widest = 4,
    }

    public enum PaletteListSortMode : int
    {
        Title = 0,
        PaletteColor = 1,
        ClusterId = 2,
        ClusterColor = 3
    }

    public struct PaletteItem
    {
        public int Id;
        public string Title;
        public Color[] Colors;
        public int Cluster;

        public PaletteItem(int id, string title, int cluster, Color[] colors, int count)
        {
            this.Id = id;
            this.Title = title;
            this.Cluster = cluster;
            if (colors == null)
            {
                this.Colors = null;
            }
            else
            {
                this.Colors = new Color[count];
                for (int i = 0; i < this.Colors.Length; i++)
                    this.Colors[i] = colors[i];
            }
        }
        public static int Compare(PaletteItem x, PaletteItem y)
        {
            int r = String.Compare(x.Title, y.Title, true);
            if (r == 0)
                r = x.Id.CompareTo(y.Id);
            return r;
        }
    }
}