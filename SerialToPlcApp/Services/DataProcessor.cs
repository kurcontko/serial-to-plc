using SerialToPlcApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SerialToPlcApp.Services
{
    public interface IDataProcessor
    {
        byte[] ProcessReceivedData(string receivedData, SerialCommands matchedCommand);
    }
    public class DataProcessor : IDataProcessor
    {
        public byte[] ProcessReceivedData(string receivedData, SerialCommands matchedCommand)
        {
            if (Regex.IsMatch(receivedData, matchedCommand.ValidationPattern))
            {
                switch (matchedCommand.SendCommand)
                {
                    case "RT\r":
                        return ProcessRtCommandResponse(receivedData);
                    case "RUFS\r":
                        return ProcessRufsCommandResponse(receivedData);
                    default:
                        break;
                }
            }

            return null; // Return null if the received data is not recognized or cannot be processed
        }

        private byte[] ProcessRtCommandResponse(string receivedData)
        {
            string trimmedData = receivedData.TrimEnd('C', '\r');
            float temperature;
            float.TryParse(trimmedData, NumberStyles.Float, CultureInfo.InvariantCulture, out temperature);
            byte[] temperatureBytes = BitConverter.GetBytes(temperature);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(temperatureBytes);
            }

            return temperatureBytes;
        }

        private byte[] ProcessRufsCommandResponse(string receivedData)
        {
            string[] values = receivedData.TrimEnd('\r').Split(' ');
            byte[] valuesBytes = new byte[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                valuesBytes[i] = byte.Parse(values[i]);
            }

            return valuesBytes;
        }
    }

}
