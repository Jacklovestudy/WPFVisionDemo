using Prism.Mvvm;

namespace VisionStudyDemo.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "VisionStudyDemo";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        private string _test = "Test";

        public string Test
        {
            get => _test;
            set => SetProperty(ref _test, value);
        }
    }
}
