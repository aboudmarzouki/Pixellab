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

        public MainWindow()
        {
            InitializeComponent();
            _processor = new ImageProcessor();
            UpdateChannelLabels();
        }

        // السحب والإفلات
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

            // إيقاف الأحداث مؤقتاً مشان ما يعمل تحديث للصورة بشكل جنوني أثناء ضبط السلايدرات برمجياً
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

            _processor.ApplySystemFilters(_currentColorSystem, v1, v2, v3, v4);

            // التحديث اللحظي للـ 3D
            Update3DModel();
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

    //     private void UpdateColorInfoPanel(System.Windows.Media.Color c)
    //     {
    //         ColorInfoPanel.Visibility = Visibility.Visible;
    //         PickedColorSwatch.Background = new System.Windows.Media.SolidColorBrush(c);
    //         TxtPickedHex.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    //         TxtPickedRGB.Text = $"RGB: ({c.R}, {c.G}, {c.B})";

    //         var hsv = Models.ColorConverter.RgbToHsv(c.R, c.G, c.B);
    //         TxtPickedHSV.Text = $"HSV: ({System.Math.Round(hsv.H)}°, {System.Math.Round(hsv.S * 100)}%, {System.Math.Round(hsv.V * 100)}%)";

    //         var lab = Models.ColorConverter.RgbToLab(c.R, c.G, c.B);
    //         TxtPickedLAB.Text = $"LAB: ({System.Math.Round(lab.L)}, {System.Math.Round(lab.A)}, {System.Math.Round(lab.B_channel)})";

    //         var cmyk = Models.ColorConverter.RgbToCmyk(c.R, c.G, c.B);
    //         TxtPickedCMYK.Text = $"CMYK: ({System.Math.Round(cmyk.C * 100)}%, {System.Math.Round(cmyk.M * 100)}%, {System.Math.Round(cmyk.Y * 100)}%, {System.Math.Round(cmyk.K * 100)}%)";

    //         // مزامنة السلايدرات مع اللون المختار بشكل آمن
    //         DetachSliderEvents();

    //         if (_currentColorSystem == "RGB")
    //         {
    //             SldChannel1.Value = c.R; SldChannel2.Value = c.G; SldChannel3.Value = c.B;
    //         }
    //         else if (_currentColorSystem == "HSV")
    //         {
    //             SldChannel1.Value = hsv.H; SldChannel2.Value = hsv.S * 100; SldChannel3.Value = hsv.V * 100;
    //         }
    //         else if (_currentColorSystem == "LAB")
    //         {
    //             SldChannel1.Value = lab.L; SldChannel2.Value = lab.A; SldChannel3.Value = lab.B_channel;
    //         }
    //         else if (_currentColorSystem == "CMYK")
    //         {
    //             SldChannel1.Value = cmyk.C * 100; SldChannel2.Value = cmyk.M * 100;
    //             SldChannel3.Value = cmyk.Y * 100; SldChannel4.Value = cmyk.K * 100;
    //         }
    //         else if (_currentColorSystem == "YCbCr" || _currentColorSystem == "YUV")
    //         {
    //             var ycbcr = Models.ColorConverter.RgbToYCbCr(c.R, c.G, c.B);
    //             SldChannel1.Value = ycbcr.Y; SldChannel2.Value = ycbcr.Cb; SldChannel3.Value = ycbcr.Cr;
    //         }

    //         AttachSliderEvents();

    //         // تحديث الصورة بعد ما السلايدرات اخدت القيم الجديدة
    //         double v1 = ChkChannel1.IsChecked == true ? SldChannel1.Value : 0;
    //         double v2 = ChkChannel2.IsChecked == true ? SldChannel2.Value : 0;
    //         double v3 = ChkChannel3.IsChecked == true ? SldChannel3.Value : 0;
    //         double v4 = ChkChannel4.IsChecked == true ? SldChannel4.Value : 0;
    //         _processor.ApplySystemFilters(_currentColorSystem, v1, v2, v3, v4);
    //     }
    // }
}