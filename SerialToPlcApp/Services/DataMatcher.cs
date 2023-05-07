using SerialToPlcApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialToPlcApp.Services
{
    public interface IDataMatcher
    {
        SerialCommands MatchCommand(string receivedData, List<SerialCommands> serialCommands);
    }

    public class DataMatcher : IDataMatcher
    {
        public SerialCommands MatchCommand(string receivedData, List<SerialCommands> serialCommands)
        {
            foreach (var command in serialCommands)
            {
                if (command.ValidateResponse(receivedData))
                {
                    return command;
                }
            }

            return null;
        }
    }
}
