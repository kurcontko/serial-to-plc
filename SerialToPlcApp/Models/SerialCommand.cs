using System.Text.RegularExpressions;

namespace SerialToPlcApp.Models
{
    public class SerialCommand
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
