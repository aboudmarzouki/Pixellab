using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace PixelLab
{
    public partial class MainWindow : Window
    {
        private ImageProcessor _processor;
        private string _currentColorSystem = "RGB";
        private int _currentQuantizationLevel = 0; // 0 means quantization is turned off

        public MainWindow()
        {
            InitializeComponent();
            _processor = new ImageProcessor();
            UpdateChannelLabels();
        }

        // Drag and Drop
        private void ImageArea_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string filePath = files[0];

                MainImage.Source = _processor.LoadImage(filePath);
                PlaceholderText.Visibility = Visibility.Collapsed;

                FileInfo fileInfo = new FileInfo(filePath);
                LblImageName.Text = "Name: " + fileInfo.Name;
                LblImageFormat.Text = "Format: " + fileInfo.Extension;
                LblImageSize.Text = "Size: " + (fileInfo.Length / 1024) + " KB";

                ResetControls();
            }
        }

        private void CmbColorSystem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || CmbColorSystem.SelectedItem == null) return;

            ComboBoxItem selectedItem = (ComboBoxItem)CmbColorSystem.SelectedItem;
            _currentColorSystem = selectedItem.Content?.ToString() ?? "RGB";

            UpdateChannelLabels();
        }

        private void UpdateChannelLabels()
        {
            Channel4Panel.Visibility = Visibility.Collapsed;

            // Detach events temporarily so we don't trigger processing loops while updating UI
            DetachSliderEvents();

            if (_currentColorSystem == "RGB")
            {
                ChkChannel1.Content = "Red (R)"; ChkChannel2.Content = "Green (G)"; ChkChannel3.Content = "Blue (B)";
                SldChannel1.Maximum = 255; SldChannel2.Maximum = 255; SldChannel3.Maximum = 255;
            }
            else if (_currentColorSystem == "HSV")
            {
                ChkChannel1.Content = "Hue (H)"; ChkChannel2.Content = "Saturation (S)"; ChkChannel3.Content = "Value (V)";
                SldChannel1.Maximum = 360; SldChannel2.Maximum = 100; SldChannel3.Maximum = 100;
            }
            else if (_currentColorSystem == "CMYK")
            {
                ChkChannel1.Content = "Cyan (C)"; ChkChannel2.Content = "Magenta (M)"; ChkChannel3.Content = "Yellow (Y)";
                SldChannel1.Maximum = 100; SldChannel2.Maximum = 100; SldChannel3.Maximum = 100;

                Channel4Panel.Visibility = Visibility.Visible;
                ChkChannel4.Content = "Key (K)";
                SldChannel4.Maximum = 100;
            }
            else if (_currentColorSystem == "YUV")
            {
                ChkChannel1.Content = "Luma (Y)"; ChkChannel2.Content = "Chroma (U)"; ChkChannel3.Content = "Chroma (V)";
                SldChannel1.Maximum = 255; SldChannel2.Maximum = 255; SldChannel3.Maximum = 255;
            }
            else if (_currentColorSystem == "LAB")
            {
                ChkChannel1.Content = "Lightness (L)"; ChkChannel2.Content = "A Channel"; ChkChannel3.Content = "B Channel";
                SldChannel1.Maximum = 100; SldChannel2.Maximum = 127; SldChannel3.Maximum = 127;
            }
            else if (_currentColorSystem == "YCbCr")
            {
                ChkChannel1.Content = "Luma (Y)"; ChkChannel2.Content = "Blue Diff (Cb)"; ChkChannel3.Content = "Red Diff (Cr)";
                SldChannel1.Maximum = 255; SldChannel2.Maximum = 255; SldChannel3.Maximum = 255;
            }

            SldChannel1.Value = SldChannel1.Maximum;
            SldChannel2.Value = SldChannel2.Maximum;
            SldChannel3.Value = SldChannel3.Maximum;
            if (Channel4Panel.Visibility == Visibility.Visible) SldChannel4.Value = SldChannel4.Maximum;

            AttachSliderEvents();
            ProcessImage();
        }

        private void ProcessImage()
        {
            if (!IsLoaded || _processor?.CurrentBitmap == null) return;

            double v1 = ChkChannel1.IsChecked == true ? SldChannel1.Value : 0;
            double v2 = ChkChannel2.IsChecked == true ? SldChannel2.Value : 0;
            double v3 = ChkChannel3.IsChecked == true ? SldChannel3.Value : 0;
            double v4 = ChkChannel4.IsChecked == true ? SldChannel4.Value : 0;

            // Unified Pipeline: Filter + Quantize
            _processor.ApplySystemFilters(_currentColorSystem, v1, v2, v3, v4, _currentQuantizationLevel);

            // Real-time 3D update
            Update3DModel();
        }

        private void ChannelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ProcessImage();
        private void ChannelToggle_Changed(object sender, RoutedEventArgs e) => ProcessImage();

        private void DetachSliderEvents()
        {
            SldChannel1.ValueChanged -= ChannelSlider_ValueChanged;
            SldChannel2.ValueChanged -= ChannelSlider_ValueChanged;
            SldChannel3.ValueChanged -= ChannelSlider_ValueChanged;
            SldChannel4.ValueChanged -= ChannelSlider_ValueChanged;
        }

        private void AttachSliderEvents()
        {
            SldChannel1.ValueChanged += ChannelSlider_ValueChanged;
            SldChannel2.ValueChanged += ChannelSlider_ValueChanged;
            SldChannel3.ValueChanged += ChannelSlider_ValueChanged;
            SldChannel4.ValueChanged += ChannelSlider_ValueChanged;
        }

        private void ApplyQuantization_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || _processor?.CurrentBitmap == null) return;

            if (int.TryParse(TxtColorCount.Text, out int colorCount))
            {
                _currentQuantizationLevel = System.Math.Clamp(colorCount, 2, 256);
                TxtColorCount.Text = _currentQuantizationLevel.ToString();

                // Run the unified pipeline
                ProcessImage();
            }
            else
            {
                MessageBox.Show("Please enter a valid whole number.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetControls();

        private void ResetControls()
        {
            DetachSliderEvents();

            SldChannel1.Value = SldChannel1.Maximum;
            SldChannel2.Value = SldChannel2.Maximum;
            SldChannel3.Value = SldChannel3.Maximum;
            if (Channel4Panel.Visibility == Visibility.Visible) SldChannel4.Value = SldChannel4.Maximum;

            ChkChannel1.IsChecked = true;
            ChkChannel2.IsChecked = true;
            ChkChannel3.IsChecked = true;
            ChkChannel4.IsChecked = true;

            // Reset quantization state
            _currentQuantizationLevel = 0;
            TxtColorCount.Text = "256";

            AttachSliderEvents();
            ProcessImage();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_processor?.CurrentBitmap == null)
            {
                MessageBox.Show("There is no image to save!", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.Title = "Save Processed Image";
            saveDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp";
            saveDialog.DefaultExt = ".png";

            if (saveDialog.ShowDialog() == true)
            {
                string extension = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();
                BitmapEncoder encoder;

                if (extension == ".jpg" || extension == ".jpeg") encoder = new JpegBitmapEncoder();
                else if (extension == ".bmp") encoder = new BmpBitmapEncoder();
                else encoder = new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(_processor.CurrentBitmap));

                using (var fileStream = new System.IO.FileStream(saveDialog.FileName, System.IO.FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                MessageBox.Show("Image saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ==========================================
        // 3D Rendering Engine
        // ==========================================

        private void Generate3D_Click(object sender, RoutedEventArgs e)
        {
            if (_processor?.CurrentBitmap == null)
            {
                MessageBox.Show("Please load an image in the 2D View first", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Update3DModel();
        }

        private void Update3DModel()
        {
            if (!IsLoaded || _processor?.CurrentBitmap == null) return;

            // Hide the color info panel if the 3D shape refreshes
            ColorInfoPanel.Visibility = Visibility.Collapsed;

            while (ModelGroup3D.Children.Count > 3)
            {
                ModelGroup3D.Children.RemoveAt(3);
            }

            DrawAxes();

            int stride = _processor.CurrentBitmap.PixelWidth * 4;
            int size = _processor.CurrentBitmap.PixelHeight * stride;
            byte[] pixels = new byte[size];
            _processor.CurrentBitmap.CopyPixels(pixels, stride, 0);

            // Sub-sampling for performance
            int sampleStep = 15;
            int width = _processor.CurrentBitmap.PixelWidth;
            int height = _processor.CurrentBitmap.PixelHeight;

            for (int y = 0; y < height; y += sampleStep)
            {
                for (int x = 0; x < width; x += sampleStep)
                {
                    int index = (y * stride) + (x * 4);

                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];

                    double posX = 0, posY = 0, posZ = 0;

                    if (_currentColorSystem == "RGB")
                    {
                        posX = r / 255.0; posY = g / 255.0; posZ = b / 255.0;
                    }
                    else if (_currentColorSystem == "HSV")
                    {
                        var hsv = Models.ColorConverter.RgbToHsv(r, g, b);
                        double angleRad = hsv.H * (System.Math.PI / 180.0);
                        posX = 0.5 + (hsv.S * 0.5 * System.Math.Cos(angleRad));
                        posZ = 0.5 + (hsv.S * 0.5 * System.Math.Sin(angleRad));
                        posY = hsv.V;
                    }
                    else if (_currentColorSystem == "LAB")
                    {
                        var lab = Models.ColorConverter.RgbToLab(r, g, b);
                        posY = lab.L / 100.0;
                        posX = (lab.A + 128.0) / 256.0;
                        posZ = (lab.B_channel + 128.0) / 256.0;
                    }
                    else if (_currentColorSystem == "CMYK")
                    {
                        var cmyk = Models.ColorConverter.RgbToCmyk(r, g, b);
                        posX = cmyk.C; posY = cmyk.M; posZ = cmyk.Y;
                    }
                    else if (_currentColorSystem == "YCbCr" || _currentColorSystem == "YUV")
                    {
                        var ycbcr = Models.ColorConverter.RgbToYCbCr(r, g, b);
                        posY = ycbcr.Y / 255.0; posX = ycbcr.Cb / 255.0; posZ = ycbcr.Cr / 255.0;
                    }

                    DrawTinyBox(posX, posY, posZ, System.Windows.Media.Color.FromRgb(r, g, b));
                }
            }
        }

        private void DrawAxes()
        {
            double length = 1.1; double thickness = 0.003;
            DrawAxisBox(length, thickness, thickness, length / 2, 0, 0, System.Windows.Media.Colors.Red);
            DrawAxisBox(thickness, length, thickness, 0, length / 2, 0, System.Windows.Media.Colors.Lime);
            DrawAxisBox(thickness, thickness, length, 0, 0, length / 2, System.Windows.Media.Colors.DodgerBlue);
            DrawAxisBox(0.015, 0.015, 0.015, 0, 0, 0, System.Windows.Media.Colors.White);
        }

        private void DrawAxisBox(double sizeX, double sizeY, double sizeZ, double centerX, double centerY, double centerZ, System.Windows.Media.Color color)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            double hX = sizeX / 2.0; double hY = sizeY / 2.0; double hZ = sizeZ / 2.0;

            Point3D[] corners = new Point3D[]
            {
                new Point3D(centerX - hX, centerY - hY, centerZ - hZ), new Point3D(centerX + hX, centerY - hY, centerZ - hZ),
                new Point3D(centerX + hX, centerY + hY, centerZ - hZ), new Point3D(centerX - hX, centerY + hY, centerZ - hZ),
                new Point3D(centerX - hX, centerY - hY, centerZ + hZ), new Point3D(centerX + hX, centerY - hY, centerZ + hZ),
                new Point3D(centerX + hX, centerY + hY, centerZ + hZ), new Point3D(centerX - hX, centerY + hY, centerZ + hZ)
            };
            foreach (var p in corners) mesh.Positions.Add(p);
            int[] indices = { 4, 5, 6, 6, 7, 4, 1, 0, 3, 3, 2, 1, 0, 4, 7, 7, 3, 0, 5, 1, 2, 2, 6, 5, 3, 7, 6, 6, 2, 3, 4, 0, 1, 1, 5, 4 };
            foreach (int index in indices) mesh.TriangleIndices.Add(index);

            System.Windows.Media.SolidColorBrush brush = new System.Windows.Media.SolidColorBrush(color);
            EmissiveMaterial material = new EmissiveMaterial(brush);
            GeometryModel3D model = new GeometryModel3D(mesh, material);
            ModelGroup3D.Children.Add(model);
        }

        private void DrawTinyBox(double x, double y, double z, System.Windows.Media.Color color)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            double s = 0.01;

            Point3D[] corners = new Point3D[]
            {
                new Point3D(x - s, y - s, z - s), new Point3D(x + s, y - s, z - s),
                new Point3D(x + s, y + s, z - s), new Point3D(x - s, y + s, z - s),
                new Point3D(x - s, y - s, z + s), new Point3D(x + s, y - s, z + s),
                new Point3D(x + s, y + s, z + s), new Point3D(x - s, y + s, z + s)
            };
            foreach (var p in corners) mesh.Positions.Add(p);
            int[] indices = { 4, 5, 6, 6, 7, 4, 1, 0, 3, 3, 2, 1, 0, 4, 7, 7, 3, 0, 5, 1, 2, 2, 6, 5, 3, 7, 6, 6, 2, 3, 4, 0, 1, 1, 5, 4 };
            foreach (int index in indices) mesh.TriangleIndices.Add(index);

            System.Windows.Media.SolidColorBrush brush = new System.Windows.Media.SolidColorBrush(color);
            DiffuseMaterial material = new DiffuseMaterial(brush);
            GeometryModel3D model = new GeometryModel3D(mesh, material);
            ModelGroup3D.Children.Add(model);
        }

        // ==========================================
        // 3D Interaction (Camera, Zoom, Pick Color)
        // ==========================================

        private double _cameraRadius = 2.5;
        private double _cameraAngleRadians = 45.0 * (System.Math.PI / 180.0);

        private void UpdateCameraPosition()
        {
            if (Camera3D == null) return;
            double centerX = 0.5; double centerZ = 0.5;
            double newX = centerX + System.Math.Cos(_cameraAngleRadians) * _cameraRadius;
            double newZ = centerZ + System.Math.Sin(_cameraAngleRadians) * _cameraRadius;

            Camera3D.Position = new Point3D(newX, 1.5, newZ);
            Camera3D.LookDirection = new Vector3D(centerX - newX, 0.5 - 1.5, centerZ - newZ);
        }

        private void CameraSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _cameraAngleRadians = e.NewValue * (System.Math.PI / 180.0);
            UpdateCameraPosition();
        }

        private void MainViewport3D_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _cameraRadius -= e.Delta * 0.001;
            _cameraRadius = System.Math.Clamp(_cameraRadius, 0.5, 10.0);
            UpdateCameraPosition();
        }

        private void MainViewport3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(MainViewport3D);
            PointHitTestParameters hitParams = new PointHitTestParameters(mousePos);
            VisualTreeHelper.HitTest(MainViewport3D, null, ResultCallback, hitParams);
        }

        private HitTestResultBehavior ResultCallback(HitTestResult result)
        {
            if (result is RayMeshGeometry3DHitTestResult meshResult)
            {
                if (meshResult.ModelHit is GeometryModel3D model)
                {
                    System.Windows.Media.Color? pickedColor = null;
                    if (model.Material is DiffuseMaterial diffMat && diffMat.Brush is System.Windows.Media.SolidColorBrush diffBrush)
                    {
                        pickedColor = diffBrush.Color;
                    }

                    if (pickedColor.HasValue)
                    {
                        UpdateColorInfoPanel(pickedColor.Value);
                        return HitTestResultBehavior.Stop;
                    }
                }
            }
            return HitTestResultBehavior.Continue;
        }

        private void UpdateColorInfoPanel(System.Windows.Media.Color c)
        {
            ColorInfoPanel.Visibility = Visibility.Visible;
            PickedColorSwatch.Background = new System.Windows.Media.SolidColorBrush(c);
            TxtPickedHex.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            TxtPickedRGB.Text = $"RGB: ({c.R}, {c.G}, {c.B})";

            var hsv = Models.ColorConverter.RgbToHsv(c.R, c.G, c.B);
            TxtPickedHSV.Text = $"HSV: ({System.Math.Round(hsv.H)}°, {System.Math.Round(hsv.S * 100)}%, {System.Math.Round(hsv.V * 100)}%)";

            var lab = Models.ColorConverter.RgbToLab(c.R, c.G, c.B);
            TxtPickedLAB.Text = $"LAB: ({System.Math.Round(lab.L)}, {System.Math.Round(lab.A)}, {System.Math.Round(lab.B_channel)})";

            var cmyk = Models.ColorConverter.RgbToCmyk(c.R, c.G, c.B);
            TxtPickedCMYK.Text = $"CMYK: ({System.Math.Round(cmyk.C * 100)}%, {System.Math.Round(cmyk.M * 100)}%, {System.Math.Round(cmyk.Y * 100)}%, {System.Math.Round(cmyk.K * 100)}%)";

            // Sync sliders safely without triggering loops
            DetachSliderEvents();

            if (_currentColorSystem == "RGB")
            {
                SldChannel1.Value = c.R; SldChannel2.Value = c.G; SldChannel3.Value = c.B;
            }
            else if (_currentColorSystem == "HSV")
            {
                SldChannel1.Value = hsv.H; SldChannel2.Value = hsv.S * 100; SldChannel3.Value = hsv.V * 100;
            }
            else if (_currentColorSystem == "LAB")
            {
                SldChannel1.Value = lab.L; SldChannel2.Value = lab.A; SldChannel3.Value = lab.B_channel;
            }
            else if (_currentColorSystem == "CMYK")
            {
                SldChannel1.Value = cmyk.C * 100; SldChannel2.Value = cmyk.M * 100;
                SldChannel3.Value = cmyk.Y * 100; SldChannel4.Value = cmyk.K * 100;
            }
            else if (_currentColorSystem == "YCbCr" || _currentColorSystem == "YUV")
            {
                var ycbcr = Models.ColorConverter.RgbToYCbCr(c.R, c.G, c.B);
                SldChannel1.Value = ycbcr.Y; SldChannel2.Value = ycbcr.Cb; SldChannel3.Value = ycbcr.Cr;
            }

            AttachSliderEvents();

            // Re-apply pipeline so the newly picked color is reflected actively
            double v1 = ChkChannel1.IsChecked == true ? SldChannel1.Value : 0;
            double v2 = ChkChannel2.IsChecked == true ? SldChannel2.Value : 0;
            double v3 = ChkChannel3.IsChecked == true ? SldChannel3.Value : 0;
            double v4 = ChkChannel4.IsChecked == true ? SldChannel4.Value : 0;
            _processor.ApplySystemFilters(_currentColorSystem, v1, v2, v3, v4, _currentQuantizationLevel);
        }
    }
}