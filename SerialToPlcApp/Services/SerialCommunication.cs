using System;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SerialToPlcApp.Services
{
    public interface ISerialCommunication : IDisposable
    {
        void Open();
        void Close();
        Task SendAsync(string command, CancellationToken cancellationToken);
        Task<string> ReceiveAsync(CancellationToken cancellationToken);
    }
    public class SerialCommunication : ISerialCommunication
    {
        private SerialPort serialPort;

        public SerialCommunication(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        }

        public void Open()
        {
            if (!serialPort.IsOpen)
            {
                serialPort.Open();
            }
        }

        public void Close()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        public string ReadData()
        {
            return serialPort.ReadLine();
        }

        public void WriteData(string data)
        {
            serialPort.WriteLine(data);
        }

        public void Dispose()
        {
            if (serialPort != null)
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                serialPort.Dispose();
            }
        }
        public void FlushInputBuffer()
        {
            serialPort.DiscardInBuffer();
        }

        public async Task SendAsync(string command, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(serialPort.BaseStream))
            {
                await writer.WriteLineAsync(command);
                await writer.FlushAsync();
            }
        }

        public async Task<string> ReceiveAsync(CancellationToken cancellationToken)
        {
            using (var reader = new StreamReader(serialPort.BaseStream))
            {
                return await reader.ReadLineAsync();
            }
        }
    }
}
