using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfMrpSimulatorApp.Views
{
    /// <summary>
    /// MonitoringView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MonitoringView : UserControl
    {
        public MonitoringView()
        {
            InitializeComponent();
        }

        // 뷰상에 있는 이벤트핸들러를 전부 제거
        // WPF상의 객체 애니메이션 추가. 애니메이션은 디자이너역할(View)
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
                To = 417,  // x축: 센서아래 위치
                Duration = TimeSpan.FromSeconds(2), // 계획 로드타임(Schedules의 LoadTime 값이 들어가야 함)
            }; // 이런 초기화가 좀더 최신 코딩방식.

            // 아래는 구식 코딩방식
            //pa.From = 127;
            //pa.To = 417; 
            //pa.Duration = TimeSpan.FromSeconds(2); 

            Product.BeginAnimation(Canvas.LeftProperty, pa);
        }

        public void StartSensorCheck()
        {
            // 센서 애니메이션
            DoubleAnimation sa = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true
            };

            SortingSensor.BeginAnimation(OpacityProperty, sa);
        }
    }
}
