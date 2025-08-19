using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTnet;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WpfIoTSimulatorApp.Models;

namespace WpfIoTSimulatorApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        #region MQTT 재접속용 변수

        private Timer _mqttMonitorTimer;
        private bool _isReconnecting = false;

        #endregion

        #region 뷰와 연계되는 멤버변수/속성과 바인딩 

        private string _greeting;
        // 색상표시할 변수
        private Brush _productBrush;
        private string _logText;  // 로그출력

        #endregion

        #region 뷰와 관계없은 멤버변수

        private IMqttClient mqttClient;
        private string brokerHost;
        private string mqttPubTopic;
        private string mqttSubTopic;
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
            mqttPubTopic = "pknu/sf52/data"; // 스마트팩토리 토픽
            mqttSubTopic = "pknu/sf52/control"; // 모니터리에서 넘어오는 토픽

            logNum = 1; // 로그번호를 1부터 시작
            // MQTT 클라이언트 생성 및 초기화
            InitMqttClient();
            // MQTT 재접속확인용 타이머 실행
            StartMqttMonitor();
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

        private void StartMqttMonitor()
        {
            _mqttMonitorTimer = new Timer(async _ =>
            {
                await CheckMqttConnectionAsync();  // 
            }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10)); 
            // 프로그램실행 2초 이후부터, 10초마다 한번씩 연결여부 확인, 재접속
        }

        // 핵심. MQTTClient 접속이 끊어지면 재접속
        private async Task CheckMqttConnectionAsync()
        {
            if (!mqttClient.IsConnected)
            {
                _isReconnecting = true;
                LogText = "MQTT 연결해제. 재접속 중...";

                try
                {
                    // MQTT 클라이언트 접속 설정
                    var options = new MqttClientOptionsBuilder()
                                        .WithTcpServer(brokerHost, 1883)
                                        .WithClientId(clientId)
                                        .WithCleanSession(true)
                                        .Build();
                    await mqttClient.ConnectAsync(options);
                    LogText = "MQTT 재접속 성공!";
                    
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MQTT 재접속 실패 : {ex.Message}");
                }
            }
        }

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
                                .WithTopic(mqttPubTopic)
                                .WithPayload("Hello From IoT Simulator!")
                                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                                .Build();

            // MQTT 브로커로 전송!
            await mqttClient.PublishAsync(message);
            LogText = "MQTT 브로커에 초기메시지 전송!";

            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(mqttSubTopic).Build());
            mqttClient.ApplicationMessageReceivedAsync += MqttMessageReceivedAsync;
        }

        private Task MqttMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            var payload = Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);
            // PrcMsg클래스로 Deserialization 처리
            var data = JsonConvert.DeserializeObject<PrcMsg>(payload);

            // LogText = data.Flag; "on" or "ON"
            if (data.Flag.ToUpper() == "ON")
            {
                Move(); // 이동끝나고 나면
                Thread.Sleep(2200);
                Check(); // 
            } 

            return Task.CompletedTask;
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
            Application.Current.Dispatcher.Invoke(() =>  // UI스레드와 VM스레드간 분리
            {
                StartHmiRequested?.Invoke();  // 컨베이어벨트 애니메이션 요청(View에서 처리)
            });
        }

        [RelayCommand]
        public void Check()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StartSensorCheckRequested?.Invoke();
            });
            
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

            try
            {
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
                                    .WithTopic(mqttPubTopic)
                                    .WithPayload(jsonPayload)
                                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                                    .Build();

                // MQTT 브로커로 전송!
                mqttClient.PublishAsync(message);
                LogText = $"MQTT 브로커에 결과메시지 전송 : {logNum++}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        #endregion
    }
}
