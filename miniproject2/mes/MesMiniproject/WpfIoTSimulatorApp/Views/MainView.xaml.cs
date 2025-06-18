using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WpfIoTSimulatorApp.ViewModels;

namespace WpfIoTSimulatorApp.Views
{
    /// <summary>
    /// MainView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainView : MetroWindow
    {
        
        public MainView()
        {
            InitializeComponent();
        }

        //Timer timer = new Timer();
        Stopwatch sw = new Stopwatch();

        // 뷰 상에 있는 이벤트 핸들러 전부 제거
        // WPF상의 객체 애니메이션 추가
        public void StartHmiAni()
        {

            // 기어애니메이션
            DoubleAnimation ga = new DoubleAnimation
            {
                From = 0,
                To = 360, // 360도 회전
                Duration = TimeSpan.FromSeconds(2),  // 계획 로드타임(Schedules의 LoadTime 값이 들어가야 함)
            };
            

            RotateTransform rt = new RotateTransform();
            GearStart.RenderTransform = rt;
            GearStart.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            GearEnd.RenderTransform = rt;
            GearEnd.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

            rt.BeginAnimation(RotateTransform.AngleProperty, ga);

            // 제품애니메이션
            DoubleAnimation pa = new DoubleAnimation
            {
                From = 127,
                To = 417, // x축: 센서아래 위치
                Duration = TimeSpan.FromSeconds(2), // 계획 로드타임(Schedules의 LoadTime 값이 들어가야 함)
            };  // 이런 초기화가 최신 트렌드
            
            Product.BeginAnimation(Canvas.LeftProperty, pa);
        }

        public void StartSensorCheck()
        {
            // 센서 애니메이션
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                Debug.WriteLine("UI작업시작");
                DoubleAnimation sa = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(1),
                    AutoReverse = true                   
                };

                SortingSensor.BeginAnimation(OpacityProperty, sa);

                Debug.WriteLine("UI작업종료");
            }));

            Debug.WriteLine("Dispatcher 완전종료");
            
        }
    }
}
