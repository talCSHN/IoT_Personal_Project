using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;
using WpfMrpSimulatorApp.Helpers;
using WpfMrpSimulatorApp.Models;
using WpfMrpSimulatorApp.Views;

namespace WpfMrpSimulatorApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // 다이얼로그 코디네이터 변수 선언
        private readonly IDialogCoordinator dialogCoordinator;

        private string _greeting;
        private UserControl _currentView;

        public MainViewModel(IDialogCoordinator coordinator)
        {
            this.dialogCoordinator = coordinator; // 다이얼로그 코디네이터 초기화

            Greeting = "MRP 공정관리!";
        }

        public string Greeting
        {
            get => _greeting;
            set => SetProperty(ref _greeting, value);
        }

        public UserControl CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        [RelayCommand]
        public async Task AppExit()
        {
            //var result = MessageBox.Show("종료하시겠습니까?", "종료확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            var result = await this.dialogCoordinator.ShowMessageAsync(this, "종료확인", "종료하시겠습니까?", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative) 
            {
                Application.Current.Shutdown();
            } else
            {
                return;
            }
        }

        [RelayCommand]
        public void AppSetting()
        {
            var viewModel = new SettingViewModel(Common.DIALOGCOORDINATOR);
            var view = new SettingView
            {
                DataContext = viewModel,
            };

            CurrentView = view;
        }

        [RelayCommand]
        public void SetSchedule()
        {
            var viewModel = new ScheduleViewModel(Common.DIALOGCOORDINATOR);
            var view = new ScheduleView
            {
                DataContext = viewModel,
            };

            CurrentView = view;
        }

        [RelayCommand]
        public void GetMonitoring()
        {
            var viewModel = new MonitoringViewModel(Common.DIALOGCOORDINATOR);
            var view = new MonitoringView
            {
                DataContext = viewModel,
            };

            viewModel.StartHmiRequested += view.StartHmiAni; // 애니메이션 동작 연결
            viewModel.StartSensorCheckRequested += view.StartSensorCheck; // 센서동작 애니메이션 연결

            CurrentView = view;
        }
    }
}
