using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SerialToPlcApp.Models;

namespace SerialToPlcApp.Configuration
{
    public class PlcSettingsManager
    {
        public List<PlcSetting> LoadDeviceSettings(string jsonFilePath)
        {
            using (var reader = new StreamReader(jsonFilePath))
            {
                var json = File.ReadAllText(jsonFilePath);
                var settings = JsonConvert.DeserializeObject<PlcSettings>(json);
                return settings.Devices;
            }
        }
    }
}
