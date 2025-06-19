using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfMqttSubApp.Models;

namespace WpfMqttSubApp.Helpers
{
    public static class ConfigLoader
    {
        public static TotalConfig Load(string path = "./config.json")
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("설정 파일 없음", path);
            }
            
            string json = File.ReadAllText(path);   // 문자열로 읽음
            var config = JsonConvert.DeserializeObject<TotalConfig>(json);
            
            if (config == null)
            {
                throw new InvalidDataException("설정 파일 읽을 수 없음");
            }

            return config;
        }
    }
}
