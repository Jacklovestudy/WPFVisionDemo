using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VisionStudyDemo.Imaging
{
    public static class ImageFileLoader
    {
        /// <summary>完整读取图片后关闭文件，避免图片一直被程序锁定。</summary>
        public static BitmapSource Load(string path)
        {
            using var stream = File.OpenRead(path);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        /// <summary>统一为 BGRA，再按学过的 RGB 权重计算灰度。</summary>
        public static BitmapSource ToGray(BitmapSource image)
        {
            var color = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
            int width = color.PixelWidth;
            int height = color.PixelHeight;
            int stride = checked(width * 4);
            byte[] source = new byte[checked(stride * height)];
            color.CopyPixels(source, stride, 0);
            byte[] gray = new byte[checked(width * height)];

            for (int x = 0; x < height; x++) // x 行，y 列
            {
                for (int y = 0; y < width; y++)
                {
                    int index = x * stride + y * 4;
                    byte b = source[index];
                    byte g = source[index + 1];
                    byte r = source[index + 2];
                    double alpha = source[index + 3] / 255.0;
                    // 透明图片按白色背景合成，再转为不透明的 Gray8。
                    double value = (0.299 * r + 0.587 * g + 0.114 * b) * alpha + 255 * (1 - alpha);
                    gray[x * width + y] = (byte)Math.Clamp((int)Math.Round(value), 0, 255);
                }
            }

            var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, gray, width);
            result.Freeze();
            return result;
        }
    }
}
