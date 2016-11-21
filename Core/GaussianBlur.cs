using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Colorissimo.Core
{

    public enum BlurType
    {
        Both,
        HorizontalOnly,
        VerticalOnly,
    }

    sealed class GaussianBlur
    {
        private int _radius = 1;
        private int[] _kernel;
        private int _kernelSum;
        private int[,] _multable;
        private BlurType _blurType;

        public GaussianBlur()
        {
            PreCalculateSomeStuff();
        }

        public GaussianBlur(int radius)
        {
            _radius = radius;
            PreCalculateSomeStuff();
        }

        private void PreCalculateSomeStuff()
        {
            int sz = _radius * 2 + 1;
            _kernel = new int[sz];
            _multable = new int[sz, 256];
            for (int i = 1; i <= _radius; i++)
            {
                int szi = _radius - i;
                int szj = _radius + i;
                _kernel[szj] = _kernel[szi] = (szi + 1) * (szi + 1);
                _kernelSum += (_kernel[szj] + _kernel[szi]);
                for (int j = 0; j < 256; j++)
                {
                    _multable[szj, j] = _multable[szi, j] = _kernel[szj] * j;
                }
            }
            _kernel[_radius] = (_radius + 1) * (_radius + 1);
            _kernelSum += _kernel[_radius];
            for (int j = 0; j < 256; j++)
            {
                _multable[_radius, j] = _kernel[_radius] * j;
            }
        }

        public long t1 = 0;
        public long t2 = 0;
        public long t3 = 0;
        public long t4 = 0;


        public RawImage ProcessImage(RawImage origin)
        {

            int pixelCount = origin.Width * origin.Height;
            int[] blurred = new int[pixelCount];

            int[] b = new int[pixelCount];
            int[] g = new int[pixelCount];
            int[] r = new int[pixelCount];

            int[] b2 = new int[pixelCount];
            int[] g2 = new int[pixelCount];
            int[] r2 = new int[pixelCount];

            for (int i = 0; i < pixelCount; i++)
            {
                int rgb = origin.Pixels[i];
                b[i] = (int)(rgb & 0xff);
                g[i] = (int)((rgb >> 8) & 0xff);
                r[i] = (int)((rgb >> 16) & 0xff);
            }

            int bsum;
            int gsum;
            int rsum;
            int read;
            int start = 0;
            int index = 0;
            int p;

            if (_blurType != BlurType.VerticalOnly)
            {
                p = 0;
                for (int i = 0; i < origin.Height; i++)
                {
                    for (int j = 0; j < origin.Width; j++)
                    {
                        bsum = gsum = rsum = 0;
                        read = index - _radius;

                        for (int z = 0; z < _kernel.Length; z++)
                        {
                            if (read < start)
                            {
                                bsum += _multable[z, b[start]];
                                gsum += _multable[z, g[start]];
                                rsum += _multable[z, r[start]];
                            }
                            else if (read > start + origin.Width - 1)
                            {
                                int idx = start + origin.Width - 1;
                                bsum += _multable[z, b[idx]];
                                gsum += _multable[z, g[idx]];
                                rsum += _multable[z, r[idx]];
                            }
                            else
                            {
                                bsum += _multable[z, b[read]];
                                gsum += _multable[z, g[read]];
                                rsum += _multable[z, r[read]];
                            }
                            ++read;
                        }

                        b2[index] = (bsum / _kernelSum);
                        g2[index] = (gsum / _kernelSum);
                        r2[index] = (rsum / _kernelSum);

                        if (_blurType == BlurType.HorizontalOnly)
                        {
                            blurred[p++] = GaussianBlur.ToRgb((byte)(rsum / _kernelSum), (byte)(gsum / _kernelSum), (byte)(bsum / _kernelSum));
                        }

                        ++index;
                    }
                    start += origin.Width;
                }
            }
            if (_blurType == BlurType.HorizontalOnly)
                return new RawImage(origin.Width, origin.Height, blurred);

            int tempy;
            p = 0;
            for (int i = 0; i < origin.Height; i++)
            {
                int y = i - _radius;
                start = y * origin.Width;
                for (int j = 0; j < origin.Width; j++)
                {
                    bsum = gsum = rsum = 0;
                    read = start + j;
                    tempy = y;
                    for (int z = 0; z < _kernel.Length; z++)
                    {
                        if (_blurType == BlurType.VerticalOnly)
                        {
                            if (tempy < 0)
                            {
                                bsum += _multable[z, b[j]];
                                gsum += _multable[z, g[j]];
                                rsum += _multable[z, r[j]];
                            }
                            else if (tempy > origin.Height - 1)
                            {
                                int idx = pixelCount - (origin.Width - j);
                                bsum += _multable[z, b[idx]];
                                gsum += _multable[z, g[idx]];
                                rsum += _multable[z, r[idx]];
                            }
                            else
                            {
                                bsum += _multable[z, b[read]];
                                gsum += _multable[z, g[read]];
                                rsum += _multable[z, r[read]];
                            }
                        }
                        else
                        {
                            if (tempy < 0)
                            {
                                bsum += _multable[z, b2[j]];
                                gsum += _multable[z, g2[j]];
                                rsum += _multable[z, r2[j]];
                            }
                            else if (tempy > origin.Height - 1)
                            {
                                int idx = pixelCount - (origin.Width - j);
                                bsum += _multable[z, b2[idx]];
                                gsum += _multable[z, g2[idx]];
                                rsum += _multable[z, r2[idx]];
                            }
                            else
                            {
                                bsum += _multable[z, b2[read]];
                                gsum += _multable[z, g2[read]];
                                rsum += _multable[z, r2[read]];
                            }
                        }


                        read += origin.Width;
                        ++tempy;
                    }

                    blurred[p++] = GaussianBlur.ToRgb((byte)(rsum / _kernelSum), (byte)(gsum / _kernelSum), (byte)(bsum / _kernelSum));
                }
            }

            return new RawImage(origin.Width, origin.Height, blurred);
        }

        private static int ToRgb(byte r, byte g, byte b)
        {
            return ((int)r << 16) | ((int)g << 8) | (int)b;
        }

        public int Radius
        {
            get { return _radius; }
            set
            {
                if (value < 1)
                {
                    throw new InvalidOperationException("Radius must be greater then 0");
                }
                _radius = value;
                PreCalculateSomeStuff();
            }
        }

        public BlurType BlurType
        {
            get { return _blurType; }
            set
            {
                _blurType = value;
            }
        }
    }
}