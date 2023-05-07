using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SerialToPlcApp.Models
{
    public class SerialCommands
    {
        public string SendCommand { get; set; }
        public string ValidationPattern { get; set; }
        public int OffsetAddress { get; set; }

        public bool ValidateResponse(string response)
        {
            return Regex.IsMatch(response, ValidationPattern);
        }
    }


}
