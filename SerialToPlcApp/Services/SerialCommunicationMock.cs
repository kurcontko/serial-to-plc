using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SerialToPlcApp.Services
{
    public class SerialCommunicationMock : ISerialCommunication
    {
        private readonly Dictionary<string, string> responseMapping = new Dictionary<string, string>
    {
        { "RT\r", "20.0C" },
        { "RS\r", "20.0C" },
        { "RUFS\r", "0 0 0 13 64" },
        { "RCK\r", "18:47:53" },
    };

        public void Open()
        {
            // No implementation needed for the mock
        }

        public void Close()
        {
            // No implementation needed for the mock
        }

        public Task SendAsync(string command, CancellationToken cancellationToken)
        {
            // No implementation needed for the mock
            return Task.CompletedTask;
        }

        public Task<string> ReceiveAsync(CancellationToken cancellationToken)
        {
            // Simulate a delay before receiving the response
            return Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken)
                .ContinueWith(t => responseMapping.Values.ElementAt(new Random().Next(responseMapping.Count)), cancellationToken);
        }

        public void Dispose()
        {
            // No implementation needed for the mock
        }
    }
}
