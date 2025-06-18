using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfIoTSimulatorApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private string _greeting;

        public MainViewModel()
        {
            Greeting = "IoT Sorting Simulator";
        }

        public string Greeting
        {
            get => _greeting;
            set => SetProperty(ref _greeting, value);
        }
    }
}
