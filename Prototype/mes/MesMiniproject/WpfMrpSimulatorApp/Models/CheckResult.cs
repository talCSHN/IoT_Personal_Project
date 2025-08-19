namespace WpfMrpSimulatorApp.Models
{
    // JSON 전송용 객체. 딴데안쓰고 MQTT때만 사용
    public class CheckResult
    {
        public string ClientId { get; set; }
        public string Timestamp { get; set; }
        public string Result { get; set; }
    }
}
