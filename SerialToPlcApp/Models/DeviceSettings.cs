using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialToPlcApp.Models
{
    public class DeviceSetting
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public string IpAddress { get; set; }
        public int Rack { get; set; }
        public int Slot { get; set; }
        public int DbNumber { get; set; }
        public int StartAddress { get; set; }
    }

    public class DeviceSettings
    {
        public List<DeviceSetting> Devices { get; set; }
    }
}
