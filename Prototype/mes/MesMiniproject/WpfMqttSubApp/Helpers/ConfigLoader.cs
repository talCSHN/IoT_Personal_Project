using Newtonsoft.Json;
using System.IO;
using WpfMqttSubApp.Models;

namespace WpfMqttSubApp.Helpers
{
    public static class ConfigLoader
    {
        public static TotalConfig Load(string path = "./config.json")
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("설정파일이 없습니다", path);
            }

            string json = File.ReadAllText(path); // 문자열로 읽음
            var config = JsonConvert.DeserializeObject<TotalConfig>(json);

            if (config == null)
            {
                throw new InvalidDataException("설정파일을 읽을 수 없습니다.");
            }
            return config;
        }
    }
}
