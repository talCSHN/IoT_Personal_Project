using System.Windows;
using WpfIoTSimulatorApp.Views;
using WpfIoTSimulatorApp.ViewModels;

namespace WpfIoTSimulatorApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var viewModel = new MainViewModel();
            var view = new MainView { 
                DataContext = viewModel,
            };

            view.ShowDialog();            
        }
    }

}
