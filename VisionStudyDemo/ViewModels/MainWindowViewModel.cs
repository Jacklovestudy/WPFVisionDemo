using Prism.Mvvm;
using Prism.Commands;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VisionStudyDemo.AsyncCommands;
using VisionStudyDemo.Imaging;

namespace VisionStudyDemo.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public ICommand TestCommand { get; }
        public DelegateCommand SelectImageCommand { get; }
        public DelegateCommand ConvertToGrayCommand { get; }
        public DelegateCommand ShowLoadedOriginalCommand { get; }
        private BitmapSource? _loadedOriginal;
        private string _loadedFileName = "";

        private Stretch _imageStretch = Stretch.Fill;
        public Stretch ImageStretch
        {
            get => _imageStretch;
            private set => SetProperty(ref _imageStretch, value);
        }

        private BitmapScalingMode _imageScalingMode = BitmapScalingMode.NearestNeighbor;
        public BitmapScalingMode ImageScalingMode
        {
            get => _imageScalingMode;
            private set => SetProperty(ref _imageScalingMode, value);
        }

        private string _title = "VisionStudyDemo";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _test = "点击测试按钮：框出面积最大的连通域";
        public string Test
        {
            get => _test;
            set => SetProperty(ref _test, value);
        }

        private BitmapSource? _grayImage;
        public BitmapSource? GrayImage
        {
            get => _grayImage;
            set => SetProperty(ref _grayImage, value);
        }

        // 与 XAML 共用显示尺寸，缩放计算和图片大小保持一致（WPF 布局单位）。
        public double ImageDisplayWidth => 400;
        public double ImageDisplayHeight => 320;

        private double _boxLeft;
        public double BoxLeft
        {
            get => _boxLeft;
            set => SetProperty(ref _boxLeft, value);
        }

        private double _boxTop;
        public double BoxTop
        {
            get => _boxTop;
            set => SetProperty(ref _boxTop, value);
        }

        private double _boxWidth;
        public double BoxWidth
        {
            get => _boxWidth;
            set => SetProperty(ref _boxWidth, value);
        }

        private double _boxHeight;
        public double BoxHeight
        {
            get => _boxHeight;
            set => SetProperty(ref _boxHeight, value);
        }

        // 质心标记圆点：Left、Top 是外接方框的左上角，不是圆心。
        public double DotDiameter => 8;

        private double _dotLeft;
        public double DotLeft
        {
            get => _dotLeft;
            set => SetProperty(ref _dotLeft, value);
        }

        private double _dotTop;
        public double DotTop
        {
            get => _dotTop;
            set => SetProperty(ref _dotTop, value);
        }

        private Visibility _dotVisibility = Visibility.Collapsed;
        public Visibility DotVisibility
        {
            get => _dotVisibility;
            set => SetProperty(ref _dotVisibility, value);
        }

        // 学习约定：x 是行，y 是列。
        private readonly byte[,] _gray =
        {
            { 220, 220, 220, 220, 220 },
            { 220,  30,  30,  30, 220 },
            { 220,  30,  30,  30, 220 },
            { 220, 220, 220, 220, 220 }
        };

        public MainWindowViewModel()
        {
            TestCommand = AsyncCommand.Create(TestAsync);
            SelectImageCommand = new DelegateCommand(SelectImage);
            ConvertToGrayCommand = new DelegateCommand(ShowLoadedGray, () => _loadedOriginal != null);
            ShowLoadedOriginalCommand = new DelegateCommand(ShowLoadedOriginal, () => _loadedOriginal != null);
        }

        /// <summary>选择文件；取消时保持当前图像。</summary>
        private void SelectImage()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择一张图片",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.gif|所有文件|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog() != true)
                return;

            try
            {
                LoadImageFile(dialog.FileName);
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException
                or NotSupportedException or System.IO.FileFormatException or ArgumentException)
            {
                Test = $"图片加载失败：{ex.Message}";
            }
        }

        /// <summary>加载成功后才替换原图；保留原图供反复切换和灰度转换。</summary>
        private void LoadImageFile(string path)
        {
            BitmapSource image = ImageFileLoader.Load(path);
            _loadedOriginal = image;
            _loadedFileName = System.IO.Path.GetFileName(path);
            ConvertToGrayCommand.RaiseCanExecuteChanged();
            ShowLoadedOriginalCommand.RaiseCanExecuteChanged();
            ShowLoadedOriginal();
        }

        private void ShowLoadedOriginal()
        {
            if (_loadedOriginal != null)
                DisplayPhoto(_loadedOriginal, "原图");
        }

        private void ShowLoadedGray()
        {
            if (_loadedOriginal != null)
                DisplayPhoto(ImageFileLoader.ToGray(_loadedOriginal), "灰度图");
        }

        private void DisplayPhoto(BitmapSource image, string description)
        {
            ClearRegionBox();
            ImageStretch = Stretch.Uniform; // 照片等比例显示，避免杯子被拉伸
            ImageScalingMode = BitmapScalingMode.HighQuality;
            GrayImage = image; // 现有显示属性也可承载彩色 BitmapSource
            Test = $"{description}：{_loadedFileName}，{image.PixelWidth} × {image.PixelHeight}，格式：{image.Format}";
        }

        // 测试入口：每次选择一个实验，具体功能各有独立方法。
        private Task TestAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[,] gray = CreateLessonImage();

            // 复习其他功能时，用下面任意一行替换当前调用：
            // ShowOriginal(gray);      //原始灰度图、尺寸与指定像素
            // ShowBrightness(gray, 100); // -100 为变暗  亮度调整，观察上下限截断
            // ShowInverted(gray);   //灰度反转
            // ShowBinary(gray, 100);  //二值化与白色像素计数
            //ShowConnectedComponents(gray, 100); // 查看全部连通域，并框出最大的一块
            // ShowAreaFiltered(gray, 100, 2); // 只保留面积大于等于 2 的块
            ShowCropped(gray);
            // 当前是小数组同步实验，没有需要 await 的操作。
            return Task.CompletedTask;
        }
        #region 功能区
        private void ShowCropped(byte[,] gray)
        {
            byte[,] cropped = GrayImageProcessor.Crop(
                gray,
                startRow: 1,
                startCol: 1,
                roiWidth: 3,
                roiHeight: 2);

            byte[] binary = GrayImageProcessor.Threshold(cropped, 100);

            List<ConnectedRegion> regions =
     GrayImageProcessor.GetFourConnectedRegions(
         binary,
         cropped.GetLength(1),  // 小图宽度：3
         cropped.GetLength(0));// 小图高度：2

            DisplayImage(binary, cropped);

            Test = string.Join("；", regions.Select(region =>
                $"面积={region.Area}，" +
                $"起始行索引={region.MinRow}，列索引={region.MinCol}，" +
                $"宽={region.Width}，高={region.Height}"));
        }
        /// <summary>复制原图，将实验图的右上角设为黑色。</summary>
        private byte[,] CreateLessonImage()
        {
            byte[,] gray = (byte[,])_gray.Clone();
            gray[0, gray.GetLength(1) - 1] = 0;
            return gray;
        }

        /// <summary>实验 1：原始灰度图、尺寸与指定像素。</summary>
        private void ShowOriginal(byte[,] gray)
        {
            DisplayImage(GrayImageProcessor.Flatten(gray), gray);
            Test = $"高度：{gray.GetLength(0)}，宽度：{gray.GetLength(1)}，行索引2、列索引1的灰度：{gray[2, 1]}";
        }

        /// <summary>实验 2：亮度调整，观察上下限截断。</summary>
        private void ShowBrightness(byte[,] gray, int offset)
        {
            DisplayImage(GrayImageProcessor.AdjustBrightness(gray, offset), gray);
            Test = $"亮度偏移：{offset}，输出限制到 0～255";
        }

        /// <summary>实验 3：灰度反转。</summary>
        private void ShowInverted(byte[,] gray)
        {
            DisplayImage(GrayImageProcessor.Invert(gray), gray);
            Test = "灰度反转：输出 = 255 - 原灰度";
        }

        /// <summary>实验 4：二值化与白色像素计数。</summary>
        private void ShowBinary(byte[,] gray, int threshold)
        {
            byte[] pixels = GrayImageProcessor.Threshold(gray, threshold);
            int count = GrayImageProcessor.CountForeground(pixels);
            DisplayImage(pixels, gray);
            Test = $"阈值：{threshold}（严格小于），前景像素数：{count}";
        }

        /// <summary>实验 5：先二值化，再搜索四邻域连通域。</summary>
        private void ShowConnectedComponents(byte[,] gray, int threshold)
        {
            byte[] pixels = GrayImageProcessor.Threshold(gray, threshold);
            int count = GrayImageProcessor.CountForeground(pixels);
            List<ConnectedRegion> regions = GrayImageProcessor.GetFourConnectedRegions(
                pixels, gray.GetLength(1), gray.GetLength(0));

            DisplayImage(pixels, gray);
            // Test = $"阈值：{threshold}，前景像素：{count}，四邻域连通域：{regions.Count}，面积：{string.Join("、", regions.Select(region => region.Area))}";
            Test = string.Join("；", regions.Select((region, index) =>
    $"第{index + 1}块：" +
    $"面积={region.Area}，" +
    $"左上角=第{region.MinRow}行、第{region.MinCol}列，" +
    $"宽={region.Width}，高={region.Height}"));
            if (regions.Count == 0)
                Test = "没有找到前景连通域";
            ShowLargestRegionBox(regions, gray.GetLength(1), gray.GetLength(0));
        }

        /// <summary>实验 6：按面积去除小块，显示筛选后的二值图。</summary>
        private void ShowAreaFiltered(byte[,] gray, int threshold, int minArea)
        {
            int width = gray.GetLength(1);
            int height = gray.GetLength(0);
            byte[] binary = GrayImageProcessor.Threshold(gray, threshold);
            byte[] filtered = GrayImageProcessor.FilterByArea(binary, width, height, minArea);

            // 再统计输出图，确认实际保留了哪些连通域。
            List<ConnectedRegion> remaining = GrayImageProcessor.GetFourConnectedRegions(filtered, width, height);
            DisplayImage(filtered, gray);
            ShowLargestRegionBox(remaining, width, height);
            Test = $"最小面积：{minArea}，保留：{remaining.Count} 块，面积：{string.Join("、", remaining.Select(region => region.Area))}，前景像素：{GrayImageProcessor.CountForeground(filtered)}";
        }

        /// <summary>只框面积最大的一块；同面积时选逐行扫描先发现的块。</summary>
        private void ShowLargestRegionBox(List<ConnectedRegion> regions, int imageWidth, int imageHeight)
        {
            var largest = regions.OrderByDescending(region => region.Area).FirstOrDefault();
            if (largest == null || largest.Area == 0)
            {
                ClearRegionBox();
                return;
            }

            ShowRegionBox(largest, imageWidth, imageHeight);
            ShowRegionCentroid(largest, imageWidth, imageHeight);
        }

        /// <summary>图像坐标转成叠加层位置：列决定 Left，行决定 Top。</summary>
        private void ShowRegionBox(ConnectedRegion region, int imageWidth, int imageHeight)
        {
            double scaleX = ImageDisplayWidth / imageWidth;
            double scaleY = ImageDisplayHeight / imageHeight;
            BoxLeft = region.MinCol * scaleX;
            BoxTop = region.MinRow * scaleY;
            BoxWidth = region.Width * scaleX;
            BoxHeight = region.Height * scaleY;
        }

        /// <summary>用直径为 8 的红点标出同一个连通域的质心。</summary>
        private void ShowRegionCentroid(ConnectedRegion region, int imageWidth, int imageHeight)
        {
            double pixelWidth = ImageDisplayWidth / imageWidth;
            double pixelHeight = ImageDisplayHeight / imageHeight;

            // 第一步：平均左/上边缘位置 + 一个像素宽/高的一半，得到质心显示位置。
            // 使用所有像素的平均索引，不能用外接矩形的中心代替质心。
            double centerLeft = region.CenterCol * pixelWidth + pixelWidth / 2;
            double centerTop = region.CenterRow * pixelHeight + pixelHeight / 2;

            // 第二步：已知圆心，减去圆点自身宽/高的一半，得到摆放用的左上角。
            DotLeft = centerLeft - DotDiameter / 2;
            DotTop = centerTop - DotDiameter / 2;
            DotVisibility = Visibility.Visible;
        }

        /// <summary>清除旧框和质心点，防止无目标或切换到其他实验时残留。</summary>
        private void ClearRegionBox()
        {
            BoxLeft = BoxTop = BoxWidth = BoxHeight = 0;
            DotLeft = DotTop = 0;
            DotVisibility = Visibility.Collapsed;
        }

        /// <summary>统一显示出口：一维像素数组转换为 WPF 图像。</summary>
        private void DisplayImage(byte[] pixels, byte[,] gray)
        {
            ClearRegionBox();
            ImageStretch = Stretch.Fill;
            ImageScalingMode = BitmapScalingMode.NearestNeighbor;
            int width = gray.GetLength(1);
            int height = gray.GetLength(0);
            GrayImage = BitmapSource.Create(
                width, height,
                96, 96,               // DPI：不改变图像的像素数量
                PixelFormats.Gray8,   // 单通道，每个像素 1 字节
                null,
                pixels,
                width);               // stride：紧密排列的一行占 width 字节
        }
        #endregion


    }
}
