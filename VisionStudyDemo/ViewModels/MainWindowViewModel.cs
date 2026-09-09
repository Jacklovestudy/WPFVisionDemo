using Prism.Mvvm;
using System.Windows.Input;

namespace VisionStudyDemo.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region
        public ICommand TestCommand { get; set; }
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
        #endregion

      

        private readonly byte[,] _gray =
        {
            { 220, 220, 220, 220, 220 },
            { 220,  30,  30,  30, 220 },
            { 220,  30,  30,  30, 220 },
            { 220, 220, 220, 220, 220 }
        };

        public MainWindowViewModel()
        {
            //TestCommand = 
        }
    }
}
