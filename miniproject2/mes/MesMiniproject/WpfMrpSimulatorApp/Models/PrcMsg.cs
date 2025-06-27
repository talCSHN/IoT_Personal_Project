using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMrpSimulatorApp.Models
{
    // JSON용 클래스
    public class PrcMsg
    {
        public string ClientId { get; set; }
        public string PlantCode { get; set; }
        public string FacilityId { get; set; }
        public string Timestamp { get; set; }
        public string Flag { get; set; }
    }
}
