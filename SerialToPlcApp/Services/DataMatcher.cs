using System.Collections.Generic;
using SerialToPlcApp.Models;

namespace SerialToPlcApp.Services
{
    public interface IDataMatcher
    {
        SerialCommand MatchCommand(string receivedData, List<SerialCommand> serialCommands);
    }

    public class DataMatcher : IDataMatcher
    {
        public SerialCommand MatchCommand(string receivedData, List<SerialCommand> serialCommands)
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
