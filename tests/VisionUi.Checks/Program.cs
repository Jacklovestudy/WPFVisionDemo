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
        // 加载真实 XAML，但不显示窗口、不干扰正在运行的学习程序。
        var window = new MainWindow { DataContext = vm };
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(800, 600));
        content.Arrange(new Rect(0, 0, 800, 600));
        var box = FindRectangle(content) ?? throw new Exception("Missing overlay rectangle");
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
            vm.ShowLoadedOriginalCommand.Execute();
            if (!ReferenceEquals(original, vm.GrayImage)) throw new Exception("Original photo not preserved");
            RunExperiment("ShowConnectedComponents", moved, 100);
            if (vm.ImageStretch != Stretch.Fill) throw new Exception("Array display mode not restored");
            CheckBox(0,160,160,160);
            vm.ShowLoadedOriginalCommand.Execute();
            CheckDot(null, null);
            checks++;
            Console.WriteLine($"Photo verified: {original.PixelWidth} x {original.PixelHeight}; Gray8, original toggle, released file lock.");
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
