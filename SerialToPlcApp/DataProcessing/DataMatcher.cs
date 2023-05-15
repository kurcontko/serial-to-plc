using System.Collections.Generic;
using SerialToPlcApp.Models;

namespace SerialToPlcApp.DataProcessing
{
    public interface IDataMatcher
    {
        SerialCommand MatchCommand(string receivedData, List<SerialCommand> serialCommands);
        void SetLastSentCommand(SerialCommand command);
    }

    public class DataMatcher : IDataMatcher
    {
        private SerialCommand lastSentCommand;

        public void SetLastSentCommand(SerialCommand command)
        {
            lastSentCommand = command;
        }

        public SerialCommand MatchCommand(string receivedData, List<SerialCommand> serialCommands)
        {
            if (lastSentCommand != null && lastSentCommand.ValidateResponse(receivedData))
            {
                return lastSentCommand;
            }

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
