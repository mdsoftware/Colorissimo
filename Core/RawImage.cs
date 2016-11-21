using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Colorissimo.Core
{

    public sealed class RawImage
    {
        public int Width;
        public int Height;
        public int[] Pixels;

        public RawImage(int width, int height, int[] pixels)
        {
            this.Width = width;
            this.Height = height;
            this.Pixels = pixels;
        }

        public void Clear()
        {
            this.Width = 0;
            this.Height = 0;
            this.Pixels = null;
        }

        public static Color3[] ToColor3Array(int[] colors, IColor3Transform transform, int count)
        {
            int l = (count <= 0) ? Int32.MaxValue : count;
            if (l > colors.Length) l = colors.Length;
            Color3[] a = new Color3[l];
            for (int i = 0; i < l; i++)
            {
                int c = colors[i];
                a[i] = transform.ToColor3(Color.FromArgb((c >> 16) & 0xff, (c >> 8) & 0xff, c & 0xff));
            }
            return a;
        }

        public static Color[] FromColor3Array(Color3[] colors, IColor3Transform transform)
        {
            Color[] a = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                a[i] = transform.FromColor3(colors[i]);
            return a;
        }

        public int[] Vector(int countLimit)
        {
            SortedDictionary<int, int> index = new SortedDictionary<int, int>();
            for (int i = 0; i < this.Pixels.Length; i++)
            {
                int c = this.Pixels[i];
                if (index.ContainsKey(c))
                {
                    index[c]++;
                }
                else
                {
                    index.Add(c, 1);
                }
            }
            List<VectorColor> l = new List<VectorColor>();
            foreach (KeyValuePair<int, int> p in index)
            {
                if (p.Value < countLimit)
                    continue;
                l.Add(new VectorColor(p.Key, p.Value));
            }
            index = null;
            if (l.Count == 0)
                return null;
            l.Sort(RawImage.CompareVectorColors);
            int[] v = new int[l.Count];
            for (int i = 0; i < l.Count; i++)
                v[i] = l[i].Color;
            l = null;
            return v;
        }

        private static int CompareVectorColors(VectorColor x, VectorColor y)
        {
            return y.Count.CompareTo(x.Count);
        }

        public static byte[] Jpeg(Bitmap bmp, long quality)
        {
            if (bmp == null)
                return null;
            ImageCodecInfo codecInfo = RawImage.GetEncoder("image/jpeg");
            Stream stream = null;
            if (codecInfo == null)
            {
                stream = new MemoryStream();
                bmp.Save(stream, ImageFormat.Jpeg);
            }
            else
            {
                System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
                System.Drawing.Imaging.EncoderParameters paramList = new EncoderParameters(1);
                paramList.Param[0] = new System.Drawing.Imaging.EncoderParameter(encoder, quality);
                ImageFormat fmt = ImageFormat.Jpeg;
                stream = new MemoryStream();
                bmp.Save(stream, codecInfo, paramList);
            }
            bmp.Dispose();
            bmp = null;
            if (stream == null)
                return null;
            byte[] b = null;
            if (stream.Length > 0)
            {
                b = new byte[stream.Length];
                stream.Position = 0;
                stream.Read(b, 0, b.Length);
            }
            stream.Close();
            stream = null;
            return b;
        }

        public byte[] Jpeg(long quality)
        {
            return RawImage.Jpeg(this.GetBitmap(), quality);
        }

        private static ImageCodecInfo GetEncoder(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        public RawImage ResizeTo(int maxDimension)
        {
            if ((this.Width <= maxDimension) && (this.Height <= maxDimension))
                return this;

            double scaleX = (double)this.Width / (double)maxDimension;
            double scaleY = (double)this.Height / (double)maxDimension;
            scaleX = scaleX > scaleY ? scaleX : scaleY;

            int w = (int)((double)this.Width / scaleX);
            int h = (int)((double)this.Height / scaleX);

            scaleX = (double)this.Width / (double)(w + 1);
            int[] mapX = new int[w + 1];
            for (int i = 0; i <= w; i++)
                mapX[i] = (int)(scaleX * (double)i);
            mapX[w] = this.Width;

            scaleY = (double)this.Height / (double)(h + 1);
            int[] mapY = new int[h + 1];
            for (int i = 0; i <= h; i++)
                mapY[i] = (int)(scaleY * (double)i);
            mapY[h] = this.Height;

            int p = 0;
            double l;
            double a;
            double b;
            int[] dest = new int[w * h];

            for (int i = 0; i < h; i++)
            {
                int y0 = mapY[i];
                int y1 = mapY[i + 1];
                for (int j = 0; j < w; j++)
                {
                    int x0 = mapX[j];
                    int x1 = mapX[j + 1];
                    int count = 0;
                    l = a = b = 0f;

                    for (int ii = y0; ii < y1; ii++)
                        for (int jj = x0; jj < x1; jj++)
                        {
                            int c = this.Pixels[(ii * this.Width) + jj];
                            LabColor lab = ColorTransform.RgbToLab(Color.FromArgb(c));
                            l += lab.L;
                            a += lab.A;
                            b += lab.B;
                            ++count;
                        }

                    if (count > 0)
                    {
                        dest[p] = ColorTransform.LabToRgb(new LabColor(
                            l / (double)count,
                            a / (double)count,
                            b / (double)count)).ToArgb() & 0xffffff;
                    }
                    ++p;
                }
            }

            return new RawImage(w, h, dest);

        }

        public RawImage Resample(int factor)
        {
            switch (factor)
            {
                case 2:
                case 3:
                case 4:
                    break;

                default:
                    throw new ArgumentException("Invalid resample factor");
            }

            int w = this.Width / factor;
            int h = this.Height / factor;

            int[] dest = new int[w * h];

            int p = 0;
            int[] pp = new int[4];
            double l;
            double a;
            double b;
            for (int i = 0; i < h; i++)
            {
                int x = (i * factor) * this.Width;
                for (int k = 0; k < factor; k++)
                {
                    pp[k] = x;
                    x += this.Width;
                }
                for (int j = 0; j < w; j++)
                {
                    int count = 0;
                    l = a = b = 0f;
                    for (int k = 0; k < factor; k++)
                        for (int m = 0; m < factor; m++)
                        {
                            LabColor lab = ColorTransform.RgbToLab(Color.FromArgb(this.Pixels[pp[k]]));
                            l += lab.L;
                            a += lab.A;
                            b += lab.B;
                            pp[k]++;
                            ++count;
                        }

                    dest[p++] = ColorTransform.LabToRgb(new LabColor(
                        l / (double)count,
                        a / (double)count,
                        b / (double)count)).ToArgb() & 0xffffff;
                }
            }
            return new RawImage(w, h, dest);
        }

        public RawImage Blur(int radius)
        {
            GaussianBlur blur = new GaussianBlur();
            blur.Radius = radius;
            RawImage blurred = blur.ProcessImage(this);
            blur = null;
            return blurred;
        }

        public Bitmap GetBitmap()
        {
            Bitmap bmp = new Bitmap(this.Width, this.Height);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, this.Width, this.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            unsafe
            {
                int p = 0;
                byte* ptr = (byte*)data.Scan0;
                for (int i = 0; i < this.Height; i++)
                {
                    int* ptr0 = (int*)ptr;
                    for (int j = 0; j < this.Width; j++)
                    {
                        *ptr0 = (int)((uint)this.Pixels[p++] | (uint)0xff000000);
                        ptr0++;
                    }
                    ptr += data.Stride;
                }
            }

            bmp.UnlockBits(data);
            return bmp;
        }

        public static RawImage Load(byte[] buffer)
        {
            Stream stream = new MemoryStream(buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
            stream.Position = 0;
            Bitmap bmp = new Bitmap(stream);
            stream.Dispose();
            stream = null;
            RawImage r = RawImage.Load(bmp);
            bmp.Dispose();
            bmp = null;
            return r;
        }

        public static RawImage Load(Bitmap bmp)
        {
            RawImage raw = new RawImage(0, 0, null);

            raw.Width = bmp.Width;
            raw.Height = bmp.Height;
            raw.Pixels = new int[raw.Width * raw.Height];

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, raw.Width, raw.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

            unsafe
            {
                int p = 0;
                byte* ptr = (byte*)data.Scan0;
                for (int i = 0; i < raw.Height; i++)
                {
                    int* ptr0 = (int*)ptr;
                    for (int j = 0; j < raw.Width; j++)
                    {
                        int c = *(ptr0++);
                        raw.Pixels[p++] = c & 0xffffff;
                    }
                    ptr += data.Stride;
                }
            }

            bmp.UnlockBits(data);

            bmp.Dispose();
            bmp = null;

            return raw;
        }
    }

    sealed class VectorColor
    {
        public int Color;
        public int Count;

        public VectorColor(int color, int count)
        {
            this.Color = color;
            this.Count = count;
        }
    }
}