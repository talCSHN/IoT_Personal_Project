namespace WpfMqttSubApp.Models
{
    public class TotalConfig
    {
        public DatabaseConfig Database { get; set; }
        public MqttConfig Mqtt { get; set; }
    }

    public class MqttConfig
    {
        public string Broker { get; set; }
        public string ClientId { get; set; }
        public int Port { get; set; }
        public string Topic { get; set; }
    }

    public class DatabaseConfig
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserId {  get; set; }
        public string Password { get; set; }
    }
}
