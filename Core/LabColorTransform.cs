using System;
using System.Collections.Generic;
using System.Drawing;

namespace Colorissimo.Core
{

    static class LabColorTransform
    {
        public static double diff(Color color1, Color color2)
        {
            LabColor lab1 = LabColorTransform.RgbToLab(color1);
            LabColor lab2 = LabColorTransform.RgbToLab(color2);

            return Math.Sqrt((lab2.L - lab1.L) * (lab2.L - lab1.L) + (lab2.A - lab1.A) * (lab2.A - lab1.A) + (lab2.B - lab1.B) * (lab2.B - lab1.B));
        }

        private const double ref_X = 95.047f; //ref_X =  95.047     Observer= 2°, Illuminant= D65
        private const double ref_Y = 100.000f; //ref_Y = 100.000
        private const double ref_Z = 108.883f; //ref_Z = 108.883

        public static LabColor RgbToLab(Color rgb)
        {
            return ColorTransform.XyzToLab(ColorTransform.RgbToXyz(rgb));
        }

        public static Color LabToRgb(LabColor lab)
        {
            return ColorTransform.XyzToRgb(ColorTransform.LabToXyz(lab));
        }

        public static XyzColor LabToXyz(LabColor lab)
        {
            double var_Y = (lab.L + 16f) / 116f;
            double var_X = lab.A / 500f + var_Y;
            double var_Z = var_Y - lab.B / 200f;

            if (Math.Pow(var_Y, 3f) > 0.008856f) var_Y = Math.Pow(var_Y, 3f);
            else var_Y = (var_Y - 16f / 116f) / 7.787f;
            if (Math.Pow(var_X, 3f) > 0.008856f) var_X = Math.Pow(var_X, 3f);
            else var_X = (var_X - 16f / 116f) / 7.787f;
            if (Math.Pow(var_Z, 3f) > 0.008856f) var_Z = Math.Pow(var_Z, 3f);
            else var_Z = (var_Z - 16f / 116f) / 7.787f;

            return new XyzColor(ref_X * var_X,     //ref_X =  95.047     Observer= 2°, Illuminant= D65
            ref_Y * var_Y,     //ref_Y = 100.000
            ref_Z * var_Z);     //ref_Z = 108.883
        }

        public static LabColor XyzToLab(XyzColor xyz)
        {
            double var_X = xyz.X / ref_X;          //ref_X =  95.047   Observer= 2°, Illuminant= D65
            double var_Y = xyz.Y / ref_Y;          //ref_Y = 100.000
            double var_Z = xyz.Z / ref_Z;          //ref_Z = 108.883

            if (var_X > 0.008856f) var_X = Math.Pow(var_X, 1f / 3f);
            else var_X = (7.787f * var_X) + (16f / 116f);
            if (var_Y > 0.008856f) var_Y = Math.Pow(var_Y, 1f / 3f);
            else var_Y = (7.787f * var_Y) + (16f / 116f);
            if (var_Z > 0.008856f) var_Z = Math.Pow(var_Z, 1f / 3f);
            else var_Z = (7.787f * var_Z) + (16f / 116f);

            return new LabColor((116f * var_Y) - 16f,
            500f * (var_X - var_Y),
            200f * (var_Y - var_Z));
        }

        public static Color XyzToRgb(XyzColor xyz)
        {
            double var_X = xyz.X / 100;        //X from 0 to  95.047      (Observer = 2°, Illuminant = D65)
            double var_Y = xyz.Y / 100f;        //Y from 0 to 100.000
            double var_Z = xyz.Z / 100f;        //Z from 0 to 108.883

            double var_R = var_X * 3.2406f + var_Y * -1.5372f + var_Z * -0.4986f;
            double var_G = var_X * -0.9689f + var_Y * 1.8758f + var_Z * 0.0415f;
            double var_B = var_X * 0.0557f + var_Y * -0.2040f + var_Z * 1.0570f;

            if (var_R > 0.0031308f) var_R = 1.055f * (Math.Pow(var_R, 1f / 2.4f)) - 0.055f;
            else var_R = 12.92f * var_R;
            if (var_G > 0.0031308f) var_G = 1.055f * (Math.Pow(var_G, 1f / 2.4f)) - 0.055f;
            else var_G = 12.92f * var_G;
            if (var_B > 0.0031308f) var_B = 1.055f * (Math.Pow(var_B, 1f / 2.4f)) - 0.055f;
            else var_B = 12.92f * var_B;

            return Color.FromArgb(
                LabColorTransform.Range((int)Math.Round(var_R * 255f, 0f), 0, 0xff),
                LabColorTransform.Range((int)Math.Round(var_G * 255f, 0f), 0, 0xff),
                LabColorTransform.Range((int)Math.Round(var_B * 255f, 0f), 0, 0xff));
        }

        public static XyzColor RgbToXyz(Color rgb)
        {
            double var_R = ((double)rgb.R / 255f);        //R from 0 to 255
            double var_G = ((double)rgb.G / 255f);        //G from 0 to 255
            double var_B = ((double)rgb.B / 255f);        //B from 0 to 255

            if (var_R > 0.04045f) var_R = Math.Pow(((var_R + 0.055f) / 1.055f), 2.4f);
            else var_R = var_R / 12.92f;
            if (var_G > 0.04045f) var_G = Math.Pow(((var_G + 0.055f) / 1.055f), 2.4f);
            else var_G = var_G / 12.92f;
            if (var_B > 0.04045f) var_B = Math.Pow(((var_B + 0.055f) / 1.055f), 2.4f);
            else var_B = var_B / 12.92f;

            var_R = var_R * 100f;
            var_G = var_G * 100f;
            var_B = var_B * 100f;

            //Observer. = 2°, Illuminant = D65
            return new XyzColor(var_R * 0.4124f + var_G * 0.3576f + var_B * 0.1805f,
            var_R * 0.2126f + var_G * 0.7152f + var_B * 0.0722f,
            var_R * 0.0193f + var_G * 0.1192f + var_B * 0.9505f);
        }

        private static int Range(int v, int min, int max)
        {
            if (v > max) return max;
            if (v < min) return min;
            return v;
        }
    }
}