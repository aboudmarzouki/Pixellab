using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PixelLab
{
    public partial class Form1 : Form
    {
        private Bitmap originalImage;

        public Form1()
        {
            InitializeComponent();
            SetupDragAndDrop();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void picDisplay_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadImage(openFileDialog.FileName);
            }
        }
        private void LoadImage(string filePath)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (Bitmap tempImage = new Bitmap(stream))
                    {
                        originalImage = new Bitmap(tempImage.Width, tempImage.Height, tempImage.PixelFormat);

                        using (Graphics g = Graphics.FromImage(originalImage))
                        {
                            g.DrawImage(tempImage, 0, 0);
                        }
                    }
                } 
                picDisplay.Image = originalImage;

                UpdateImageInfo(filePath);

            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل الصورة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdateImageInfo(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            lblName.Text = fileInfo.Name; 

            lblFormat.Text = fileInfo.Extension.Replace(".", "").ToUpper();

            lblDimensions.Text = $"{originalImage.Width} × {originalImage.Height} بكسل";

            lblSize.Text = (fileInfo.Length / 1024.0).ToString("F2");
        }
        private void SetupDragAndDrop()
        {
            picDisplay.AllowDrop = true;

            // ربط الأحداث بالدوال المناسبة
            picDisplay.DragEnter += PicDisplay_DragEnter;
            picDisplay.DragDrop += PicDisplay_DragDrop;
        }
        private void PicDisplay_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy; // يظهر علامة النسخ (+) بجانب الماوس
            }
            else
            {
                e.Effect = DragDropEffects.None; // يظهر علامة المنع
            }
        }
        private void PicDisplay_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                LoadImage(files[0]);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            if (originalImage != null)
            {
                picDisplay.Image = originalImage;

                MessageBox.Show("تمت إعادة ضبط الصورة إلى حالتها الأصلية.", "إعادة ضبط", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("لا توجد صورة مفتوحة لإعادة ضبطها!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (picDisplay.Image == null)
            {
                MessageBox.Show("لا توجد صورة لحفظها! افتح صورة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "صورة PNG|*.png|صورة JPEG|*.jpg|صورة BMP|*.bmp";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Png;

                    if (saveFileDialog.FilterIndex == 2)
                    {
                        format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    }
                    else if (saveFileDialog.FilterIndex == 3)
                    {
                        format = System.Drawing.Imaging.ImageFormat.Bmp;
                    }

                    picDisplay.Image.Save(saveFileDialog.FileName, format);

                    MessageBox.Show("تم حفظ الصورة بنجاح!", "حفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ أثناء الحفظ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}