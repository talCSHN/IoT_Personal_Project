using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfIoTSimulatorApp.Models
{
    // JSON 전송용 객체
    public class CheckResult
    {
        public string ClientId { get; set; }
        public string Timestamp { get; set; }
        public string Result { get; set; }
    }
}
