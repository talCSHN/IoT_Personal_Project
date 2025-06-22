using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTnet;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using WpfIoTSimulatorApp.Models;

namespace WpfIoTSimulatorApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        #region 뷰와 연계되는 멤버변수/속성과 바인딩 

        private string _greeting;
        // 색상표시할 변수
        private Brush _productBrush;
        private string _logText;  // 로그출력

        #endregion

        #region 뷰와 관계없은 멤버변수

        private IMqttClient mqttClient;
        private string brokerHost;
        private string mqttTopic;
        private string clientId;

        private int logNum;  // 로그메시지 순번

        #endregion

        #region 생성자 

        public MainViewModel()
        {
            Greeting = "IoT Sorting Simulator";
            LogText = "프로그램 실행";

            // MQTT용 초기화
            brokerHost = "210.119.12.52"; // 본인 PC 아이피
            clientId = "IOT01";  // IoT장비번호
            mqttTopic = "pknu/sf52/data"; // 스마트팩토리 토픽
            logNum = 1; // 로그번호를 1부터 시작
            // MQTT 클라이언트 생성 및 초기화
            InitMqttClient();
        }

        #endregion

        #region 뷰와 연계되는 속성

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        public string Greeting
        {
            get => _greeting;
            set => SetProperty(ref _greeting, value);
        }

        // 제품 배경색 바인딩 속성
        public Brush ProductBrush
        {
            get => _productBrush;
            set => SetProperty(ref _productBrush, value);
        }

        #endregion

        #region 일반메서드

        private async Task InitMqttClient()
        {
            var mqttFactory = new MqttClientFactory();
            mqttClient = mqttFactory.CreateMqttClient();

            // MQTT 클라이언트 접속 설정
            var mqttClientOptions = new MqttClientOptionsBuilder()
                                        .WithTcpServer(brokerHost, 1883)   // 포트가 기존과 다르면 포트번호도 입력 필요
                                        .WithClientId(clientId)
                                        .WithCleanSession(true)
                                        .Build();
            // MQTT 클라이언트에 접속
            mqttClient.ConnectedAsync += async e =>
            {
                LogText = "MQTT 브로커 접속성공!";
            };

            await mqttClient.ConnectAsync(mqttClientOptions);

            // 테스트 메시지 
            var message = new MqttApplicationMessageBuilder()
                                .WithTopic(mqttTopic)
                                .WithPayload("Hello From IoT Simulator!")
                                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                                .Build();

            // MQTT 브로커로 전송!
            await mqttClient.PublishAsync(message);
            LogText = "MQTT 브로커에 초기메시지 전송!";
        }

        #endregion

        #region 이벤트 영역 

        public event Action? StartHmiRequested;
        public event Action? StartSensorCheckRequested; // VM에서 View에 있는 이벤트를 호출

        #endregion

        #region 릴레이커맨드 영역

        [RelayCommand]
        public void Move()
        {
            ProductBrush = Brushes.Gray;
            StartHmiRequested?.Invoke();  // 컨베이어벨트 애니메이션 요청(View에서 처리)
        }

        [RelayCommand]
        public void Check()
        {
            StartSensorCheckRequested?.Invoke();
            
            // 양품불량품 판단
            Random rand = new();
            int result = rand.Next(1, 3); // 1 ~ 2

            /*
            switch (result)
            {
                case 1:
                    ProductBrush = Brushes.Green;
                    break;
                case 2:
                    ProductBrush = Brushes.Crimson;
                    break;
                default:
                    ProductBrush = Brushes.Aqua;
                    break;
            } // 아래의 람다 switch와 완전동일 기능  */ 
            ProductBrush = result switch
            {
                1 => Brushes.Green, // 양품
                2 => Brushes.Crimson, // 불량
                _ => Brushes.Aqua,      // default 혹시나
            };

            // MQTT로 데이터 전송
            var resultText = result == 1 ? "OK" : "FAIL";
            var payload = new CheckResult
            {
                ClientId = clientId,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Result = resultText,
            };
            // 일반 객체 데이터를 json으로 변경 -> 직렬화(Serialization).
            var jsonPayload = JsonConvert.SerializeObject(payload, Formatting.Indented);
            var message = new MqttApplicationMessageBuilder()
                                .WithTopic(mqttTopic)
                                .WithPayload(jsonPayload)
                                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                                .Build();

            // MQTT 브로커로 전송!
            mqttClient.PublishAsync(message);
            LogText = $"MQTT 브로커에 결과메시지 전송 : {logNum++}";

        }

        #endregion
    }
}
