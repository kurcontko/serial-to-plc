using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SerialToPlcApp.Models;

namespace SerialToPlcApp.Configuration
{
    public class DeviceSettingsManager
    {
        public List<DeviceSetting> LoadDeviceSettings(string jsonFilePath)
        {
            using (var reader = new StreamReader(jsonFilePath))
            {
                var json = File.ReadAllText(jsonFilePath);
                var settings = JsonConvert.DeserializeObject<DeviceSettings>(json);
                return settings.Devices;
            }
        }
    }
}
