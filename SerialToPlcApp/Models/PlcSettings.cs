using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialToPlcApp.Models
{
    public class PlcSetting
    {
        public string IpAddress { get; set; }
        public int Rack { get; set; }
        public int Slot { get; set; }
        public int DbNumber { get; set; }
        public int StartAddress { get; set; }
    }

    public class PlcSettings
    {
        public List<PlcSetting> Devices { get; set; }
    }
}
