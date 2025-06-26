using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MahApps.Metro.Controls.Dialogs;
using MQTTnet;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using WpfMrpSimulatorApp.Helpers;
using WpfMrpSimulatorApp.Models;

namespace WpfMrpSimulatorApp.ViewModels
{
    public partial class MonitoringViewModel : ObservableObject
    {
        private readonly IDialogCoordinator dialogCoordinator;

        #region 뷰와 관계없는 멤버변수

        private IMqttClient mqttClient;
        private string brokerHost;
        private string mqttSubTopic;    // MQTT 메시지 받아올 때 쓰는 토픽
        private string mqttPubTopic;    // MQTT 메시지 보낼 때 쓰는 토픽
        private string clientId;    // 클라이언트 자신 아이디

        #endregion

        private Brush _productBrush;
        private string _plantName;
        private string _prcDate;
        private string _prcLoadTime;
        private string _prcFacilityName;
        private int _schAmount;
        private int _successAmount;
        private int _failAmount;
        private string _successRate;
        private int _schIdx;
        private string _logText;

        public int SchIdx
        {
            get => _schIdx;
            set => SetProperty(ref _schIdx, value);
        }
        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        public Brush ProductBrush
        {
            get => _productBrush;
            set => SetProperty(ref _productBrush, value);
        }

        public string PlantName
        {
            get => _plantName;
            set => SetProperty(ref _plantName, value);
        }
        public string PrcDate
        {
            get => _prcDate;
            set => SetProperty(ref _prcDate, value);
        }
        public string PrcLoadTime
        {
            get => _prcLoadTime;
            set => SetProperty(ref _prcLoadTime, value);
        }
        public string PrcFacilityName
        {
            get => _prcFacilityName;
            set => SetProperty(ref _prcFacilityName, value);
        }
        public int SchAmount
        {
            get => _schAmount;
            set => SetProperty(ref _schAmount, value);
        }
        public int SuccessAmount
        {
            get => _successAmount;
            set => SetProperty(ref _successAmount, value);
        }
        public int FailAmount
        {
            get => _failAmount;
            set => SetProperty(ref _failAmount, value);
        }
        public string SuccessRate
        {
            get => _successRate;
            set => SetProperty(ref _successRate, value);
        }

        public event Action? StartHmiRequested;
        public event Action? StartSensorCheckRequested; // VM에서 View에 있는 이벤트를 호출
        
        public MonitoringViewModel(IDialogCoordinator coordinator)
        {
            this.dialogCoordinator = coordinator;
            SchIdx = 1; // 최초 1부터 시작

            // MQTT 초기화
            brokerHost = "210.119.12.54";   // 본인 IP
            clientId = "MON01";
            mqttSubTopic = "pknu/sf54/data";
            mqttPubTopic = "pknu/sf54/control";

            InitMqttClient();
        }

        private async Task InitMqttClient()
        {
            var mqttFactory = new MqttClientFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            
            // MQTT 클라이언트 접속 설정
            var options = new MqttClientOptionsBuilder()
                             .WithTcpServer(brokerHost)
                             .WithClientId(clientId)
                             .WithCleanSession(true)
                             .Build();

            // MQTT 브로커에 접속
            mqttClient.ConnectedAsync += async e =>
            {
                LogText = "접속 성공";
            };

            await mqttClient.ConnectAsync(options);

            // 구독
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(mqttSubTopic).Build());

            mqttClient.ApplicationMessageReceivedAsync += MqttMessageReceivedAsync;
        }

        // 구독메시지 들어오면 처리하는 이벤트
        private Task MqttMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            //LogText = Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);
            var payload = Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);

            try
            {
                var data = JsonConvert.DeserializeObject<CheckResult>(payload);
                Console.WriteLine(data.ToString());
                if (data.Result.ToUpper().Equals("OK"))
                {
                    SuccessAmount += 1;
                }
                else if (data.Result.ToUpper().Equals("FAIL"))
                {
                    FailAmount += 1;
                }
            }
            catch (Exception ex)
            {

            }

            return Task.CompletedTask;
        }

        public void CheckAni()
        {

        }

        [RelayCommand]
        public async Task SearchProcess()
        {
            try
            {
                string query = @"SELECT sch.schIdx, sch.plantCode, set1.codeName AS plantName,
                                        sch.schDate, sch.loadTime,
                                        sch.schStartTime, sch.schEndTime,
                                        sch.schFacilityId, set2.codeName AS schFacilityName,
                                        sch.schAmount    
                                   FROM schedules AS sch
                                   JOIN settings AS set1
                                     ON sch.plantCode = set1.BasicCode
                                   JOIN settings AS set2
                                     ON sch.schFacilityId = set2.BasicCode
                                  WHERE sch.schIdx = @schIdx";
                DataSet ds = new DataSet();
                using (MySqlConnection conn  = new MySqlConnection(Common.CONNSTR))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@schIdx", SchIdx);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(ds, "Result");

                    

                }
                if (ds.Tables["Result"].Rows.Count != 0)
                {
                    var row = ds.Tables["Result"].Rows[0];
                    PlantName = row["plantName"].ToString();
                    PrcDate = Convert.ToDateTime(row["schDate"]).ToString("yyyy-MM-dd");
                    PrcLoadTime = row["loadTime"].ToString();
                    PrcFacilityName = row["schFacilityName"].ToString();
                    SchAmount = Convert.ToInt32(row["schAmount"]);
                    SuccessAmount = FailAmount = 0;
                    SuccessRate = "0.0%";
                    
                }
                else
                {
                    await this.dialogCoordinator.ShowMessageAsync(this, "공정조회", "해당 공정 없음");
                    PlantName = string.Empty;   // 공정내용 전부 초기화
                    PrcDate = string.Empty;   // 공정내용 전부 초기화
                    PrcLoadTime = string.Empty;   // 공정내용 전부 초기화
                    PrcFacilityName = string.Empty;   // 공정내용 전부 초기화
                    SchAmount = 0;   // 공정내용 전부 초기화
                    SuccessAmount = FailAmount = 0;   // 공정내용 전부 초기화
                    SuccessRate = "0.0%"; // 공정내용 전부 초기화
                    return;
                }
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "공정조회 오류", ex.Message);
            }
        }
        [RelayCommand]
        public async Task StartProcess()
        {
            // MQTT Publish
            // 테스트 메시지 
            var message = new MqttApplicationMessageBuilder()
                                .WithTopic(mqttPubTopic)
                                .WithPayload("전달메시지")
                                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                                .Build();

            // MQTT 브로커로 전송!
            await mqttClient.PublishAsync(message);

            ProductBrush = Brushes.Gray;
            StartHmiRequested?.Invoke();  // 컨베이어벨트 애니메이션 요청(View에서 처리)
        }
    }
}
