using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using WpfMqttSubApp.ViewModels;

namespace WpfMqttSubApp.Views
{
    
    public partial class MainView : MetroWindow
    {
        public MainView()
        {
            InitializeComponent();

            var vm = new MainViewModel(DialogCoordinator.Instance);
            this.DataContext = vm;
            vm.PropertyChanged += (sender, e) => { 
                if (e.PropertyName == nameof(vm.LogText))
                {   
                    Dispatcher.InvokeAsync(() =>
                    {
                        LogBox.CaretPosition = LogBox.Document.ContentEnd;
                        LogBox.ScrollToEnd();
                    }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }
            };
        }
    }
}
