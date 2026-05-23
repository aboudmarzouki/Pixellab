using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PixelLab
{
    public class ImageProcessor
    {
        private byte[]? _originalPixels;
        private int _width;
        private int _height;
        private int _stride;

        public WriteableBitmap? CurrentBitmap { get; private set; }

        public WriteableBitmap LoadImage(string filePath)
        {
            BitmapImage bitmapSource = new BitmapImage(new Uri(filePath));
            FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap(bitmapSource, System.Windows.Media.PixelFormats.Bgra32, null, 0);

            _width = convertedBitmap.PixelWidth;
            _height = convertedBitmap.PixelHeight;
            _stride = _width * 4;

            _originalPixels = new byte[_height * _stride];
            convertedBitmap.CopyPixels(_originalPixels, _stride, 0);

            CurrentBitmap = new WriteableBitmap(convertedBitmap);
            return CurrentBitmap;
        }

        public void ApplySystemFilters(string system, double channel1Scale, double channel2Scale, double channel3Scale, double channel4Scale)
        {
            if (_originalPixels == null || CurrentBitmap == null) return;

            byte[] modifiedPixels = new byte[_originalPixels.Length];
            int totalPixels = _originalPixels.Length / 4;

            Parallel.For(0, totalPixels, pixelIndex =>
            {
                int i = pixelIndex * 4;

                byte originalBlue = _originalPixels[i];
                byte originalGreen = _originalPixels[i + 1];
                byte originalRed = _originalPixels[i + 2];
                byte alphaChannel = _originalPixels[i + 3];

                byte finalRed = originalRed;
                byte finalGreen = originalGreen;
                byte finalBlue = originalBlue;

                if (system == "RGB")
                {
                    finalRed = (byte)(originalRed * (channel1Scale / 255.0));
                    finalGreen = (byte)(originalGreen * (channel2Scale / 255.0));
                    finalBlue = (byte)(originalBlue * (channel3Scale / 255.0));
                }
                else if (system == "HSV")
                {
                    var hsv = Models.ColorConverter.RgbToHsv(originalRed, originalGreen, originalBlue);
                    double scaledH = Math.Clamp(hsv.H * (channel1Scale / 360.0), 0, 360);
                    double scaledS = Math.Clamp(hsv.S * (channel2Scale / 100.0), 0, 1);
                    double scaledV = Math.Clamp(hsv.V * (channel3Scale / 100.0), 0, 1);

                    var rgb = Models.ColorConverter.HsvToRgb(scaledH, scaledS, scaledV);
                    finalRed = rgb.R; finalGreen = rgb.G; finalBlue = rgb.B;
                }
                else if (system == "CMYK")
                {
                    var cmyk = Models.ColorConverter.RgbToCmyk(originalRed, originalGreen, originalBlue);
                    double scaledC = Math.Clamp(cmyk.C * (channel1Scale / 100.0), 0, 1);
                    double scaledM = Math.Clamp(cmyk.M * (channel2Scale / 100.0), 0, 1);
                    double scaledY = Math.Clamp(cmyk.Y * (channel3Scale / 100.0), 0, 1);
                    double scaledK = Math.Clamp(cmyk.K * (channel4Scale / 100.0), 0, 1);

                    var rgb = Models.ColorConverter.CmykToRgb(scaledC, scaledM, scaledY, scaledK);
                    finalRed = rgb.R; finalGreen = rgb.G; finalBlue = rgb.B;
                }
                else if (system == "YCbCr")
                {
                    var ycbcr = Models.ColorConverter.RgbToYCbCr(originalRed, originalGreen, originalBlue);
                    double scaledY = Math.Clamp(ycbcr.Y * (channel1Scale / 255.0), 0, 255);
                    double scaledCb = Math.Clamp(ycbcr.Cb * (channel2Scale / 255.0), 0, 255);
                    double scaledCr = Math.Clamp(ycbcr.Cr * (channel3Scale / 255.0), 0, 255);

                    var rgb = Models.ColorConverter.YCbCrToRgb(scaledY, scaledCb, scaledCr);
                    finalRed = rgb.R; finalGreen = rgb.G; finalBlue = rgb.B;
                }
                else if (system == "YUV")
                {
                    var yuv = Models.ColorConverter.RgbToYuv(originalRed, originalGreen, originalBlue);
                    double scaledY = yuv.Y * (channel1Scale / 255.0);
                    double scaledU = yuv.U * (channel2Scale / 255.0);
                    double scaledV = yuv.V * (channel3Scale / 255.0);

                    var rgb = Models.ColorConverter.YuvToRgb(scaledY, scaledU, scaledV);
                    finalRed = rgb.R; finalGreen = rgb.G; finalBlue = rgb.B;
                }
                else if (system == "LAB")
                {
                    var lab = Models.ColorConverter.RgbToLab(originalRed, originalGreen, originalBlue);
                    double scaledL = Math.Clamp(lab.L * (channel1Scale / 100.0), 0, 100);
                    double scaledA = lab.A * (channel2Scale / 127.0);
                    double scaledB = lab.B_channel * (channel3Scale / 127.0);

                    var rgb = Models.ColorConverter.LabToRgb(scaledL, scaledA, scaledB);
                    finalRed = rgb.R; finalGreen = rgb.G; finalBlue = rgb.B;
                }

                modifiedPixels[i] = finalBlue;
                modifiedPixels[i + 1] = finalGreen;
                modifiedPixels[i + 2] = finalRed;
                modifiedPixels[i + 3] = alphaChannel;
            });

            CurrentBitmap.WritePixels(new Int32Rect(0, 0, _width, _height), modifiedPixels, _stride, 0);
        }

        public void QuantizeImage(int targetTotalColors)
        {
            if (_originalPixels == null || CurrentBitmap == null || targetTotalColors < 2) return;

            byte[] modifiedPixels = new byte[_originalPixels.Length];
            int levelsPerChannel = (int)Math.Max(2, Math.Round(Math.Pow(targetTotalColors, 1.0 / 3.0)));
            int step = 255 / (levelsPerChannel - 1);

            Parallel.For(0, _originalPixels.Length / 4, pixelIndex =>
            {
                int i = pixelIndex * 4;

                byte b = _originalPixels[i];
                byte g = _originalPixels[i + 1];
                byte r = _originalPixels[i + 2];
                byte a = _originalPixels[i + 3];

                byte snappedB = (byte)Math.Clamp(Math.Round((double)b / step) * step, 0, 255);
                byte snappedG = (byte)Math.Clamp(Math.Round((double)g / step) * step, 0, 255);
                byte snappedR = (byte)Math.Clamp(Math.Round((double)r / step) * step, 0, 255);

                modifiedPixels[i] = snappedB;
                modifiedPixels[i + 1] = snappedG;
                modifiedPixels[i + 2] = snappedR;
                modifiedPixels[i + 3] = a;
            });

            CurrentBitmap.WritePixels(new Int32Rect(0, 0, _width, _height), modifiedPixels, _stride, 0);
        }
    }
}