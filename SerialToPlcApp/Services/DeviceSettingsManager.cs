using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SerialToPlcApp.Models;

namespace SerialToPlcApp.Services
{
    public class DeviceSettingsManager
    {
        public List<DeviceSetting> LoadDeviceSettings(string jsonFilePath)
        {
            using (var reader = new StreamReader(jsonFilePath))
            {
                var json = reader.ReadToEnd();
                var settings = JsonConvert.DeserializeObject<DeviceSettings>(json);
                return settings.Devices;
            }
        }
    }
}
