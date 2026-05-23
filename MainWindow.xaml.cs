using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace PixelLab
{
    public partial class MainWindow : Window
    {
        private ImageProcessor _processor;
        private string _currentColorSystem = "RGB";

        public MainWindow()
        {
            InitializeComponent();
            _processor = new ImageProcessor();

            UpdateChannelLabels();
        }

        // هون ال drag and drop
        private void ImageArea_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string filePath = files[0];

                MainImage.Source = _processor.LoadImage(filePath);
                PlaceholderText.Visibility = Visibility.Collapsed;

                // معلومات الصورة
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

            // Safely retrieve the selected color system string
            ComboBoxItem selectedItem = (ComboBoxItem)CmbColorSystem.SelectedItem;
            _currentColorSystem = selectedItem.Content?.ToString() ?? "RGB";

            UpdateChannelLabels();
        }

        // تغيير السلايدرات حسب النظتم يلي منختارو
        private void UpdateChannelLabels()
        {
            // تغطاية القناة الرابعة بالبداية لأنو ما منبلش بالcmyk
            Channel4Panel.Visibility = Visibility.Collapsed;

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

                // هون اظهرنا القناة الرابعة
                Channel4Panel.Visibility = Visibility.Visible;
                ChkChannel4.Content = "Key (K)";
                SldChannel4.Maximum = 100;
                SldChannel4.Value = SldChannel4.Maximum;
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

            // منرجع القيم لاعلى قيمة وقت نقلب النظام
            SldChannel1.Value = SldChannel1.Maximum;
            SldChannel2.Value = SldChannel2.Maximum;
            SldChannel3.Value = SldChannel3.Maximum;

            ProcessImage();
        }

        private void ProcessImage()
        {
            if (!IsLoaded || _processor?.CurrentBitmap == null) return;

            // اذا مو معمول check منبعت 0
            double v1 = ChkChannel1.IsChecked == true ? SldChannel1.Value : 0;
            double v2 = ChkChannel2.IsChecked == true ? SldChannel2.Value : 0;
            double v3 = ChkChannel3.IsChecked == true ? SldChannel3.Value : 0;
            double v4 = ChkChannel4.IsChecked == true ? SldChannel4.Value : 0;

            _processor.ApplySystemFilters(_currentColorSystem, v1, v2, v3, v4);
        }

        private void ChannelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ProcessImage();
        private void ChannelToggle_Changed(object sender, RoutedEventArgs e) => ProcessImage();

        private void ApplyQuantization_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || _processor?.CurrentBitmap == null) return;

            if (int.TryParse(TxtColorCount.Text, out int colorCount))
            {
                // منحصر القيم بين 0 و 256 مشان ما يعمل كراش او يعكي اكسيبشن
                colorCount = System.Math.Clamp(colorCount, 2, 256);
                TxtColorCount.Text = colorCount.ToString();

                _processor.QuantizeImage(colorCount);
            }
            else
            {
                MessageBox.Show("Please enter a valid whole number.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetControls();

        private void ResetControls()
        {
            // reset 
            SldChannel1.Value = SldChannel1.Maximum;
            SldChannel2.Value = SldChannel2.Maximum;
            SldChannel3.Value = SldChannel3.Maximum;
            if (Channel4Panel.Visibility == Visibility.Visible) SldChannel4.Value = SldChannel4.Maximum;

            ChkChannel1.IsChecked = true;
            ChkChannel2.IsChecked = true;
            ChkChannel3.IsChecked = true;
            ChkChannel4.IsChecked = true;

            TxtColorCount.Text = "256";
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

        // 3D COLOR SPACE

        private void Generate3D_Click(object sender, RoutedEventArgs e)
        {
            if (_processor?.CurrentBitmap == null)
            {
                MessageBox.Show("Please load an image in the 2D View first", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // هاد شغلتو انو كل ما منكبس بينمحى المكعب القديم
            while (ModelGroup3D.Children.Count > 3)
            {
                ModelGroup3D.Children.RemoveAt(3);
            }

            DrawAxes();

            // منستخرج المصفوفة تبع الصورة
            int stride = _processor.CurrentBitmap.PixelWidth * 4;
            int size = _processor.CurrentBitmap.PixelHeight * stride;
            byte[] pixels = new byte[size];
            _processor.CurrentBitmap.CopyPixels(pixels, stride, 0);

            // عم نعمل sampling عم نقطع كل 15 بايت
            int sampleStep = 15;
            int width = _processor.CurrentBitmap.PixelWidth;
            int height = _processor.CurrentBitmap.PixelHeight;

            for (int y = 0; y < height; y += sampleStep)
            {
                for (int x = 0; x < width; x += sampleStep)
                {
                    // حساب موقعنا بالمصفوفة الأحادية
                    int index = (y * stride) + (x * 4);

                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];

                    // منحول القيم اللونية لاحداثيات هندسية 
                    double posX = r / 255.0;
                    double posY = g / 255.0;
                    double posZ = b / 255.0;

                    DrawTinyBox(posX, posY, posZ, System.Windows.Media.Color.FromRgb(r, g, b));
                }
            }
        }

        private void DrawAxes()
        {
            double length = 1.1;
            double thickness = 0.003;

            // منرسم الخطوط على انها موشور مستطيلات رفيع كتير وطويل
            DrawAxisBox(length, thickness, thickness, length / 2, 0, 0, System.Windows.Media.Colors.Red);    // X-Axis
            DrawAxisBox(thickness, length, thickness, 0, length / 2, 0, System.Windows.Media.Colors.Lime);   // Y-Axis
            DrawAxisBox(thickness, thickness, length, 0, 0, length / 2, System.Windows.Media.Colors.DodgerBlue); // Z-Axis

            // نقطة بيضا بمبدأ الاحداثيات
            DrawAxisBox(0.015, 0.015, 0.015, 0, 0, 0, System.Windows.Media.Colors.White);
        }

        private void DrawAxisBox(double sizeX, double sizeY, double sizeZ, double centerX, double centerY, double centerZ, System.Windows.Media.Color color)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            double hX = sizeX / 2.0;
            double hY = sizeY / 2.0;
            double hZ = sizeZ / 2.0;

            // نولد 8 زوايا للخط
            Point3D[] corners = new Point3D[]
            {
                new Point3D(centerX - hX, centerY - hY, centerZ - hZ),
                new Point3D(centerX + hX, centerY - hY, centerZ - hZ),
                new Point3D(centerX + hX, centerY + hY, centerZ - hZ),
                new Point3D(centerX - hX, centerY + hY, centerZ - hZ),
                new Point3D(centerX - hX, centerY - hY, centerZ + hZ),
                new Point3D(centerX + hX, centerY - hY, centerZ + hZ),
                new Point3D(centerX + hX, centerY + hY, centerZ + hZ),
                new Point3D(centerX - hX, centerY + hY, centerZ + hZ)
            };

            foreach (var p in corners) mesh.Positions.Add(p);

            // ربط النقاط مشان نعمل 12 مثلث للوجوه ال6
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
                new Point3D(x - s, y - s, z - s),
                new Point3D(x + s, y - s, z - s),
                new Point3D(x + s, y + s, z - s),
                new Point3D(x - s, y + s, z - s),
                new Point3D(x - s, y - s, z + s),
                new Point3D(x + s, y - s, z + s),
                new Point3D(x + s, y + s, z + s),
                new Point3D(x - s, y + s, z + s)
            };

            foreach (var p in corners) mesh.Positions.Add(p);

            // مصفوفة الأوجه
            int[] indices = { 4, 5, 6, 6, 7, 4, 1, 0, 3, 3, 2, 1, 0, 4, 7, 7, 3, 0, 5, 1, 2, 2, 6, 5, 3, 7, 6, 6, 2, 3, 4, 0, 1, 1, 5, 4 };
            foreach (int index in indices) mesh.TriangleIndices.Add(index);

            System.Windows.Media.SolidColorBrush brush = new System.Windows.Media.SolidColorBrush(color);
            DiffuseMaterial material = new DiffuseMaterial(brush); // Diffuse allows it to catch light/shadows

            GeometryModel3D model = new GeometryModel3D(mesh, material);
            ModelGroup3D.Children.Add(model);
        }

            // سلايدر بحرك الكاميرة بشكل دائري
        private void CameraSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || Camera3D == null) return;

            // عم نحول درجات الزوايا للراديان
            double angleInRadians = e.NewValue * (System.Math.PI / 180.0);

            double radius = 2.5;
            double centerX = 0.5;
            double centerZ = 0.5;

            // نحدد موقع الكاميرة على حفة الدائرة
            double newX = centerX + System.Math.Cos(angleInRadians) * radius;
            double newZ = centerZ + System.Math.Sin(angleInRadians) * radius;

            Camera3D.Position = new Point3D(newX, 1.5, newZ);

            // منجبر الكاميرة تضل عم تبحلق على المركز
            Camera3D.LookDirection = new Vector3D(centerX - newX, 0.5 - 1.5, centerZ - newZ);
        }
    }
}