using System;

namespace SerialToPlcApp.Models
{
    public class ReceivedDataWithOffset
    {
        public byte[] ReceivedData { get; set; }
        public int OffsetAddress { get; set; }
    }
}
