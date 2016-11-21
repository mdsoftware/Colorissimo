using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data.SqlClient;
using System.Drawing;
using Colorissimo.Core.Clustering;

namespace Colorissimo.Core
{


    public sealed class PaletteManager : IDisposable, IColor3Transform
    {
        private string connStr;
        private FindColorComparer comparer;

        public PaletteManager(string connStr)
        {
            this.connStr = connStr;
            this.comparer = new FindColorComparer();
        }

        private SqlConnection GetConnection()
        {
            SqlConnection conn = new SqlConnection(this.connStr);
            conn.Open();
            return conn;
        }

        public Color3 ToColor3(Color color)
        {
            return new Color3(ColorTransform.RgbToLab(color));
        }

        public Color FromColor3(Color3 color)
        {
            return ColorTransform.LabToRgb(new LabColor(color.A, color.B, color.C));
        }

        public static void Sort(List<PaletteItem> list, PaletteListSortMode mode, SortedDictionary<int, PaletteItem> clusters)
        {
            PaletteSortContext.Sort(list, mode, clusters);
        }

        public List<PaletteItem> Find(Color color1, Color color2, ColorSearchWidth width, bool addSimilar, int maxCount)
        {
            int mask;
            SortedDictionary<int, int> colors = this.FindColors(color1, color2, width, out mask);
            if (colors.Count == 0) return null;
            return this.LoadForColors(colors, mask, addSimilar, maxCount);
        }

        public Color[] FindColor(Color color, ColorSearchWidth width)
        {
            SortedDictionary<int, int> l = new SortedDictionary<int, int>();
            using (SqlConnection conn = this.GetConnection())
            {
                this.FindAddColors(conn, l, color.ToArgb() & 0xffffff, width, 0x1);
            }
            if (l.Count == 0) return null;
            Color[] r = new Color[l.Count];
            int i = 0;
            foreach (KeyValuePair<int, int> p in l)
            {
                r[i++] = PaletteManager.ColorFromInt(p.Key);
            }
            l = null;
            return r;
        }

        public static Color FindLabNearest(Color color, Color[] colors)
        {
            if (colors == null) return color;
            if (colors.Length == 0) return color;
            Color c = colors[0];
            double diff = ColorTransform.LabDistance(color, c);
            for (int i = 1; i < colors.Length; i++)
            {
                double d = ColorTransform.LabDistance(color, colors[i]);
                if (d < diff)
                {
                    diff = d;
                    c = colors[i];
                }
            }
            return c;
        }

        public static Color[] LabDistanceSort(Color[] colors, Color color)
        {
            if (colors == null) return colors;
            if (colors.Length == 0) return colors;
            SortedDictionary<ColorIndex, Color> l = new SortedDictionary<ColorIndex, Color>();
            int i;
            for (i = 0; i < colors.Length; i++)
                l.Add(new ColorIndex(ColorTransform.LabDistance(color, colors[i]), i), colors[i]);
            Color[] r = new Color[colors.Length];
            i = 0;
            foreach (KeyValuePair<ColorIndex, Color> p in l)
                r[i++] = p.Value;
            return r;
        }

        struct ColorIndex : IComparable<ColorIndex>
        {
            public double Dist;
            public int Index;

            public ColorIndex(double dist, int index)
            {
                this.Dist = dist;
                this.Index = index;
            }

            public int CompareTo(ColorIndex y)
            {
                int r = this.Dist.CompareTo(y.Dist);
                if (r == 0) r = this.Index.CompareTo(y.Index);
                return r;
            }
        }

        public PaletteItem Load(int paletteId)
        {
            PaletteItem pi = new PaletteItem();
            pi.Id = -1;
            using (SqlConnection conn = this.GetConnection())
            {
                string sql = @"SELECT p.ID, p.[DESCRIPTION], c.RGB, p.CLUSTER
FROM PALETTE_COLOR c, PALETTE p WHERE (p.ID = c.PALETTE_ID) AND (p.ID = " + paletteId.ToString() + @") ORDER BY p.ID, c.[ORDER]";

                SqlCommand cmd = new SqlCommand(sql, conn);

                SqlDataReader r = cmd.ExecuteReader();

                Color[] colors = new Color[32];
                int p = 0;
                while (r.Read())
                {
                    int x = r.GetInt32(0);
                    if (x != pi.Id)
                    {
                        pi.Id = x;
                        p = 0;
                        pi.Title = r.GetString(1);
                        if (r.IsDBNull(3))
                        {
                            pi.Cluster = 0;
                        }
                        else
                        {
                            pi.Cluster = r.GetInt32(3);
                        }
                    }
                    colors[p++] = PaletteManager.ColorFromInt(r.GetInt32(2));
                }
                r.Close();
                if (pi.Id > 0)
                    pi = new PaletteItem(pi.Id, pi.Title, pi.Cluster, colors, p);
            }
            return pi;
        }

        private SortedDictionary<int, int> FindColors(Color color1, Color color2, ColorSearchWidth width, out int mask)
        {
            mask = 0;
            SortedDictionary<int, int> l = new SortedDictionary<int,int>();
            using (SqlConnection conn = this.GetConnection())
            {
                if (color1 != Color.Transparent)
                {
                    this.FindAddColors(conn, l, color1.ToArgb() & 0xffffff, width, 0x1);
                    mask |= 0x1;
                }
                if (color2 != Color.Transparent)
                {
                    this.FindAddColors(conn, l, color2.ToArgb() & 0xffffff, width, 0x2);
                    mask |= 0x2;
                }
            }
            return l;
        }

        private void FindAddColors(SqlConnection conn, SortedDictionary<int, int> list, int color, ColorSearchWidth width, int mask)
        {
            LabColor lab;
            string sql = PaletteManager.ColorSearchSql(color, width, out lab);
            SqlCommand cmd = new SqlCommand(sql, conn);
            SqlDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                int c = r.GetInt32(0);
                if (!list.ContainsKey(c))
                {
                    list.Add(c, mask);
                }
                else
                {
                    list[c] |= mask;
                }
            }
            r.Close();
            r = null;
            cmd = null;
        }

        private List<PaletteItem> LoadForColors(SortedDictionary<int, int> colors, int mask, bool addSimilar, int maxCount)
        {
            List<PaletteItem> l = null;
            using (SqlConnection conn = this.GetConnection())
            {
                StringBuilder sb = new StringBuilder(1024);
                SortedDictionary<int, PaletteItem> list = new SortedDictionary<int, PaletteItem>();
                int count = 0;
                foreach (KeyValuePair<int, int> c in colors)
                {
                    if (sb.Length > 0) sb.Append(',');
                    sb.Append(c.Key.ToString());
                    if (sb.Length < 512)
                        continue;
                    this.AddForColors(conn, list, sb.ToString(), colors, mask, maxCount, ref count);
                    if (count >= maxCount) break;
                    sb.Length = 0;
                }
                if ((sb.Length > 0) && (count < maxCount))
                    this.AddForColors(conn, list, sb.ToString(), colors, mask, maxCount, ref count);
                sb = null;

                if (addSimilar)
                {
                    SortedDictionary<int, int> clusters = new SortedDictionary<int, int>();
                    sb = new StringBuilder();
                    int cnt = 0;
                    foreach (KeyValuePair<int, PaletteItem> pp in list)
                    {
                        int c = pp.Value.Cluster;
                        if (c < 0)
                            if (!clusters.ContainsKey(c))
                            {
                                clusters.Add(c, c);
                                if (cnt++ > 0) sb.Append(',');
                                sb.Append(c.ToString());
                            }
                    }
                    if (cnt > 0)
                        this.AddForClusters(conn, list, sb.ToString(), maxCount, ref count);
                    sb = null;
                }

                if (list.Count > 0)
                {
                    l = new List<PaletteItem>(list.Count);
                    foreach (KeyValuePair<int, PaletteItem> pp in list)
                        l.Add(pp.Value);
                }
            }
            return l;
        }

        private static bool MatchMask(Color[] colors, SortedDictionary<int, int> colorIndex, int mask)
        {
            if (colorIndex == null) return true;
            int m = 0;

            for (int i = 0; i < colors.Length; i++)
            {
                int c = colors[i].ToArgb() & 0xffffff;
                if (colorIndex.ContainsKey(c))
                    m |= colorIndex[c];
            }

            return (m == mask);
        }

        private void AddForClusters(SqlConnection conn, SortedDictionary<int, PaletteItem> list, string clusters, int maxCount, ref int count)
        {
            string sql = @"SELECT p.ID, p.[DESCRIPTION], c.RGB, p.CLUSTER
FROM PALETTE_COLOR c, PALETTE p WHERE (p.ID = c.PALETTE_ID) AND (p.CLUSTER IN (" +
clusters + @")) ORDER BY p.ID, c.[ORDER]";

            SqlCommand cmd = new SqlCommand(sql, conn);

            SqlDataReader r = cmd.ExecuteReader();

            int id = -1;
            string title = null;
            int cluster = -1;
            Color[] colors = new Color[32];
            int p = 0;
            bool add = false;
            while (r.Read())
            {
                int x = r.GetInt32(0);
                if (x != id)
                {
                    if (title != null)
                    {
                        if (add)
                        {
                            list.Add(id, new PaletteItem(id, title, cluster, colors, p));
                            ++count;
                            title = null;
                            if (count >= maxCount) break;
                        }
                    }
                    id = x;
                    if (list.ContainsKey(id))
                    {
                        add = false;
                        p = 0;
                        title = null;
                    }
                    else
                    {
                        add = true;
                        p = 0;
                        title = r.GetString(1);
                        if (r.IsDBNull(3))
                        {
                            cluster = 0;
                        }
                        else
                        {
                            cluster = r.GetInt32(3);
                        }
                    }
                }
                if (add)
                    colors[p++] = PaletteManager.ColorFromInt(r.GetInt32(2));
            }
            if (title != null)
            {
                if (add)
                {
                    if (count < maxCount)
                        list.Add(id, new PaletteItem(id, title, cluster, colors, p));
                }
            }

            r.Close();
            r = null;
        }

        private void AddForColors(SqlConnection conn, SortedDictionary<int, PaletteItem> list, string colorList, SortedDictionary<int, int> colorIndex, int mask, int maxCount, ref int count)
        {

            string sql = @"SELECT p.ID, p.[DESCRIPTION], c.RGB, p.CLUSTER
FROM PALETTE_COLOR c, PALETTE p
WHERE (p.ID = c.PALETTE_ID) AND
(p.ID IN (SELECT DISTINCT pc.PALETTE_ID FROM PALETTE_COLOR pc WHERE (pc.RGB IN ("
+ colorList + 
@")))) ORDER BY p.ID, c.[ORDER]";

            SqlCommand cmd = new SqlCommand(sql, conn);

            SqlDataReader r = cmd.ExecuteReader();

            int id = -1;
            string title = null;
            int cluster = -1;
            Color[] colors = new Color[32];
            int p = 0;
            bool add = false;
            while (r.Read())
            {
                int x = r.GetInt32(0);
                if (x != id)
                {
                    if (title != null)
                    {
                        if (add)
                        {
                            PaletteItem pal = new PaletteItem(id, title, cluster, colors, p);
                            if (PaletteManager.MatchMask(pal.Colors, colorIndex, mask))
                            {
                                list.Add(id, pal);
                                ++count;
                                if (count >= maxCount) break;
                            }
                        }
                    }
                    id = x;
                    if (list.ContainsKey(id))
                    {
                        add = false;
                        p = 0;
                        title = null;
                    }
                    else
                    {
                        add = true;
                        p = 0;
                        title = r.GetString(1);
                        if (r.IsDBNull(3))
                        {
                            cluster = 0;
                        }
                        else
                        {
                            cluster = r.GetInt32(3);
                        }
                    }
                }
                if (add)
                    colors[p++] = PaletteManager.ColorFromInt(r.GetInt32(2));
            }
            if (title != null)
            {
                if (add && (count < maxCount))
                {
                    PaletteItem pal = new PaletteItem(id, title, cluster, colors, p);
                    if (PaletteManager.MatchMask(pal.Colors, colorIndex, mask))
                    {
                        list.Add(id, pal);
                        ++count;
                    }
                }
            }

            r.Close();
            r = null;
        }

        public SortedDictionary<int, PaletteItem> LoadClusters()
        {
            SortedDictionary<int, PaletteItem> clusters = null;
            using (SqlConnection conn = this.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(@"SELECT c.PALETTE_ID, c.RGB FROM PALETTE_COLOR c WHERE c.PALETTE_ID < 0
ORDER BY c.PALETTE_ID, c.[ORDER]", conn);

                SqlDataReader r = cmd.ExecuteReader();

                int id = 1;
                Color[] colors = new Color[32];
                int p = 0;
                while (r.Read())
                {
                    int x = r.GetInt32(0);
                    if (x != id)
                    {
                        if (id < 0)
                        {
                            if (clusters == null) 
                                clusters = new SortedDictionary<int, PaletteItem>();
                            clusters.Add(id, new PaletteItem(id, null, id, colors, p));
                        }
                        id = x;
                        p = 0;
                    }
                    colors[p++] = PaletteManager.ColorFromInt(r.GetInt32(1));
                }
                if (id < 0)
                {
                    if (clusters == null)
                        clusters = new SortedDictionary<int, PaletteItem>();
                    clusters.Add(id, new PaletteItem(id, null, id, colors, p));
                }

                r.Close();
                r = null;
            }
            return clusters;
        }

        public int AddColor(Color color)
        {
            int rgb = (color.ToArgb() & 0xffffff);
            LabColor lab = ColorTransform.RgbToLab(color);

            using (SqlConnection conn = this.GetConnection())
            {

                SqlCommand cmd = new SqlCommand("UP_ADD_COLOR", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.Add("@rgb", System.Data.SqlDbType.Int).Value = rgb;
                cmd.Parameters.Add("@l", System.Data.SqlDbType.Float).Value = Math.Round(lab.L, 6);
                cmd.Parameters.Add("@a", System.Data.SqlDbType.Float).Value = Math.Round(lab.A, 6);
                cmd.Parameters.Add("@b", System.Data.SqlDbType.Float).Value = Math.Round(lab.B, 6);
                cmd.Parameters.Add("@l_hash", System.Data.SqlDbType.Int).Value = lab.HashL;
                cmd.Parameters.Add("@a_hash", System.Data.SqlDbType.Int).Value = lab.HashA;
                cmd.Parameters.Add("@b_hash", System.Data.SqlDbType.Int).Value = lab.HashB;

                cmd.ExecuteNonQuery();

                cmd = null;
            }

            return rgb;
        }

        public static Color ColorFromInt(int x)
        {
            return Color.FromArgb((byte)((x >> 16) & 0xff), (byte)((x >> 8) & 0xff), (byte)(x & 0xff));
        }

        private static string ColorSearchSql(int color, ColorSearchWidth width, out LabColor lab)
        {
            lab = ColorTransform.RgbToLab(PaletteManager.ColorFromInt(color));

            StringBuilder sb = new StringBuilder();

            int w = (int)width;

            sb.Append(@"SELECT c.RGB, c.L, c.A, c.B
FROM COLOR c, COLOR_INDEX_L l, COLOR_INDEX_A a, COLOR_INDEX_B b
WHERE (l.RGB = c.RGB) AND (a.RGB = c.RGB) AND (b.RGB = l.RGB)");

            sb.Append("AND (l.[HASH] ");
            int h = lab.HashL;
            if (w > 0)
            {
                sb.Append("IN (");

                int c = 0;
                for (int i = -w; i <= w; i++)
                {
                    if (c++ > 0) sb.Append(',');
                    sb.Append((h + i).ToString());
                }

                sb.Append(')');
            }
            else
            {
                sb.Append("= ");
                sb.Append(h.ToString());
            }
            sb.Append(')');

            sb.Append(" AND (a.[HASH] ");
            h = lab.HashA;
            if (w > 0)
            {
                sb.Append("IN (");
                int c = 0;
                for (int i = -w; i <= w; i++)
                {
                    if (c++ > 0) sb.Append(',');
                    sb.Append((h + i).ToString());
                }

                sb.Append(')');
            }
            else
            {
                sb.Append("= ");
                sb.Append(h.ToString());
            }
            sb.Append(')');

            sb.Append(" AND (b.[HASH] ");
            h = lab.HashB;
            if (w > 0)
            {
                sb.Append("IN (");
                int c = 0;
                for (int i = -w; i <= w; i++)
                {
                    if (c++ > 0) sb.Append(',');
                    sb.Append((h + i).ToString());
                }

                sb.Append(')');
            }
            else
            {
                sb.Append("= ");
                sb.Append(h.ToString());
            }
            sb.Append(')');

            return sb.ToString();
        }

        public int[] Find(int color, int count)
        {
            
            List<FindColorInfo> list = new List<FindColorInfo>();

            using (SqlConnection conn = this.GetConnection())
            {
                LabColor lab;
                SqlCommand cmd = new SqlCommand(PaletteManager.ColorSearchSql(color, ColorSearchWidth.Narrow, out lab), conn);
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new FindColorInfo((int)r[0],
                        lab.Distance(new LabColor((float)(double)r[1], (float)(double)r[2], (float)(double)r[3]))));
                r.Close();
                r = null;
                cmd = null;
            }

            if (list.Count == 0)
                return null;

            list.Sort(this.comparer);

            int l = count;
            if (l > list.Count) l = count;

            int[] found = new int[l];
            for (int i = 0; i < l; i++)
                found[i] = list[i].Rgb;

            list = null;

            return found;
        }

        public int AddPalette(string description)
        {
            int id = 0;
            using (SqlConnection conn = this.GetConnection())
            {

                SqlCommand cmd = new SqlCommand("UP_ADD_PALETTE", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.Add("@desc", System.Data.SqlDbType.NVarChar, 2048).Value = description;
                SqlParameter p = cmd.Parameters.Add("@id", System.Data.SqlDbType.Int);
                p.Direction = System.Data.ParameterDirection.Output;

                cmd.ExecuteNonQuery();

                id = (int)cmd.Parameters["@id"].Value;

                cmd = null;
            }
            return id;
        }

        public void UpdatePaletteClusters(ClusteringPoint[] clusters, ClusteringPoint[] points)
        {
            if (clusters == null)
                return;

            using (SqlConnection conn = this.GetConnection())
            {

                SqlCommand cmd = new SqlCommand(@"DELETE FROM PALETTE_COLOR WHERE PALETTE_ID < 0", conn);
                cmd.ExecuteNonQuery();
                cmd = null;

                cmd = new SqlCommand(@"UPDATE PALETTE SET CLUSTER = NULL", conn);
                cmd.ExecuteNonQuery();
                cmd = null;

                for (int i = 0; i < clusters.Length; i++)
                {
                    ClusteringPoint p = clusters[i];
                    int j = 0;
                    int order = 0;
                    while (j < p.Size)
                    {
                        int rgb = ColorTransform.LabToRgb(new LabColor(p[j++], p[j++], p[j++])).ToArgb() & 0xffffff;
                        ++order;
                        cmd = new SqlCommand(@"INSERT INTO [PALETTE_COLOR] ([PALETTE_ID],[ORDER],[RGB]) VALUES
(-" + (i + 1).ToString() + "," + order.ToString() + "," + rgb.ToString() + ")", conn);
                        cmd.ExecuteNonQuery();
                        cmd = null;
                    }
                }

                for (int i = 0; i < points.Length; i++)
                {
                    int id = points[i].Id;
                    int cluster = -(points[i].Index + 1);
                    cmd = new SqlCommand(@"UPDATE PALETTE SET CLUSTER = " + cluster.ToString() + " WHERE ID = " + id.ToString(), conn);
                    cmd.ExecuteNonQuery();
                    cmd = null;
                }
            }
        }

        public ClusteringPoint[] LoadPaletteColors()
        {
            List<ClusteringPoint> l = null;
            using (SqlConnection conn = this.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(@"SELECT pc.PALETTE_ID, c.L, c.A, c.B
FROM PALETTE_COLOR pc, COLOR c WHERE (c.RGB = pc.RGB) ORDER BY pc.PALETTE_ID, pc.[ORDER]", conn);
                SqlDataReader r = cmd.ExecuteReader();

                double[] values = new double[64];
                int id = -1;
                int p = 0;
                l = new List<ClusteringPoint>();
                while (r.Read())
                {
                    int x = r.GetInt32(0);
                    if (x != id)
                    {
                        if (id > 0)
                            l.Add(new ClusteringPoint(id, values, p));
                        id = x;
                        p = 0;
                    }
                    values[p++] = r.GetDouble(1);
                    if (p >= values.Length) break;
                    values[p++] = r.GetDouble(2);
                    if (p >= values.Length) break;
                    values[p++] = r.GetDouble(3);
                    if (p >= values.Length) break;
                }
                l.Add(new ClusteringPoint(id, values, p));
                r.Close();
                r = null;
                cmd = null;
            }
            if (l == null) return null;
            if (l.Count == 0) return null;
            return l.ToArray();
        }

        public const int PositionMarkSize = 8;

        public static byte[] GetPaletteHlsPosition(int paletteId, int size)
        {
            int count = (paletteId >> 8) & 0xff;
            if (count < 2) return null;
            int pos = paletteId & 0xff;
            if (pos >= count) pos = 0;

            Bitmap bmp = new Bitmap(size, size);
            Graphics gfx = Graphics.FromImage(bmp);
            gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            gfx.FillRectangle(Brushes.White, 0, 0, size, size);

            int r = ((size - PaletteManager.PositionMarkSize) >> 1) - 2;
            double angle = 0f;
            double step = (2 * Math.PI) / (double)count;

            int x = (size >> 1) + (int)(Math.Sin(angle) * (double)r);
            int y = (size >> 1) - (int)(Math.Cos(angle) * (double)r);

            Pen pen = new Pen(Color.Black);

            for (int i = 0; i < count; i++)
            {
                angle += step;
                int x0 = (size >> 1) + (int)(Math.Sin(angle) * (double)r);
                int y0 = (size >> 1) - (int)(Math.Cos(angle) * (double)r);

                gfx.DrawLine(pen, x, y, x0, y0);
                if (pos == i)
                {
                    gfx.FillEllipse(Brushes.Black,
                        x - (PaletteManager.PositionMarkSize >> 1),
                        y - (PaletteManager.PositionMarkSize >> 1),
                        PaletteManager.PositionMarkSize,
                        PaletteManager.PositionMarkSize);
                }

                x = x0;
                y = y0;
            }

            gfx.Dispose();
            gfx = null;
            byte[] buf = RawImage.Jpeg(bmp, 80);
            bmp.Dispose();
            bmp=null;
            return buf;
        }

        public byte[] GetPaletteThumbnail(int paletteId)
        {
            byte[] image = null;
            using (SqlConnection conn = this.GetConnection())
            {
                MemoryStream stream = null;

                SqlCommand cmd = new SqlCommand(@"SELECT t.DATA FROM PALETTE_THUMBNAIL t WHERE t.PALETTE_ID = " + paletteId.ToString() + " ORDER BY t.[ORDER]", conn);
                SqlDataReader r = cmd.ExecuteReader();

                while (r.Read())
                {
                    byte[] b = r[0] as byte[];
                    if (b == null) continue;
                    if (stream == null) stream = new MemoryStream();
                    stream.Write(b, 0, b.Length);
                }

                r.Close();
                r = null;
                cmd = null;

                if (stream != null)
                {
                    if (stream.Length > 0)
                    {
                        stream.Position = 0;
                        image = new byte[stream.Length];
                        stream.Read(image, 0, image.Length);
                    }
                    stream.Close();
                    stream = null;
                }
            }
            return image;
        }

        public int AddPaletteThumbnail(int paletteId, byte[] image, int pageSize)
        {
            int id = 0;
            using (SqlConnection conn = this.GetConnection())
            {

                SqlCommand cmd = new SqlCommand("UP_ADD_THUMBNAIL", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.Add("@palette_id", System.Data.SqlDbType.Int).Value = paletteId;
                cmd.Parameters.Add("@order", System.Data.SqlDbType.Int);
                cmd.Parameters.Add("@data", System.Data.SqlDbType.Binary, pageSize);

                int order = 0;
                int pos = 0;
                while (pos < image.Length)
                {
                    int l = (image.Length - pos);
                    if (l > pageSize) l = pageSize;

                    cmd.Parameters["@order"].Value = (++order);
                    byte[] b = new byte[l];
                    Buffer.BlockCopy(image, pos, b, 0, l);
                    cmd.Parameters["@data"].Value = b;
                    cmd.ExecuteNonQuery();
                    b = null;
                    pos += l;
                }
                cmd = null;
            }
            return id;
        }

        public void AddPaletteColor(int paletteId, int color, int order, bool last)
        {
            using (SqlConnection conn = this.GetConnection())
            {

                SqlCommand cmd = new SqlCommand("UP_ADD_PALETTE_COLOR", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.Add("@palette_id", System.Data.SqlDbType.Int).Value = paletteId;
                cmd.Parameters.Add("@order", System.Data.SqlDbType.Int).Value = order;
                cmd.Parameters.Add("@rgb", System.Data.SqlDbType.Int).Value = color;
                cmd.Parameters.Add("@clear", System.Data.SqlDbType.Bit).Value = last;

                cmd.ExecuteNonQuery();

                cmd = null;
            }
        }

        public void Dispose()
        {
        }

        private static Color[] LabSort(Color[] colors, Color origin)
        {
            List<LabColor> l = new List<LabColor>(colors.Length);
            for (int i = 0; i < colors.Length; i++)
                l.Add(ColorTransform.RgbToLab(colors[i]));

            l.Sort(new PaletteColorComparer(origin));

            Color[] c = new Color[l.Count];
            for (int i = 0; i < l.Count; i++)
                c[i] = ColorTransform.LabToRgb(l[i]);
            return c;
        }

        public int CreatePalette(int colors, int colorCountLimit, int maxDimension, string title, RawImage image, IClustering clustering, int thumbnailSize)
        {
            RawImage img = image.ResizeTo(maxDimension);
            int[] vector = img.Vector(colorCountLimit);
            clustering.SetPoints(RawImage.ToColor3Array(vector, this, 0));

            Color3[] p = RawImage.ToColor3Array(vector, this, colors);
            for (int j = 0; j < p.Length; j++)
                clustering.AddCluster(p[j]);

            clustering.Start();
            clustering.Run();

            Color[] palette = PaletteManager.LabSort(RawImage.FromColor3Array(clustering.Clusters(colors), this), Color.Black);

            int id = this.AddPalette(title);
            for (int i = 0; i < palette.Length; i++)
            {
                this.AddColor(palette[i]);
                this.AddPaletteColor(id, palette[i].ToArgb() & 0xffffff, i + 1, (i + 1) >= palette.Length);
            }

            if (thumbnailSize > 0)
            {
                img = img.ResizeTo(thumbnailSize);
                this.AddPaletteThumbnail(id, img.Jpeg(80), 2048);
                img.Clear();
                img = null;
            }

            return id;
        }

    }

    sealed class FindColorComparer : IComparer<FindColorInfo>
    {
        public FindColorComparer()
        {
        }

        public int Compare(FindColorInfo x, FindColorInfo y)
        {
            return x.Distance.CompareTo(y.Distance);
        }
    }

    sealed class PaletteColorComparer : IComparer<LabColor>
    {
        private LabColor origin;

        public PaletteColorComparer(Color rgb)
        {
            this.origin = ColorTransform.RgbToLab(rgb);
        }

        public int Compare(LabColor x, LabColor y)
        {
            double dx = x.Distance(this.origin);
            double dy = y.Distance(this.origin);
            return dx.CompareTo(dy);
        }
    }

    struct FindColorInfo
    {
        public int Rgb;
        public double Distance;

        public FindColorInfo(int rgb, double dist)
        {
            this.Rgb = rgb;
            this.Distance = dist;
        }

        public override string ToString()
        {
            return String.Format("0x{0} ({1})", this.Rgb.ToString("x8"), this.Distance);
        }
    }
}