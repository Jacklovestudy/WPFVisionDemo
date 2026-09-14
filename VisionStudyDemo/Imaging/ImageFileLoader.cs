using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VisionStudyDemo.Imaging
{
    public static class ImageFileLoader
    {
        /// <summary>照片转 Gray8，再交给二维数组中值算法；不修改原图。</summary>
        public static BitmapSource MedianFilter3x3(BitmapSource image)
        {
            ArgumentNullException.ThrowIfNull(image);
            BitmapSource grayImage = ToGray(image);
            int width = grayImage.PixelWidth;
            int height = grayImage.PixelHeight;
            byte[] pixels = new byte[checked(width * height)];
            grayImage.CopyPixels(pixels, width, 0);

            // 一维像素数据 → [行, 列]，便于独立学习邻域算法。
            byte[,] gray = new byte[height, width];
            for (int x = 0; x < height; x++)
                for (int y = 0; y < width; y++)
                    gray[x, y] = pixels[x * width + y];

            byte[,] filtered = GrayImageProcessor.MedianFilter3x3(gray);
            // 二维结果重新平铺后生成 WPF 灰度图片。
            var result = BitmapSource.Create(width, height, 96, 96,
                PixelFormats.Gray8, null, GrayImageProcessor.Flatten(filtered), width);
            result.Freeze();
            return result;
        }

        /// <summary>照片转灰度，再调用独立的二维数组均值滤波算法。</summary>
        public static BitmapSource MeanFilter3x3(BitmapSource image)
        {
            ArgumentNullException.ThrowIfNull(image);
            BitmapSource grayImage = ToGray(image);
            int width = grayImage.PixelWidth;
            int height = grayImage.PixelHeight;
            byte[] pixels = new byte[checked(width * height)];
            grayImage.CopyPixels(pixels, width, 0);

            // WPF 读出的是一维数组；转成课程使用的 [行, 列] 方便理解邻域。
            byte[,] gray = new byte[height, width];
            for (int x = 0; x < height; x++)
                for (int y = 0; y < width; y++)
                    gray[x, y] = pixels[x * width + y];

            byte[,] filtered = GrayImageProcessor.MeanFilter3x3(gray);
            // 算法处理完后重新平铺，交给 WPF 显示。
            var result = BitmapSource.Create(width, height, 96, 96,
                PixelFormats.Gray8, null, GrayImageProcessor.Flatten(filtered), width);
            result.Freeze();
            return result;
        }
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

        /// <summary>
        /// 照片二值化：先转灰度，再将小于阈值的像素设为白色，其余设为黑色。
        /// 返回新的图片，不修改原图。等于阈值时输出黑色。
        /// </summary>
        public static BitmapSource ToBinary(BitmapSource image, int threshold)
        {
            ArgumentNullException.ThrowIfNull(image);
            if (threshold < 0 || threshold > 255)
                throw new ArgumentOutOfRangeException(nameof(threshold), "阈值必须在 0～255 之间。");

            // 1. 统一得到 Gray8：每个像素只有一个灰度字节，不再是四字节 BGRA。
            BitmapSource grayImage = ToGray(image);
            int width = grayImage.PixelWidth;
            int height = grayImage.PixelHeight;

            // 2. 复制灰度数据。紧密排列的 Gray8 每行正好占 width 字节。
            byte[] pixels = new byte[checked(width * height)];
            grayImage.CopyPixels(pixels, width, 0);

            // 3. 各像素独立判断，所以一层循环即可，不需要行列坐标。
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = pixels[i] < threshold ? (byte)255 : (byte)0;
            }

            // 4. 输出仍用 Gray8 存储，但数组中的值现在只有 0 和 255。
            var result = BitmapSource.Create(width, height, 96, 96,
                PixelFormats.Gray8, null, pixels, width);
            result.Freeze(); // 设为只读，并允许跨线程共享；不是保存文件。
            return result;
        }

        /// <summary>接收一张image，返回转换后的灰度图片</summary>
        public static BitmapSource ToGray(BitmapSource image)
        {
            //转为1维数组 每个像素4个字节 BGRA 
            var color = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
            int width = color.PixelWidth;
            int height = color.PixelHeight;
            int stride = checked(width * 4);//获取图像的实际像素宽、高，不是界面上的显示尺寸
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
