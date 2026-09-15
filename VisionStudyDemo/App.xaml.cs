using Prism.DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using VisionStudyDemo.ViewModels;
using System.Windows;

namespace VisionStudyDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        public App()
        {
            InitializeComponent();
            // 明确设置 Fluent 强调色，独立运行和测试宿主均使用同一主题。
            Wpf.Ui.Appearance.ApplicationAccentColorManager.Apply(System.Windows.Media.Color.FromRgb(96,165,250));
        }
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();
            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
        }
    }

}
