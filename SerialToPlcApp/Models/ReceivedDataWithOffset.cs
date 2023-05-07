using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialToPlcApp.Models
{
    public class ReceivedDataWithOffset
    {
        public byte[] ReceivedData { get; set; }
        public int OffsetAddress { get; set; }
    }
}
