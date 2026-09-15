using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VisionStudyDemo;
using VisionStudyDemo.ViewModels;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var vm = new MainWindowViewModel();
        // ROI 外的大暗块不能抢走 ROI 内的小目标；轮廓须映射回原图。
        var roiTraceVm = new MainWindowViewModel();
        byte[] roiTracePixels = Enumerable.Repeat((byte)255, 48).ToArray();
        for (int row = 0; row < 6; row++)
            for (int col = 0; col < 3; col++) roiTracePixels[row * 8 + col] = 0;
        roiTracePixels[3 * 8 + 5] = 0;
        var roiTraceImage = System.Windows.Media.Imaging.BitmapSource.Create(8,6,96,96,
            PixelFormats.Gray8,null,roiTracePixels,8);
        typeof(MainWindowViewModel).GetField("_loadedOriginal", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(roiTraceVm, roiTraceImage);
        var selectionField = typeof(MainWindowViewModel).GetField("_photoRoi", BindingFlags.Instance | BindingFlags.NonPublic)!;
        selectionField.SetValue(roiTraceVm, new Int32Rect(4,2,3,3));
        for (int repeat = 0; repeat < 2; repeat++)
        {
            roiTraceVm.PhotoContourCommand.Execute();
            if (roiTraceVm.PhotoContourPoints.Count != 5 ||
                roiTraceVm.PhotoContourPoints[0] != new Point(250,160) ||
                !roiTraceVm.Test.Contains("ROI") || !roiTraceVm.Test.Contains("面积 1 "))
                throw new Exception("ROI contour selection or coordinate offset incorrect");
        }
        selectionField.SetValue(roiTraceVm, new Int32Rect(6,0,2,2));
        roiTraceVm.PhotoContourCommand.Execute();
        if (roiTraceVm.PhotoContourPoints.Count != 0 || roiTraceVm.ContourNextCommand.CanExecute())
            throw new Exception("Empty ROI incorrectly fell back to full image");
        if (vm.PhotoContourCommand.CanExecute() || vm.ContourNextCommand.CanExecute())
            throw new Exception("Photo contour commands enabled before load");
        var photoTraceVm = new MainWindowViewModel();
        var photoTraceInput = System.Windows.Media.Imaging.BitmapSource.Create(3, 3, 96, 96,
            PixelFormats.Gray8, null, new byte[] { 255,255,255,255,0,255,255,255,255 }, 3);
        var photoField = typeof(MainWindowViewModel).GetField("_loadedOriginal", BindingFlags.Instance | BindingFlags.NonPublic)!;
        photoField.SetValue(photoTraceVm, photoTraceInput);
        photoTraceVm.PhotoContourCommand.Execute();
        if (!ReferenceEquals(photoTraceVm.GrayImage, photoTraceInput) || photoTraceVm.PhotoContourPoints.Count != 5)
            throw new Exception("Photo contour not derived from loaded image");
        Point firstPhotoPoint = photoTraceVm.PhotoContourPoints[0];
        if (Math.Abs(firstPhotoPoint.X - (40 + 320.0 / 3)) > 0.0001 ||
            Math.Abs(firstPhotoPoint.Y - 320.0 / 3) > 0.0001)
            throw new Exception("Photo contour letterbox mapping incorrect");
        for (int i = 0; i < 4; i++) photoTraceVm.ContourNextCommand.Execute();
        if (photoTraceVm.ContourNextCommand.CanExecute() || !photoTraceVm.Test.Contains("闭合"))
            throw new Exception("Photo contour closure incorrect");
        photoTraceVm.ShowLoadedOriginalCommand.Execute();
        if (photoTraceVm.PhotoContourPoints.Count != 0 || photoTraceVm.ContourNextCommand.CanExecute())
            throw new Exception("Stale photo contour after display switch");
        photoField.SetValue(photoTraceVm, System.Windows.Media.Imaging.BitmapSource.Create(1, 1, 96, 96,
            PixelFormats.Gray8, null, new byte[] { 255 }, 1));
        photoTraceVm.PhotoContourCommand.Execute();
        if (photoTraceVm.PhotoContourPoints.Count != 0 || !photoTraceVm.Test.Contains("为空"))
            throw new Exception("Empty foreground handled incorrectly");
        var traceVm = new MainWindowViewModel();
        (int Row, int Col)[] expectedTrace = { (1,1),(1,2),(1,3),(2,3),(3,3),(3,2),(3,1),(2,1),(1,1) };
        foreach (var point in expectedTrace)
        {
            traceVm.ContourDemoCommand.Execute();
            if (traceVm.DotLeft != (point.Col + 0.5) * 80 - 4 ||
                traceVm.DotTop != (point.Row + 0.5) * 64 - 4 ||
                traceVm.TraceVisibility != Visibility.Visible)
                throw new Exception("Contour demo position incorrect");
        }
        if (!traceVm.Test.Contains("闭合")) throw new Exception("Contour closure missing");
        traceVm.OpeningDemoCommand.Execute();
        if (traceVm.TraceVisibility != Visibility.Collapsed) throw new Exception("Stale trace labels");
        traceVm.ContourDemoCommand.Execute();
        if (!traceVm.Test.Contains("起点")) throw new Exception("Contour demo did not restart");
        if (vm.EdgeCommand.CanExecute()) throw new Exception("Edge enabled without photo");
        var edgeVm = new MainWindowViewModel();
        var edgeInput = System.Windows.Media.Imaging.BitmapSource.Create(6, 1, 96, 96,
            PixelFormats.Gray8, null, new byte[] { 0,0,0,255,255,255 }, 6);
        typeof(MainWindowViewModel).GetField("_loadedOriginal", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(edgeVm, edgeInput);
        for (int repeat = 0; repeat < 2; repeat++)
        {
            edgeVm.EdgeCommand.Execute();
            byte[] actual = new byte[6];
            edgeVm.GrayImage!.CopyPixels(actual, 6, 0);
            if (!actual.SequenceEqual(new byte[] { 0,255,255,255,0,0 }))
                throw new Exception("Mean plus edge pipeline incorrect");
        }
        edgeVm.ShowLoadedOriginalCommand.Execute();
        if (!ReferenceEquals(edgeVm.GrayImage, edgeInput)) throw new Exception("Edge lost original");
        var morphVm = new MainWindowViewModel();
        int WhiteCount()
        {
            byte[] pixels = new byte[121];
            morphVm.GrayImage!.CopyPixels(pixels, 11, 0);
            return pixels.Count(p => p == 255);
        }
        foreach (int expected in new[] { 26, 9, 25, 26 })
        {
            morphVm.OpeningDemoCommand.Execute();
            if (WhiteCount() != expected) throw new Exception("Opening demo step incorrect");
        }
        foreach (int expected in new[] { 24, 49, 25, 24 })
        {
            morphVm.ClosingDemoCommand.Execute();
            if (WhiteCount() != expected) throw new Exception("Closing demo step incorrect");
        }
        morphVm.OpeningDemoCommand.Execute();
        if (WhiteCount() != 26) throw new Exception("Switching demo did not reset step");
        if (vm.MedianFilterCommand.CanExecute() || vm.MedianBinaryCommand.CanExecute())
            throw new Exception("Median commands enabled without photo");
        var medianVm = new MainWindowViewModel();
        var medianInput = System.Windows.Media.Imaging.BitmapSource.Create(3, 3, 96, 96,
            PixelFormats.Gray8, null, new byte[] { 100,100,100,100,255,100,100,100,100 }, 3);
        typeof(MainWindowViewModel).GetField("_loadedOriginal", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(medianVm, medianInput);
        for (int repeat = 0; repeat < 2; repeat++)
        {
            medianVm.MedianFilterCommand.Execute();
            byte[] actual = new byte[9];
            medianVm.GrayImage!.CopyPixels(actual, 3, 0);
            if (actual.Any(value => value != 100) || !medianVm.GrayImage.IsFrozen)
                throw new Exception("Photo median output incorrect");
        }
        medianVm.MedianBinaryCommand.Execute();
        byte[] medianBinary = new byte[9];
        medianVm.GrayImage!.CopyPixels(medianBinary, 3, 0);
        if (medianBinary.Any(value => value != 0)) throw new Exception("Median threshold equality failed");
        medianVm.ShowLoadedOriginalCommand.Execute();
        if (!ReferenceEquals(medianVm.GrayImage, medianInput)) throw new Exception("Median lost original photo");
        // 用已知灰度图检查照片包装、命令以及重复点击不会累计处理。
        if (vm.MeanFilterCommand.CanExecute() || vm.MeanBinaryCommand.CanExecute())
            throw new Exception("Mean commands enabled without photo");
        var meanVm = new MainWindowViewModel();
        var input = System.Windows.Media.Imaging.BitmapSource.Create(3, 3, 96, 96,
            PixelFormats.Gray8, null, new byte[] { 10,10,10,10,100,10,10,10,10 }, 3);
        typeof(MainWindowViewModel).GetField("_loadedOriginal", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(meanVm, input);
        for (int repeat = 0; repeat < 2; repeat++)
        {
            meanVm.MeanFilterCommand.Execute();
            byte[] actual = new byte[9];
            meanVm.GrayImage!.CopyPixels(actual, 3, 0);
            if (!actual.SequenceEqual(new byte[] { 32,25,32,25,20,25,32,25,32 }))
                throw new Exception("Photo mean output incorrect");
        }
        meanVm.MeanBinaryCommand.Execute();
        byte[] binaryMean = new byte[9];
        meanVm.GrayImage!.CopyPixels(binaryMean, 3, 0);
        if (binaryMean.Any(value => value != 255)) throw new Exception("Mean then threshold failed");
        // 加载真实 XAML，但不显示窗口、不干扰正在运行的学习程序。
        var window = new MainWindow { DataContext = vm };
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(800, 600));
        content.Arrange(new Rect(0, 0, 800, 600));
        var box = (Rectangle?)window.FindName("RegionBox") ?? throw new Exception("Missing overlay rectangle");
        int checks = 0;

        void CheckDot(double? centerLeft, double? centerTop)
        {
            content.UpdateLayout();
            var dot = FindEllipse(content) ?? throw new Exception("Missing centroid dot");
            if (centerLeft == null)
            {
                if (dot.Visibility != Visibility.Collapsed)
                    throw new Exception("Stale centroid dot was not hidden");
            }
            else if (dot.Visibility != Visibility.Visible || dot.Width != 8 || dot.Height != 8 ||
                     Math.Abs(Canvas.GetLeft(dot) + dot.Width / 2 - centerLeft.Value) > 0.0001 ||
                     Math.Abs(Canvas.GetTop(dot) + dot.Height / 2 - centerTop!.Value) > 0.0001)
                throw new Exception("Centroid dot is not centered on the region centroid");
            checks++;
        }

        void CheckBox(double left, double top, double width, double height)
        {
            content.UpdateLayout();
            if (vm.BoxLeft != left || vm.BoxTop != top || vm.BoxWidth != width || vm.BoxHeight != height)
                throw new Exception("ViewModel box bounds incorrect");
            if (Canvas.GetLeft(box) != left || Canvas.GetTop(box) != top || box.Width != width || box.Height != height)
                throw new Exception("XAML box bindings incorrect");
            checks++;
        }

        void RunExperiment(string name, params object[] args)
        {
            typeof(MainWindowViewModel).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(vm, args);
        }

        CheckDot(null, null); // 首次计算前不显示
        vm.TestCommand.Execute(null);
        // 数组实验入口随课程变化；框测试显式选择连通域实验。
        RunExperiment("ShowConnectedComponents", new byte[,] {
            {220,220,220,220,0}, {220,30,30,30,220},
            {220,30,30,30,220}, {220,220,220,220,220} }, 100);
        CheckBox(80, 80, 240, 160); // 默认选中面积 6 的块，而非先发现的孤立点
        CheckDot(200, 160);

        byte[,] moved = { { 220, 220, 220, 220, 30 }, { 220, 220, 220, 220, 220 },
                          { 30, 30, 220, 220, 220 }, { 30, 30, 220, 220, 220 } };
        RunExperiment("ShowConnectedComponents", moved, 100);
        CheckBox(0, 160, 160, 160);
        CheckDot(80, 240);
        RunExperiment("ShowConnectedComponents", moved, 0);
        CheckBox(0, 0, 0, 0); // 无前景，旧框清除
        CheckDot(null, null);
        RunExperiment("ShowConnectedComponents", moved, 100);
        RunExperiment("ShowInverted", moved);
        CheckBox(0, 0, 0, 0); // 切换实验不会残留框
        CheckDot(null, null);
        RunExperiment("ShowAreaFiltered", moved, 100, 2);
        CheckBox(0, 160, 160, 160);
        CheckDot(80, 240);
        RunExperiment("ShowAreaFiltered", moved, 100, 5);
        CheckBox(0, 0, 0, 0);
        CheckDot(null, null);
        // L 形的质心不是外接矩形中心；同时验证横纵缩放不同的情况。
        RunExperiment("ShowConnectedComponents", new byte[,] { { 30, 30 }, { 30, 220 } }, 100);
        CheckDot(500.0 / 3, 400.0 / 3);
        RunExperiment("ShowConnectedComponents", new byte[,] { { 220, 30 } }, 100);
        CheckDot(300, 160); // 单像素标在方块中心
        if (vm.ConvertToGrayCommand.CanExecute()) throw new Exception("Gray command enabled before loading");
        if (vm.ConvertToBinaryCommand.CanExecute()) throw new Exception("Binary command enabled before loading");
        var rgb = System.Windows.Media.Imaging.BitmapSource.Create(3, 1, 96, 96,
            PixelFormats.Rgb24, null, new byte[] {255,0,0, 0,255,0, 0,0,255}, 9);
        var gray = VisionStudyDemo.Imaging.ImageFileLoader.ToGray(rgb);
        byte[] values = new byte[3];
        gray.CopyPixels(values, 3, 0);
        if (!values.SequenceEqual(new byte[] {76,150,29})) throw new Exception("RGB weights or channel order incorrect");
        checks++;

        if (args.Length > 0)
        {
            RunExperiment("LoadImageFile", args[0]);
            vm.PhotoContourCommand.Execute();
            if (vm.PhotoContourPoints.Count < 5 || vm.PhotoContourPoints[0] != vm.PhotoContourPoints[^1])
                throw new Exception("Real photo outer contour not closed");
            content.UpdateLayout();
            var contourLine = (Polyline?)window.FindName("PhotoContourLine");
            if (contourLine == null || contourLine.Points.Count != vm.PhotoContourPoints.Count)
                throw new Exception("Photo contour XAML binding missing");
            Console.WriteLine($"Real photo contour: {vm.PhotoContourPoints.Count - 1} steps, closed.");
            vm.ShowLoadedOriginalCommand.Execute();
            var original = vm.GrayImage!;
            if (original.PixelWidth != 1702 || original.PixelHeight != 1276)
                throw new Exception("Sample photo dimensions incorrect");
            if (vm.ImageStretch != Stretch.Uniform || !vm.ConvertToGrayCommand.CanExecute())
                throw new Exception("Photo display state incorrect");
            CheckDot(null, null);
            using (System.IO.File.Open(args[0], System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.None)) { }
            vm.ConvertToGrayCommand.Execute();
            if (vm.GrayImage!.Format != PixelFormats.Gray8 || vm.GrayImage.PixelWidth != original.PixelWidth)
                throw new Exception("Grayscale photo output incorrect");
            vm.ConvertToBinaryCommand.Execute();
            var binaryPhoto = vm.GrayImage!;
            byte[] binaryPixels = new byte[binaryPhoto.PixelWidth * binaryPhoto.PixelHeight];
            binaryPhoto.CopyPixels(binaryPixels, binaryPhoto.PixelWidth, 0);
            if (binaryPhoto.Format != PixelFormats.Gray8 || binaryPhoto.PixelWidth != original.PixelWidth ||
                binaryPhoto.PixelHeight != original.PixelHeight || binaryPixels.Any(p => p != 0 && p != 255) ||
                !binaryPixels.Contains((byte)0) || !binaryPixels.Contains((byte)255))
                throw new Exception("Photo binary output incorrect");
            vm.ConvertToBinaryCommand.Execute();
            byte[] repeated = new byte[binaryPixels.Length];
            vm.GrayImage!.CopyPixels(repeated, binaryPhoto.PixelWidth, 0);
            if (!repeated.SequenceEqual(binaryPixels)) throw new Exception("Binary command accumulated changes");
            checks++;
            vm.ShowLoadedOriginalCommand.Execute();
            if (!ReferenceEquals(original, vm.GrayImage)) throw new Exception("Original photo not preserved");
            RunExperiment("ShowConnectedComponents", moved, 100);
            if (vm.ImageStretch != Stretch.Fill) throw new Exception("Array display mode not restored");
            CheckBox(0,160,160,160);
            vm.ShowLoadedOriginalCommand.Execute();
            CheckDot(null, null);
            checks++;
            Console.WriteLine($"Photo verified: {original.PixelWidth} x {original.PixelHeight}; Gray8, original toggle, released file lock.");
            // Uniform 显示会有上下留白，框选坐标必须扣除留白。
            var viewport = new Size(400, 320);
            var bounds = VisionStudyDemo.Imaging.RoiCoordinates.GetImageBounds(original, viewport);
            vm.SelectRoiCommand.Execute();
            if (vm.BeginRoiDrag(new Point(1, 0), viewport)) throw new Exception("Letterbox accepted as image");
            var selection = new Int32Rect(600, 300, 600, 600);
            var displaySelection = VisionStudyDemo.Imaging.RoiCoordinates.ToDisplay(selection, bounds,
                original.PixelWidth, original.PixelHeight);
            // 使用像素内部点避免浮点边界误差；反向拖动仍应得到同一区域。
            Point start = new(displaySelection.Right - 0.01, displaySelection.Bottom - 0.01);
            Point end = new(displaySelection.Left + 0.01, displaySelection.Top + 0.01);
            if (!vm.BeginRoiDrag(start, viewport)) throw new Exception("ROI drag did not begin");
            vm.UpdateRoiDrag(end);
            vm.CompleteRoiDrag(end);
            if (!vm.RoiBinaryCommand.CanExecute()) throw new Exception("ROI processing not enabled");
            vm.RoiBinaryCommand.Execute();
            var roiOutput = vm.GrayImage!;
            if (roiOutput.PixelWidth != 600 || roiOutput.PixelHeight != 600)
                throw new Exception("ROI dimensions wrong");
            var expectedRoi = VisionStudyDemo.Imaging.ImageFileLoader.ToBinary(
                new System.Windows.Media.Imaging.CroppedBitmap(original, selection), 100);
            byte[] actualRoiPixels = new byte[600 * 600];
            byte[] expectedRoiPixels = new byte[600 * 600];
            roiOutput.CopyPixels(actualRoiPixels, 600, 0);
            expectedRoi.CopyPixels(expectedRoiPixels, 600, 0);
            if (!actualRoiPixels.SequenceEqual(expectedRoiPixels)) throw new Exception("ROI crop pixel mismatch");
            if (vm.IsSelectingRoi || vm.RoiBinaryCommand.CanExecute()) throw new Exception("ROI overlay remains on cropped view");
            checks++;

            vm.SelectRoiCommand.Execute();
            vm.BeginRoiDrag(new Point(bounds.Left, bounds.Top), viewport);
            vm.CompleteRoiDrag(new Point(900, 900)); // 拖出边界被夹到原图内部
            vm.RoiBinaryCommand.Execute();
            if (vm.GrayImage!.PixelWidth != original.PixelWidth || vm.GrayImage.PixelHeight != original.PixelHeight)
                throw new Exception("Clamped full-image ROI incorrect");
            checks++;

            vm.SelectRoiCommand.Execute();
            vm.BeginRoiDrag(new Point(100,100), viewport);
            vm.CompleteRoiDrag(new Point(100,100));
            if (vm.RoiBinaryCommand.CanExecute()) throw new Exception("Click created an empty ROI");
            vm.BeginRoiDrag(new Point(100,100), viewport);
            vm.UpdateRoiDrag(new Point(200,200));
            vm.CancelRoiDrag();
            if (vm.RoiBounds.Width != 0 || vm.IsSelectingRoi) throw new Exception("Cancellation failed");
            checks++;
            vm.SelectRoiCommand.Execute();
            vm.BeginRoiDrag(new Point(100,100), viewport);
            vm.CompleteRoiDrag(new Point(200,200));
            RunExperiment("LoadImageFile", args[0]);
            if (vm.RoiBinaryCommand.CanExecute() || vm.RoiBounds.Width != 0)
                throw new Exception("Loading another photo retained stale ROI");
            checks++;
            // 竖图有左右留白。
            var portrait = System.Windows.Media.Imaging.BitmapSource.Create(2, 4, 96, 96,
                PixelFormats.Gray8, null, new byte[8], 2);
            var portraitBounds = VisionStudyDemo.Imaging.RoiCoordinates.GetImageBounds(portrait, new Size(400,320));
            if (portraitBounds != new Rect(120,0,160,320)) throw new Exception("Portrait letterbox mapping incorrect");
            checks++;
            Console.WriteLine("ROI verified: letterbox, reverse drag, crop pixels, bounds, click, cancel, reload, portrait.");
        }
        window.Close();
        Console.WriteLine($"PASS: {checks} WPF box scenarios (ViewModel + XAML bindings).");
    }

    private static Rectangle? FindRectangle(DependencyObject parent)
    {
        if (parent is Rectangle rectangle) return rectangle;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var found = FindRectangle(VisualTreeHelper.GetChild(parent, i));
            if (found != null) return found;
        }
        return null;
    }

    private static Ellipse? FindEllipse(DependencyObject parent)
    {
        if (parent is Ellipse ellipse) return ellipse;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var found = FindEllipse(VisualTreeHelper.GetChild(parent, i));
            if (found != null) return found;
        }
        return null;
    }
}
