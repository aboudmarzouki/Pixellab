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

            int pixelCount = _originalPixels.Length / 4;

            // For exact N-color quantization use k-means clustering on RGB
            int k = Math.Clamp(targetTotalColors, 2, 256);

            // Build sample points (use all pixels but can sample if very large)
            int maxSamples = 10000;
            int sampleStep = Math.Max(1, pixelCount / maxSamples);
            var samples = new System.Collections.Generic.List<int[]>(Math.Min(pixelCount, maxSamples));
            for (int pi = 0; pi < pixelCount; pi += sampleStep)
            {
                int i = pi * 4;
                byte b = _originalPixels[i];
                byte g = _originalPixels[i + 1];
                byte r = _originalPixels[i + 2];
                samples.Add(new int[] { r, g, b });
            }

            var rnd = new Random(0);
            int sCount = samples.Count;

            // Initialize centroids by picking random unique samples
            var centroids = new int[k][];
            var used = new System.Collections.Generic.HashSet<int>();
            for (int ci = 0; ci < k; ci++)
            {
                int idx;
                int tries = 0;
                do
                {
                    idx = rnd.Next(sCount);
                    tries++;
                } while (used.Contains(idx) && tries < 10);
                used.Add(idx);
                centroids[ci] = new int[] { samples[idx][0], samples[idx][1], samples[idx][2] };
            }

            int maxIters = 12;
            var assignments = new int[sCount];

            for (int iter = 0; iter < maxIters; iter++)
            {
                bool changed = false;

                // Assignment step
                for (int si = 0; si < sCount; si++)
                {
                    int[] p = samples[si];
                    int best = 0;
                    double bestDist = double.MaxValue;
                    for (int ci = 0; ci < k; ci++)
                    {
                        int dr = p[0] - centroids[ci][0];
                        int dg = p[1] - centroids[ci][1];
                        int db = p[2] - centroids[ci][2];
                        double dist = dr * dr + dg * dg + db * db;
                        if (dist < bestDist) { bestDist = dist; best = ci; }
                    }
                    if (assignments[si] != best) { assignments[si] = best; changed = true; }
                }

                // Update step
                var sums = new long[k][];
                var counts = new int[k];
                for (int ci = 0; ci < k; ci++) sums[ci] = new long[3];

                for (int si = 0; si < sCount; si++)
                {
                    int a = assignments[si];
                    int[] p = samples[si];
                    sums[a][0] += p[0]; sums[a][1] += p[1]; sums[a][2] += p[2];
                    counts[a]++;
                }

                for (int ci = 0; ci < k; ci++)
                {
                    if (counts[ci] == 0)
                    {
                        // reinitialize empty centroid
                        int idx = rnd.Next(sCount);
                        centroids[ci][0] = samples[idx][0];
                        centroids[ci][1] = samples[idx][1];
                        centroids[ci][2] = samples[idx][2];
                    }
                    else
                    {
                        int nr = (int)(sums[ci][0] / counts[ci]);
                        int ng = (int)(sums[ci][1] / counts[ci]);
                        int nb = (int)(sums[ci][2] / counts[ci]);
                        centroids[ci][0] = nr; centroids[ci][1] = ng; centroids[ci][2] = nb;
                    }
                }

                if (!changed) break;
            }

            // Map every pixel to nearest centroid
            byte[] modifiedPixels = new byte[_originalPixels.Length];

            Parallel.For(0, pixelCount, pixelIndex =>
            {
                int i = pixelIndex * 4;
                int br = _originalPixels[i + 2];
                int bg = _originalPixels[i + 1];
                int bb = _originalPixels[i];

                int best = 0;
                double bestDist = double.MaxValue;
                for (int ci = 0; ci < k; ci++)
                {
                    int dr = br - centroids[ci][0];
                    int dg = bg - centroids[ci][1];
                    int db = bb - centroids[ci][2];
                    double dist = dr * dr + dg * dg + db * db;
                    if (dist < bestDist) { bestDist = dist; best = ci; }
                }

                modifiedPixels[i] = (byte)centroids[best][2];
                modifiedPixels[i + 1] = (byte)centroids[best][1];
                modifiedPixels[i + 2] = (byte)centroids[best][0];
                modifiedPixels[i + 3] = _originalPixels[i + 3];
            });

            CurrentBitmap.WritePixels(new Int32Rect(0, 0, _width, _height), modifiedPixels, _stride, 0);
        }
    }
}