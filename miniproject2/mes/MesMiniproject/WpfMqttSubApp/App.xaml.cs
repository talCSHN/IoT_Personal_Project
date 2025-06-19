using MahApps.Metro.Controls.Dialogs;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using WpfMqttSubApp.Helpers;
using WpfMqttSubApp.Models;
using WpfMqttSubApp.ViewModels;
using WpfMqttSubApp.Views;

namespace WpfMqttSubApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 애플리케이션이 완전 종료될 때까지 계속 사용 가능
        public static TotalConfig? Configuration { get; private set; }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // config.json 로드
            Configuration = ConfigLoader.Load();    // config.json 파일 로드

            // View 화면 로드 후 화면 띄우기
            var coordinator = DialogCoordinator.Instance;  // new DialogCoordinator() 와 동일
            var viewModel = new MainViewModel(coordinator);
            var view = new MainView
            {
                DataContext = viewModel,
            };
            view.ShowDialog();
        }
    }
}
