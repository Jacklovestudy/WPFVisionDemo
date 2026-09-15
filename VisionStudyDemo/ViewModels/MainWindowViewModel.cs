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
        public DelegateCommand ContourDemoCommand { get; }
        public DelegateCommand PhotoContourCommand { get; }
        public DelegateCommand SelectTargetCommand { get; }
        public DelegateCommand NextCandidateCommand { get; }
        private int _candidateIndex = -1;
        private int _candidateCount;
        private (TargetCriteria? Criteria, Int32Rect? Roi, MorphologyOperation Operation, int Size) _candidateContext;
        public DelegateCommand RawBinaryPreviewCommand { get; }
        public DelegateCommand OpeningPreviewCommand { get; }
        public DelegateCommand ClosingPreviewCommand { get; }
        public string PhotoMorphologySize { get; set; } = "3";
        private MorphologyOperation _photoMorphology = MorphologyOperation.None;
        private string _morphologyInfo = "当前照片处理：原始二值图。开/闭运算预览也会切换后续轮廓、筛选使用的处理方式。";
        public string MorphologyInfo { get => _morphologyInfo; private set => SetProperty(ref _morphologyInfo,value); }
        // 文本参数在点击时统一解析，输入非法内容时不沿用上次的有效数字。
        public string FilterMinArea { get; set; } = "100";
        public string FilterMinRatio { get; set; } = "0.4";
        public string FilterMaxRatio { get; set; } = "0.7";
        public string FilterMinFill { get; set; } = "0.6";
        public string FilterExpectedRow { get; set; } = "";
        public string FilterExpectedCol { get; set; } = "";
        public string FilterMaxDistance { get; set; } = "100";
        private string _filterInfo = "预期行、列留空时使用处理范围中心；参数是练习初值，需要按图片调整。";
        public string FilterInfo { get => _filterInfo; private set => SetProperty(ref _filterInfo, value); }
        private string _photoTargetLabel = "最大白色区域";
        public DelegateCommand ContourNextCommand { get; }
        private List<(int Row, int Col)> _photoContour = new();
        private int _photoContourIndex;
        private int _photoContourArea;
        private string _photoContourScope = "全图";
        private string _photoContourWarning = "";
        private PointCollection _photoContourPoints = new();
        public PointCollection PhotoContourPoints
        {
            get => _photoContourPoints;
            private set => SetProperty(ref _photoContourPoints, value);
        }
        private int _traceStep = -1;
        private Visibility _traceVisibility = Visibility.Collapsed;
        public Visibility TraceVisibility
        {
            get => _traceVisibility;
            private set => SetProperty(ref _traceVisibility, value);
        }
        public DelegateCommand OpeningDemoCommand { get; }
        public DelegateCommand ClosingDemoCommand { get; }
        private string? _morphologyMode;
        private int _morphologyStep = -1;
        public DelegateCommand SelectImageCommand { get; }
        public DelegateCommand ConvertToGrayCommand { get; }
        public DelegateCommand ConvertToBinaryCommand { get; }
        public DelegateCommand MeanFilterCommand { get; }
        public DelegateCommand MeanBinaryCommand { get; }
        public DelegateCommand MedianFilterCommand { get; }
        public DelegateCommand MedianBinaryCommand { get; }
        public DelegateCommand EdgeCommand { get; }
        public DelegateCommand ShowLoadedOriginalCommand { get; }
        private BitmapSource? _loadedOriginal;
        private string _loadedFileName = "";
        public DelegateCommand SelectRoiCommand { get; }
        public DelegateCommand RoiBinaryCommand { get; }
        public DelegateCommand ClearRoiCommand { get; }
        private Int32Rect? _photoRoi;
        private Point? _roiDragStart;
        private Rect _roiImageBounds;
        private Rect _roiBounds;
        public Rect RoiBounds { get => _roiBounds; private set => SetProperty(ref _roiBounds, value); }
        private bool _isSelectingRoi;
        public bool IsSelectingRoi { get => _isSelectingRoi; private set => SetProperty(ref _isSelectingRoi, value); }
        private string _roiInfo = "选择图片后，点击“框选 ROI”，拖动包含杯身和杯把的矩形。";
        public string RoiInfo { get => _roiInfo; private set => SetProperty(ref _roiInfo, value); }

        private void ClearPhotoRoi()
        {
            _candidateCount = 0;
            _candidateIndex = -1;
            NextCandidateCommand.RaiseCanExecuteChanged();
            _photoRoi = null;
            _roiDragStart = null;
            RoiBounds = new Rect(0, 0, 0, 0);
            IsSelectingRoi = false;
            RoiInfo = "点击“框选 ROI”，在原图上拖动；反向拖动也可以。";
            RoiBinaryCommand.RaiseCanExecuteChanged();
        }

        private void StartRoiSelection()
        {
            ShowLoadedOriginal();
            IsSelectingRoi = true;
            RoiInfo = "在原图内按住左键拖动，松开完成；Esc 取消。";
        }

        public bool BeginRoiDrag(Point point, Size viewport)
        {
            if (!IsSelectingRoi || _loadedOriginal == null || viewport.Width <= 0 || viewport.Height <= 0)
                return false;
            _roiImageBounds = RoiCoordinates.GetImageBounds(_loadedOriginal, viewport);
            if (!_roiImageBounds.Contains(point)) return false; // 留白区不是图像
            _photoRoi = null;
            RoiBinaryCommand.RaiseCanExecuteChanged();
            _roiDragStart = point;
            RoiBounds = new Rect(point, point);
            return true;
        }

        public void UpdateRoiDrag(Point point)
        {
            if (_roiDragStart is Point start)
                RoiBounds = new Rect(start, RoiCoordinates.Clamp(point, _roiImageBounds));
        }

        public void CompleteRoiDrag(Point point)
        {
            if (_roiDragStart == null || _loadedOriginal == null) return;
            UpdateRoiDrag(point);
            _roiDragStart = null;
            // 单击或过小拖动不生成 ROI。
            if (RoiBounds.Width < 2 || RoiBounds.Height < 2)
            {
                RoiBounds = new Rect(0, 0, 0, 0);
                RoiInfo = "范围太小，请重新拖出一个矩形。";
                return;
            }
            Int32Rect roi = RoiCoordinates.ToPixels(RoiBounds, _roiImageBounds,
                _loadedOriginal.PixelWidth, _loadedOriginal.PixelHeight);
            if (roi.Width <= 0 || roi.Height <= 0) return;
            _photoRoi = roi;
            RoiBounds = RoiCoordinates.ToDisplay(roi, _roiImageBounds,
                _loadedOriginal.PixelWidth, _loadedOriginal.PixelHeight);
            RoiInfo = $"原图 ROI：起始行 {roi.Y}，起始列 {roi.X}，宽 {roi.Width}，高 {roi.Height}。可点击“提取照片轮廓”只处理框内区域，或点击 ROI 二值化。";
            RoiBinaryCommand.RaiseCanExecuteChanged();
        }

        public void CancelRoiDrag() => ClearPhotoRoi();

        /// <summary>从保存的原始照片裁剪；不能从当前黑白预览上再裁剪。</summary>
        private void ShowRoiBinary()
        {
            if (_photoRoi != null) ShowBinaryPreview(MorphologyOperation.None);
        }

        private bool TryReadMorphologySize(MorphologyOperation operation, out int size)
        {
            size=3;
            if (operation == MorphologyOperation.None) return true;
            if (int.TryParse(PhotoMorphologySize,out size) && size >= 3 && size <= 15 && size % 2 == 1) return true;
            Test = "邻域边长请输入 3～15 的奇数（例如 3、5、7）。未执行处理。";
            return false;
        }

        private static string MorphologyName(MorphologyOperation operation, int size) => operation switch
        {
            MorphologyOperation.Open => $"{size}×{size} 开运算（先腐蚀再膨胀）",
            MorphologyOperation.Close => $"{size}×{size} 闭运算（先膨胀再腐蚀）",
            _ => "原始二值图"
        };

        /// <summary>从同一原图、同一 ROI 重新计算；切换按钮不叠加上一张预览结果。</summary>
        private void ShowBinaryPreview(MorphologyOperation operation)
        {
            if (_loadedOriginal == null || !TryReadMorphologySize(operation,out int size)) return;
            Int32Rect? roi = _photoRoi;
            BitmapSource source = roi is Int32Rect rect ? new CroppedBitmap(_loadedOriginal,rect) : _loadedOriginal;
            var result = ImageFileLoader.ProcessBinary(source,operation,size);
            DisplayPhoto(result,$"{(roi == null ? "全图" : "ROI")}：{MorphologyName(operation,size)}（灰度 < 100 为白）");
            _photoMorphology=operation;
            MorphologyInfo=$"当前处理：{MorphologyName(operation,size)}。后续轮廓提取和按条件选目标使用此模式；改变边长后请重新预览。";
            // 裁剪预览坐标与原图不同：不画旧选框，但保留 ROI 供下一次比较和提取。
            _photoRoi=roi;
            RoiBinaryCommand.RaiseCanExecuteChanged();
            ImageScalingMode=BitmapScalingMode.NearestNeighbor;
            FilterInfo="当前为二值处理预览；请点击“按条件选目标”重新统计筛选结果。";
            RoiInfo=roi is Int32Rect saved
                ? $"ROI 已保留：原图起始行 {saved.Y}、列 {saved.X}，{saved.Width}×{saved.Height}。可直接切换预览或提取照片轮廓。"
                : "未框选 ROI，当前处理整张照片。可先框住杯子再比较。";
        }

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
            ContourDemoCommand = new DelegateCommand(ShowContourDemo);
            PhotoContourCommand = new DelegateCommand(ShowPhotoContour, () => _loadedOriginal != null);
            SelectTargetCommand = new DelegateCommand(() => ShowPhotoContourCore(true), () => _loadedOriginal != null);
            NextCandidateCommand = new DelegateCommand(() => ShowPhotoContourCore(true, true),
                () => _candidateCount > 1 || (_candidateCount == 1 && _candidateIndex < 0));
            RawBinaryPreviewCommand = new DelegateCommand(() => ShowBinaryPreview(MorphologyOperation.None), () => _loadedOriginal != null);
            OpeningPreviewCommand = new DelegateCommand(() => ShowBinaryPreview(MorphologyOperation.Open), () => _loadedOriginal != null);
            ClosingPreviewCommand = new DelegateCommand(() => ShowBinaryPreview(MorphologyOperation.Close), () => _loadedOriginal != null);
            ContourNextCommand = new DelegateCommand(AdvancePhotoContour,
                () => _photoContourIndex + 1 < _photoContour.Count);
            OpeningDemoCommand = new DelegateCommand(() => ShowMorphologyDemo(true));
            ClosingDemoCommand = new DelegateCommand(() => ShowMorphologyDemo(false));
            SelectImageCommand = new DelegateCommand(SelectImage);
            ConvertToGrayCommand = new DelegateCommand(ShowLoadedGray, () => _loadedOriginal != null);
            // 尚未选择图片时，二值化按钮自动禁用。
            ConvertToBinaryCommand = new DelegateCommand(ShowLoadedBinary, () => _loadedOriginal != null);
            MeanFilterCommand = new DelegateCommand(ShowLoadedMean, () => _loadedOriginal != null);
            MeanBinaryCommand = new DelegateCommand(ShowLoadedMeanBinary, () => _loadedOriginal != null);
            MedianFilterCommand = new DelegateCommand(ShowLoadedMedian, () => _loadedOriginal != null);
            MedianBinaryCommand = new DelegateCommand(ShowLoadedMedianBinary, () => _loadedOriginal != null);
            EdgeCommand = new DelegateCommand(ShowLoadedEdges, () => _loadedOriginal != null);
            ShowLoadedOriginalCommand = new DelegateCommand(ShowLoadedOriginal, () => _loadedOriginal != null);
            SelectRoiCommand = new DelegateCommand(StartRoiSelection, () => _loadedOriginal != null);
            RoiBinaryCommand = new DelegateCommand(ShowRoiBinary, () => _loadedOriginal != null && _photoRoi != null);
            ClearRoiCommand = new DelegateCommand(ClearPhotoRoi);
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
            _photoMorphology = MorphologyOperation.None;
            MorphologyInfo="新图片：当前使用原始二值图；可选择开/闭运算预览。";
            RawBinaryPreviewCommand.RaiseCanExecuteChanged();
            OpeningPreviewCommand.RaiseCanExecuteChanged();
            ClosingPreviewCommand.RaiseCanExecuteChanged();
            _loadedFileName = System.IO.Path.GetFileName(path);
            ConvertToGrayCommand.RaiseCanExecuteChanged();
            ConvertToBinaryCommand.RaiseCanExecuteChanged();
            MeanFilterCommand.RaiseCanExecuteChanged();
            MeanBinaryCommand.RaiseCanExecuteChanged();
            MedianFilterCommand.RaiseCanExecuteChanged();
            MedianBinaryCommand.RaiseCanExecuteChanged();
            EdgeCommand.RaiseCanExecuteChanged();
            PhotoContourCommand.RaiseCanExecuteChanged();
            SelectTargetCommand.RaiseCanExecuteChanged();
            ShowLoadedOriginalCommand.RaiseCanExecuteChanged();
            SelectRoiCommand.RaiseCanExecuteChanged();
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

        /// <summary>原图 → 灰度 → 一次 3×3 均值滤波。重复点击不会累计模糊。</summary>
        private void ShowLoadedMean()
        {
            if (_loadedOriginal == null) return;
            DisplayPhoto(ImageFileLoader.MeanFilter3x3(_loadedOriginal), "全图 3×3 均值滤波");
        }

        /// <summary>整张原图 → 灰度 → 一次中值滤波，重复点击不累计处理。</summary>
        private void ShowLoadedMedian()
        {
            if (_loadedOriginal == null) return;
            DisplayPhoto(ImageFileLoader.MedianFilter3x3(_loadedOriginal), "全图 3×3 中值滤波");
        }

        /// <summary>每次从原照片计算一次，不在旧的黑白边缘图上重复处理。</summary>
        private void ShowLoadedEdges()
        {
            if (_loadedOriginal == null) return;
            const int threshold = 20; // 相邻灰度差的阈值，可降低它观察更多弱边缘。
            DisplayPhoto(ImageFileLoader.DetectEdges(_loadedOriginal, threshold),
                $"全图边缘：均值滤波后，右/下灰度差 > {threshold} 为白");
            ImageScalingMode = BitmapScalingMode.NearestNeighbor;
            RoiInfo = "白色表示明暗变化明显的位置，可能来自轮廓、文字、反光或纹理；不是完整物体识别。";
        }

        /// <summary>中值滤波后再按阈值 100 二值化，便于与其他按钮对比。</summary>
        private void ShowLoadedMedianBinary()
        {
            if (_loadedOriginal == null) return;
            BitmapSource filtered = ImageFileLoader.MedianFilter3x3(_loadedOriginal);
            DisplayPhoto(ImageFileLoader.ToBinary(filtered, 100), "全图中值滤波后二值化（灰度 < 100 为白）");
            ImageScalingMode = BitmapScalingMode.NearestNeighbor;
        }

        /// <summary>先在灰度图上滤波，再二值化；可与普通二值化按钮对比。</summary>
        private void ShowLoadedMeanBinary()
        {
            if (_loadedOriginal == null) return;
            BitmapSource filtered = ImageFileLoader.MeanFilter3x3(_loadedOriginal);
            DisplayPhoto(ImageFileLoader.ToBinary(filtered, 100), "全图均值滤波后二值化（灰度 < 100 为白）");
            ImageScalingMode = BitmapScalingMode.NearestNeighbor;
        }

        /// <summary>照片实验：每次从保存的原图计算，避免对上一次黑白结果重复处理。</summary>
        private void ShowLoadedBinary()
        {
            if (_loadedOriginal == null)
                return;

            const int threshold = 100; // 学习时可修改这里，对比不同阈值的结果。
            BitmapSource binary = ImageFileLoader.ToBinary(_loadedOriginal, threshold);
            DisplayPhoto(binary, $"二值图（灰度 < {threshold} 为白，其余为黑）");
            // 黑白图使用最近邻显示，避免缩放插值产生额外的灰色边缘。
            ImageScalingMode = BitmapScalingMode.NearestNeighbor;
        }

        private void DisplayPhoto(BitmapSource image, string description)
        {
            ClearPhotoContour();
            _traceStep = -1;
            TraceVisibility = Visibility.Collapsed;
            _morphologyMode = null; // 切换图片后，下次演示从原图开始。
            ClearPhotoRoi();
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
            ClearPhotoContour();
            _traceStep = -1;
            TraceVisibility = Visibility.Collapsed;
            _morphologyMode = null;
            ClearPhotoRoi();
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

        private void ClearPhotoContour()
        {
            _candidateCount = 0;
            _candidateIndex = -1;
            NextCandidateCommand.RaiseCanExecuteChanged();
            _photoContour = new();
            _photoContourIndex = 0;
            PhotoContourPoints = new PointCollection();
            ContourNextCommand.RaiseCanExecuteChanged();
        }

        /// <summary>有 ROI 时只在框内找最大白色区域，轮廓最终叠加回完整原图。</summary>
        private void ShowPhotoContour()
            => ShowPhotoContourCore(false);

        /// <summary>输入坐标是处理范围内的像素索引；ROI 模式下不是原图坐标。</summary>
        private bool TryReadTargetCriteria(int width, int height, out TargetCriteria? criteria)
        {
            criteria = null;
            if (!int.TryParse(FilterMinArea, out int area) || area < 1 ||
                !double.TryParse(FilterMinRatio, out double minRatio) || !double.IsFinite(minRatio) || minRatio <= 0 ||
                !double.TryParse(FilterMaxRatio, out double maxRatio) || !double.IsFinite(maxRatio) || maxRatio < minRatio || maxRatio > 1e9 ||
                !double.TryParse(FilterMinFill, out double fill) || !double.IsFinite(fill) || fill < 0 || fill > 1 ||
                !double.TryParse(FilterMaxDistance, out double distance) || !double.IsFinite(distance) || distance < 0 || distance > 1e9)
            {
                FilterInfo = "请输入有效参数：面积为正整数，宽高比 0 < 下限 ≤ 上限，填充率 0～1，距离为非负数（≤10亿）。";
                return false;
            }
            double row = (height - 1) / 2.0, col = (width - 1) / 2.0;
            // 空白分别使用该方向的中心索引，不要求用户换 ROI 后重新计算。
            if ((!string.IsNullOrWhiteSpace(FilterExpectedRow) && !double.TryParse(FilterExpectedRow, out row)) ||
                (!string.IsNullOrWhiteSpace(FilterExpectedCol) && !double.TryParse(FilterExpectedCol, out col)) ||
                !double.IsFinite(row) || !double.IsFinite(col) || row < 0 || row > height - 1 || col < 0 || col > width - 1)
            {
                FilterInfo = $"预期位置须在当前处理范围内：行 0～{height - 1}，列 0～{width - 1}；留空自动取中心。";
                return false;
            }
            criteria = new(area,minRatio,maxRatio,fill,row,col,distance);
            return true;
        }

        private void ShowPhotoContourCore(bool useFilters, bool nextCandidate = false)
        {
            if (_loadedOriginal == null) return;
            if (!TryReadMorphologySize(_photoMorphology,out int morphologySize))
            {
                ClearPhotoContour();
                ClearRegionBox();
                return;
            }
            const int threshold = 100; // 与照片二值化规则一致：小于 100 为白。
            // 显示入口会清除选择，必须先保存本次 ROI；裁剪始终来自原图。
            Int32Rect? selectedRoi = _photoRoi;
            BitmapSource source = _loadedOriginal;
            if (selectedRoi is Int32Rect selected)
            {
                source = new CroppedBitmap(_loadedOriginal, selected);
                source.Freeze();
            }
            TargetCriteria? criteria = null;
            if (useFilters && !TryReadTargetCriteria(source.PixelWidth, source.PixelHeight, out criteria))
            {
                ClearPhotoContour();
                ClearRegionBox();
                Test = "参数无效，未执行筛选。";
                return;
            }
            BitmapSource binary = ImageFileLoader.ProcessBinary(source,_photoMorphology,morphologySize);
            MorphologyInfo=$"本次提取/筛选使用：{MorphologyName(_photoMorphology,morphologySize)}；二值阈值 {threshold}。";
            int width = binary.PixelWidth, height = binary.PixelHeight;
            byte[] pixels = new byte[checked(width * height)];
            binary.CopyPixels(pixels, width, 0);
            var regions = GrayImageProcessor.GetFourConnectedRegions(pixels, width, height);
            TargetSelection? selection = useFilters ? TargetSelector.Select(regions, criteria!) : null;
            var region = useFilters ? selection!.Region : regions.OrderByDescending(r => r.Area).FirstOrDefault();
            // 每次从原图计算，避免参数或 ROI 改动后仍显示旧候选。
            // 浏览范围是面积/形状合格者，包含距离不合格者，并按距离由近到远排列。
            var candidates = useFilters ? regions.Where(r =>
                TargetSelector.Select(new[] { r }, criteria!).ShapeCount == 1)
                .OrderBy(r => Math.Pow(r.CenterRow - criteria!.ExpectedRow, 2) +
                              Math.Pow(r.CenterCol - criteria!.ExpectedCol, 2)).ToList() : new List<ConnectedRegion>();
            var context = (criteria, selectedRoi, _photoMorphology, morphologySize);
            int candidateIndex = region == null ? -1 : candidates.IndexOf(region);
            if (nextCandidate && candidates.Count > 0)
            {
                candidateIndex = context.Equals(_candidateContext) ? (_candidateIndex + 1) % candidates.Count : 0;
                region = candidates[candidateIndex];
                // 保留总体计数，仅更新当前候选的测量值；距离超限只提示，不阻止浏览。
                selection = selection! with { Region = region, Ratio = (double)region.Width / region.Height,
                    Fill = region.Area / ((double)region.Width * region.Height),
                    Distance = Math.Sqrt(Math.Pow(region.CenterRow - criteria!.ExpectedRow, 2) +
                                         Math.Pow(region.CenterCol - criteria.ExpectedCol, 2)) };
            }
            // 先显示原图并清除旧状态，避免将旧轮廓叠到新照片上。
            DisplayPhoto(_loadedOriginal, "照片外轮廓");
            _candidateContext = context;
            _candidateIndex = candidateIndex;
            _candidateCount = candidates.Count;
            NextCandidateCommand.RaiseCanExecuteChanged();
            _photoTargetLabel = useFilters ? "最近合格目标" : "最大白色区域";
            if (useFilters && region != null)
                _photoTargetLabel = $"{(nextCandidate ? "浏览候选" : "最近合格目标")}（面积/形状候选 {candidateIndex + 1}/{candidates.Count}）";
            if (useFilters)
                FilterInfo = $"连通域 {regions.Count} 个 → 面积/形状合格 {selection!.ShapeCount} 个 → 距离合格 {selection.DistanceCount} 个。" +
                    $"预期位置（处理范围内）：行 {criteria!.ExpectedRow:0.##}、列 {criteria.ExpectedCol:0.##}。" +
                    (region == null ? (candidates.Count > 0 ? "未找到距离合格目标，可切换查看面积/形状候选。" : "没有面积/形状合格区域。") :
                    $"当前 {candidateIndex + 1}/{candidates.Count}：面积 {region.Area} 像素，宽高比 {selection.Ratio:0.###}，填充率 {selection.Fill:P1}，距离 {selection.Distance:0.##} 像素（{(selection.Distance <= criteria.MaxDistance ? "距离合格" : "距离不合格，仅供查看")}）。");
            else FilterInfo = "当前使用最大区域模式，未使用下方筛选条件。";
            Rect bounds = RoiCoordinates.GetImageBounds(_loadedOriginal,
                new Size(ImageDisplayWidth, ImageDisplayHeight));
            _photoContourScope = selectedRoi is Int32Rect scope
                ? $"ROI（起始行 {scope.Y}、列 {scope.X}，{scope.Width}×{scope.Height}）" : "全图（未框选 ROI）";
            _photoContourWarning = "";
            if (selectedRoi is Int32Rect restored)
            {
                // 保留蓝色选框和选择，重复提取仍然处理同一 ROI。
                _photoRoi = restored;
                RoiBounds = RoiCoordinates.ToDisplay(restored, bounds,
                    _loadedOriginal.PixelWidth, _loadedOriginal.PixelHeight);
                RoiBinaryCommand.RaiseCanExecuteChanged();
            }
            if (region == null)
            {
                Test = useFilters ? $"{_photoContourScope}：未找到符合条件的目标。" :
                    $"{_photoContourScope}：灰度 < 100 的白色前景为空，没有可追踪的轮廓。";
                RoiInfo = "可以重新框选或调整阈值；ROI 内没有目标时不会改为提取全图。";
                return;
            }
            int offsetRow = selectedRoi?.Y ?? 0;
            int offsetCol = selectedRoi?.X ?? 0;
            // 裁剪后追踪得到小图坐标，加上 ROI 起点才是原图坐标。
            _photoContour = ContourTracer.TraceOuterContour(region)
                .Select(p => (Row: p.Row + offsetRow, Col: p.Col + offsetCol)).ToList();
            if (selectedRoi != null && (region.MinRow == 0 || region.MinCol == 0 ||
                region.MaxRow == height - 1 || region.MaxCol == width - 1))
                _photoContourWarning = "目标接触选框边缘，轮廓可能包含裁剪边界；可扩大选区。";
            _photoContourArea = region.Area;
            // Uniform 显示会留白，映射时必须加上真实图像区域的偏移。
            var points = new PointCollection(_photoContour.Select(p => new Point(
                bounds.Left + p.Col * bounds.Width / _loadedOriginal.PixelWidth,
                bounds.Top + p.Row * bounds.Height / _loadedOriginal.PixelHeight)));
            points.Freeze();
            PhotoContourPoints = points;
            UpdatePhotoContourPosition();
        }

        /// <summary>推进到下一个有序边界顶点；最后一点与起点相同，表示闭合。</summary>
        private void AdvancePhotoContour()
        {
            if (_photoContourIndex + 1 >= _photoContour.Count) return;
            _photoContourIndex++;
            UpdatePhotoContourPosition();
        }

        private void UpdatePhotoContourPosition()
        {
            var p = _photoContour[_photoContourIndex];
            Point displayed = PhotoContourPoints[_photoContourIndex];
            DotLeft = displayed.X - DotDiameter / 2;
            DotTop = displayed.Y - DotDiameter / 2;
            DotVisibility = Visibility.Visible;
            string direction = "起点";
            if (_photoContourIndex > 0)
            {
                var previous = _photoContour[_photoContourIndex - 1];
                direction = p.Col > previous.Col ? "向右" : p.Col < previous.Col ? "向左" :
                    p.Row > previous.Row ? "向下" : "向上";
            }
            bool closed = _photoContourIndex == _photoContour.Count - 1;
            Test = $"{_photoContourScope} {_photoTargetLabel}：面积 {_photoContourArea} 像素；外轮廓步数 {_photoContour.Count - 1}。" +
                $" 当前 {_photoContourIndex}/{_photoContour.Count - 1}：边界坐标 行 {p.Row}、列 {p.Col}，{direction}。" +
                (closed ? "已回到起点，轮廓闭合。" : "");
            RoiInfo = "橙线：外轮廓；红点：当前位置。点击“轮廓下一步”前进。坐标基于原图；灰度 < 100 为前景，当前不追踪孔洞。" + _photoContourWarning;
            ContourNextCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 固定方框的路径演示：先直观看清绕行顺序，不是任意照片的自动轮廓提取。
        /// 点坐标写成 [行, 列]；最后再次回到 A，明确展示闭合。
        /// </summary>
        private void ShowContourDemo()
        {
            int step = (_traceStep + 1) % 9;
            (int Row, int Col)[] path =
            {
                (1,1), (1,2), (1,3), (2,3), (3,3), (3,2), (3,1), (2,1), (1,1)
            };
            string[] names = { "A", "B", "C", "D", "E", "F", "G", "H", "A" };
            byte[,] boundary = new byte[5,5];
            // 这些白点构成方框边界；中间和外部是黑色。
            foreach (var point in path)
                boundary[point.Row, point.Col] = 255;
            DisplayImage(GrayImageProcessor.Flatten(boundary), boundary);
            _traceStep = step; // 显示入口清除旧演示状态后，保存本次位置。
            TraceVisibility = Visibility.Visible;
            var current = path[step];
            // 列决定屏幕横坐标，行决定纵坐标；红点放到像素中心。
            DotLeft = (current.Col + 0.5) * ImageDisplayWidth / 5 - DotDiameter / 2;
            DotTop = (current.Row + 0.5) * ImageDisplayHeight / 5 - DotDiameter / 2;
            DotVisibility = Visibility.Visible;
            string direction = "起点，准备向右";
            if (step > 0)
            {
                var previous = path[step - 1];
                direction = current.Col > previous.Col ? "向右" : current.Col < previous.Col ? "向左" :
                    current.Row > previous.Row ? "向下" : "向上";
            }
            Test = $"方框轮廓演示：当前 {names[step]}，行 {current.Row}、列 {current.Col}；{direction}。" +
                (step == 8 ? "回到 A，一圈闭合。" : "") +
                $" 路径：{string.Join(" → ", names.Take(step + 1))}";
            RoiInfo = "继续点击“轮廓追踪演示”前进一步。白色是边界，红点是当前位置；这是固定路径示例。闭合后再点从 A 重新开始。";
        }

        /// <summary>连续点击同一个按钮，循环显示原图、第一步、第二步。</summary>
        private void ShowMorphologyDemo(bool opening)
        {
            string mode = opening ? "开运算" : "闭运算";
            int step = _morphologyMode == mode ? (_morphologyStep + 1) % 3 : 0;
            byte[,] original = CreateMorphologyDemo(opening);
            // 每次重新从固定原图推导；第二步明确读取第一步的结果。
            byte[,] first = opening ? GrayImageProcessor.Erode3x3(original)
                                    : GrayImageProcessor.Dilate3x3(original);
            byte[,] second = opening ? GrayImageProcessor.Dilate3x3(first)
                                     : GrayImageProcessor.Erode3x3(first);
            byte[,] shown = step == 0 ? original : step == 1 ? first : second;
            DisplayImage(GrayImageProcessor.Flatten(shown), shown);
            ImageStretch = Stretch.Uniform; // 方格保持正方形。
            _morphologyMode = mode;
            _morphologyStep = step;
            string[] openingNotes = { "原图：左上孤立白点 + 5×5 白块", "先腐蚀：白点消失，白块缩为 3×3", "再膨胀：白块恢复 5×5，孤立白点没有恢复" };
            string[] closingNotes = { "原图：5×5 白块中心有一个黑洞", "先膨胀：黑洞填满，白块扩大为 7×7", "再腐蚀：白块回到 5×5，中心黑洞已填补" };
            Test = $"{mode}演示 {step + 1}/3 — {(opening ? openingNotes : closingNotes)[step]}";
            RoiInfo = $"继续点击“{mode}演示”看下一步；第三步后回到原图。白色为前景，3×3 方形邻域，图片外按黑色。";
        }

        /// <summary>11×11 小图放大展示，不用真实照片，方便看清每一轮变化。</summary>
        private static byte[,] CreateMorphologyDemo(bool opening)
        {
            byte[,] binary = new byte[11, 11];
            for (int x = 3; x <= 7; x++)
                for (int y = 3; y <= 7; y++)
                    binary[x, y] = 255;
            if (opening) binary[1, 1] = 255; // 独立白噪点。
            else binary[5, 5] = 0;          // 白块中心的黑洞。
            return binary;
        }


    }
}
