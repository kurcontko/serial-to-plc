using System.Collections.Generic;
using System.IO.Ports;

namespace SerialToPlcApp.Models
{
    public class SerialSetting
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int Parity { get; set; }
        public int DataBits { get; set; }
        public int StopBits { get; set; }
    }

    public class SerialSettings
    {
        public List<SerialSetting> Devices { get; set; }
    }


}
