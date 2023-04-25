using Prism.Mvvm;



using MotorAPIPlus;

namespace ElectricSlit.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "电动狭缝控制";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {

        }
    }
}
