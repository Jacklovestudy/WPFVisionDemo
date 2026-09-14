using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VisionStudyDemo.ViewModels;

namespace VisionStudyDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // 鼠标捕获确保拖到图片外面再松开时，也能结束框选。
        private void RoiMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm &&
                vm.BeginRoiDrag(e.GetPosition(ImageStage), ImageStage.RenderSize))
            {
                ImageStage.Focus();
                ImageStage.CaptureMouse();
                e.Handled = true;
            }
        }

        private void RoiMouseMove(object sender, MouseEventArgs e)
        {
            if (ImageStage.IsMouseCaptured && DataContext is MainWindowViewModel vm)
                vm.UpdateRoiDrag(e.GetPosition(ImageStage));
        }

        private void RoiMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!ImageStage.IsMouseCaptured) return;
            if (DataContext is MainWindowViewModel vm)
                vm.CompleteRoiDrag(e.GetPosition(ImageStage));
            ImageStage.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void RoiKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            if (DataContext is MainWindowViewModel vm) vm.CancelRoiDrag();
            ImageStage.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void RoiLostCapture(object sender, MouseEventArgs e)
        {
            // 正常松开时左键已经释放；意外丢失捕获则取消本次拖动。
            if (Mouse.LeftButton == MouseButtonState.Pressed && DataContext is MainWindowViewModel vm)
                vm.CancelRoiDrag();
        }
    }
}
