using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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

        private void BtnTest_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            StartHmiAni();  // Hmi 애니메이션 동작
        }

        // WPF상의 객체 애니메이션 추가
        private void StartHmiAni()
        {
            Product.Fill = new SolidColorBrush(Colors.Gray); // 제품을 회색으로 칠하기

            // 기어애니메이션
            DoubleAnimation ga = new DoubleAnimation();
            ga.From = 0;
            ga.To = 360; // 360도 회전
            ga.Duration = TimeSpan.FromSeconds(2);  // 계획 로드타임(Schedules의 LoadTime 값이 들어가야 함)

            RotateTransform rt = new RotateTransform();
            GearStart.RenderTransform = rt;
            GearStart.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            GearEnd.RenderTransform = rt;
            GearEnd.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

            rt.BeginAnimation(RotateTransform.AngleProperty, ga);

            // 제품애니메이션
            DoubleAnimation pa = new DoubleAnimation();
            pa.From = 127;
            pa.To = 417; // x축: 센서아래 위치
            pa.Duration = TimeSpan.FromSeconds(2); // 계획 로드타임(Schedules의 LoadTime 값이 들어가야 함)

            Product.BeginAnimation(Canvas.LeftProperty, pa);
        }

        private void BtnCheck_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            StartSensorCheck();

            // 랜덤으로 색상을 결정짓는 작업
            Random rand = new Random();
            int result = rand.Next(1, 3); // 1~2 중 하나 선별

            switch (result)
            {
                case 1:
                    Product.Fill = new SolidColorBrush(Colors.Green); // 양품
                    break;

                case 2:
                    Product.Fill = new SolidColorBrush(Colors.Crimson); // 불량
                    break;

                    //case 3:
                    //    Product.Fill = new SolidColorBrush(Colors.Gray); // 선별실패
                    //    break;
            }
        }

        private void StartSensorCheck()
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
