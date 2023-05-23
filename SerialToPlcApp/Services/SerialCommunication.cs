using System;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace SerialToPlcApp.Services
{
    public interface ISerialCommunication : IDisposable
    {
        Task OpenAsync(int readTimeoutMilliseconds = 500, int writeTimeoutMilliseconds = 500, CancellationToken cancellationToken = default);
        Task CloseAsync(CancellationToken cancellationToken);
        Task SendAsync(string command, CancellationToken cancellationToken);
        Task<string> ReceiveAsync(CancellationToken cancellationToken);
    }
    public class SerialCommunication : ISerialCommunication
    {
        private SerialPort serialPort;    
        private StreamWriter writer;
        private StreamReader reader;

        public SerialCommunication(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        }

        public Task OpenAsync(int readTimeoutMilliseconds = 500, int writeTimeoutMilliseconds = 500, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                serialPort.Open();
                serialPort.ReadTimeout = readTimeoutMilliseconds;
                serialPort.WriteTimeout = writeTimeoutMilliseconds;
                writer = new StreamWriter(serialPort.BaseStream);
                reader = new StreamReader(serialPort.BaseStream);
            }, cancellationToken);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (serialPort.IsOpen)
                    {
                        FlushInputBuffer();
                        FlushOutputBuffer();
                        writer.Dispose();
                        reader.Dispose();
                        serialPort.Close();
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occur during close operation
                    throw new InvalidOperationException("Failed to close serial port", ex);
                }
            }, cancellationToken);
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
            serialPort.Close();
        }
        public void FlushInputBuffer()
        {
            serialPort.DiscardInBuffer();
        }

        public void FlushOutputBuffer()
        {
            serialPort.DiscardOutBuffer();
        }

        private string ReceiveData()
        {
            var receivedData = new StringBuilder();
            int currentByte;

            // Read bytes one by one until CR
            while ((currentByte = serialPort.BaseStream.ReadByte()) != '\r')
            {
                receivedData.Append((char)currentByte);
            }

            return receivedData.ToString();
        }
        public async Task SendAsync(string command, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(command);
            await writer.FlushAsync();
        }

        public Task<string> ReceiveAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => ReceiveData(), cancellationToken);
        }

    }
}
