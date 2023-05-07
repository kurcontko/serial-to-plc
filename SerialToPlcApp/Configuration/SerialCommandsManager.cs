using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SerialToPlcApp.Models;

namespace SerialToPlcApp.Configuration
{
    public class SerialCommandsManager
    {
        public List<SerialCommand> LoadSerialCommands(string jsonFilePath)
        {
            var json = File.ReadAllText(jsonFilePath);
            var commands = JsonConvert.DeserializeObject<SerialCommandsRoot>(json);
            return commands.SerialCommands;
        }

        public class SerialCommandsRoot
        {
            public List<SerialCommand> SerialCommands { get; set; }
        }
    }
}
