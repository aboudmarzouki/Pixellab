using System;

namespace PixelLab.Models
{
    public static class ColorConverter
    {
        // ==========================================
        // HSV (Hue, Saturation, Value)
        // ==========================================

        public static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
        {
            double rNorm = r / 255.0;
            double gNorm = g / 255.0;
            double bNorm = b / 255.0;

            double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
            double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
            double delta = max - min;

            double hue = 0;

            if (delta != 0)
            {
                if (max == rNorm) hue = 60 * (((gNorm - bNorm) / delta) % 6);
                else if (max == gNorm) hue = 60 * (((bNorm - rNorm) / delta) + 2);
                else if (max == bNorm) hue = 60 * (((rNorm - gNorm) / delta) + 4);
            }

            if (hue < 0) hue += 360;

            double saturation = (max == 0) ? 0 : (delta / max);
            double value = max;

            return (hue, saturation, value);
        }

        public static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
        {
            double chroma = v * s;
            double x = chroma * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double matchValue = v - chroma;

            double rPrime = 0, gPrime = 0, bPrime = 0;

            if (h >= 0 && h < 60) { rPrime = chroma; gPrime = x; bPrime = 0; }
            else if (h >= 60 && h < 120) { rPrime = x; gPrime = chroma; bPrime = 0; }
            else if (h >= 120 && h < 180) { rPrime = 0; gPrime = chroma; bPrime = x; }
            else if (h >= 180 && h < 240) { rPrime = 0; gPrime = x; bPrime = chroma; }
            else if (h >= 240 && h < 300) { rPrime = x; gPrime = 0; bPrime = chroma; }
            else if (h >= 300 && h <= 360) { rPrime = chroma; gPrime = 0; bPrime = x; }

            byte r = (byte)Math.Clamp((rPrime + matchValue) * 255, 0, 255);
            byte g = (byte)Math.Clamp((gPrime + matchValue) * 255, 0, 255);
            byte b = (byte)Math.Clamp((bPrime + matchValue) * 255, 0, 255);

            return (r, g, b);
        }

        // ==========================================
        // CMYK (Cyan, Magenta, Yellow, Key)
        // ==========================================

        public static (double C, double M, double Y, double K) RgbToCmyk(byte r, byte g, byte b)
        {
            double rNorm = r / 255.0;
            double gNorm = g / 255.0;
            double bNorm = b / 255.0;

            double k = 1.0 - Math.Max(rNorm, Math.Max(gNorm, bNorm));
            if (k >= 1.0) return (0, 0, 0, 1.0);

            double c = (1.0 - rNorm - k) / (1.0 - k);
            double m = (1.0 - gNorm - k) / (1.0 - k);
            double y = (1.0 - bNorm - k) / (1.0 - k);

            return (c, m, y, k);
        }

        public static (byte R, byte G, byte B) CmykToRgb(double c, double m, double y, double k)
        {
            byte r = (byte)Math.Clamp(255 * (1 - c) * (1 - k), 0, 255);
            byte g = (byte)Math.Clamp(255 * (1 - m) * (1 - k), 0, 255);
            byte b = (byte)Math.Clamp(255 * (1 - y) * (1 - k), 0, 255);

            return (r, g, b);
        }

        // ==========================================
        // YCbCr & YUV
        // ==========================================

        public static (double Y, double Cb, double Cr) RgbToYCbCr(byte r, byte g, byte b)
        {
            double y = 0.299 * r + 0.587 * g + 0.114 * b;
            double cb = 128 - 0.168736 * r - 0.331264 * g + 0.5 * b;
            double cr = 128 + 0.5 * r - 0.418688 * g - 0.081312 * b;
            return (y, cb, cr);
        }

        public static (byte R, byte G, byte B) YCbCrToRgb(double y, double cb, double cr)
        {
            byte r = (byte)Math.Clamp(y + 1.402 * (cr - 128), 0, 255);
            byte g = (byte)Math.Clamp(y - 0.344136 * (cb - 128) - 0.714136 * (cr - 128), 0, 255);
            byte b = (byte)Math.Clamp(y + 1.772 * (cb - 128), 0, 255);
            return (r, g, b);
        }

        public static (double Y, double U, double V) RgbToYuv(byte r, byte g, byte b)
        {
            double y = 0.299 * r + 0.587 * g + 0.114 * b;
            double u = -0.14713 * r - 0.28886 * g + 0.436 * b;
            double v = 0.615 * r - 0.51499 * g - 0.10001 * b;
            return (y, u, v);
        }

        public static (byte R, byte G, byte B) YuvToRgb(double y, double u, double v)
        {
            byte r = (byte)Math.Clamp(y + 1.13983 * v, 0, 255);
            byte g = (byte)Math.Clamp(y - 0.39465 * u - 0.58060 * v, 0, 255);
            byte b = (byte)Math.Clamp(y + 2.03211 * u, 0, 255);
            return (r, g, b);
        }

        // ==========================================
        // CIE LAB 
        // ==========================================

        public static (double L, double A, double B_channel) RgbToLab(byte r, byte g, byte b)
        {
            double rNorm = r / 255.0;
            double gNorm = g / 255.0;
            double bNorm = b / 255.0;

            rNorm = (rNorm > 0.04045) ? Math.Pow((rNorm + 0.055) / 1.055, 2.4) : (rNorm / 12.92);
            gNorm = (gNorm > 0.04045) ? Math.Pow((gNorm + 0.055) / 1.055, 2.4) : (gNorm / 12.92);
            bNorm = (bNorm > 0.04045) ? Math.Pow((bNorm + 0.055) / 1.055, 2.4) : (bNorm / 12.92);

            double x = (rNorm * 0.4124 + gNorm * 0.3576 + bNorm * 0.1805) / 0.95047;
            double y = (rNorm * 0.2126 + gNorm * 0.7152 + bNorm * 0.0722) / 1.00000;
            double z = (rNorm * 0.0193 + gNorm * 0.1192 + bNorm * 0.9505) / 1.08883;

            x = (x > 0.008856) ? Math.Pow(x, 1.0 / 3.0) : (7.787 * x) + (16.0 / 116.0);
            y = (y > 0.008856) ? Math.Pow(y, 1.0 / 3.0) : (7.787 * y) + (16.0 / 116.0);
            z = (z > 0.008856) ? Math.Pow(z, 1.0 / 3.0) : (7.787 * z) + (16.0 / 116.0);

            double lightness = (116.0 * y) - 16.0;
            double aChannel = 500.0 * (x - y);
            double bChannel = 200.0 * (y - z);

            return (lightness, aChannel, bChannel);
        }

        public static (byte R, byte G, byte B) LabToRgb(double l, double a, double b)
        {
            double y = (l + 16.0) / 116.0;
            double x = a / 500.0 + y;
            double z = y - b / 200.0;

            x = 0.95047 * ((Math.Pow(x, 3) > 0.008856) ? Math.Pow(x, 3) : (x - 16.0 / 116.0) / 7.787);
            y = 1.00000 * ((Math.Pow(y, 3) > 0.008856) ? Math.Pow(y, 3) : (y - 16.0 / 116.0) / 7.787);
            z = 1.08883 * ((Math.Pow(z, 3) > 0.008856) ? Math.Pow(z, 3) : (z - 16.0 / 116.0) / 7.787);

            double rNorm = x * 3.2406 + y * -1.5372 + z * -0.4986;
            double gNorm = x * -0.9689 + y * 1.8758 + z * 0.0415;
            double bNorm = x * 0.0557 + y * -0.2040 + z * 1.0570;

            rNorm = (rNorm > 0.0031308) ? (1.055 * Math.Pow(rNorm, 1 / 2.4) - 0.055) : (12.92 * rNorm);
            gNorm = (gNorm > 0.0031308) ? (1.055 * Math.Pow(gNorm, 1 / 2.4) - 0.055) : (12.92 * gNorm);
            bNorm = (bNorm > 0.0031308) ? (1.055 * Math.Pow(bNorm, 1 / 2.4) - 0.055) : (12.92 * bNorm);

            byte r = (byte)Math.Clamp(rNorm * 255, 0, 255);
            byte g = (byte)Math.Clamp(gNorm * 255, 0, 255);
            byte outB = (byte)Math.Clamp(bNorm * 255, 0, 255);

            return (r, g, outB);
        }
    }
}