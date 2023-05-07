using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Globalization;
using Sharp7;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Threading;
using static SerialToPlcApp.MainWindow;

namespace SerialToPlcApp
{
    public partial class MainWindow : Window
    {

        private CancellationTokenSource cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();

            // Load configuration files
            var devicesSettings = LoadDeviceSettings("devicesettings.json");
            var commandPairs = LoadSerialCommands("serialcommands.json");

            // Initialize objects
            var dataMatcher = new DataMatcher();
            var dataProcessor = new DataProcessor();
            var dataQueue = new DataQueue();
            ILogger logger = new Logger(LogTextBox, Dispatcher);

            foreach (var deviceSetting in devicesSettings)
            {
                // Set useMock to true for testing with the mock serial device, and false for testing with the actual serial device
                bool useMock = true;

                var serialCommunicationService = new SerialCommunicationService(dataProcessor, dataQueue, deviceSetting, commandPairs, logger, dataMatcher, useMock);
                var plcCommunicationService = new PlcCommunicationService(dataProcessor, dataQueue, deviceSetting, logger);

                // Start the SerialComm task
                Task.Run(async () =>
                {
                    try
                    {
                        await serialCommunicationService.RunAsync(cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Handle the case when the operation is canceled, if necessary
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions and log them
                        logger.Log($"An error occurred: {ex.Message}");
                    }
                });

                // Start the PlcComm task
                Task.Run(async () =>
                {
                    try
                    {
                        await plcCommunicationService.RunAsync(cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Handle the case when the operation is canceled, if necessary
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions and log them
                        logger.Log($"An error occurred: {ex.Message}");
                    }
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }

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

        public interface IDataProcessor
        {
            byte[] ProcessReceivedData(string receivedData, SerialCommand matchedCommand);
        }
        public class DataProcessor : IDataProcessor
        {
            public byte[] ProcessReceivedData(string receivedData, SerialCommand matchedCommand)
            {
                if (Regex.IsMatch(receivedData, matchedCommand.ValidationPattern))
                {
                    switch (matchedCommand.SendCommand)
                    {
                        case "RT\r":
                            return ProcessRtCommandResponse(receivedData);
                        case "RUFS\r":
                            return ProcessRufsCommandResponse(receivedData);
                        // Add more processing logic for other command responses if you need
                        default:
                            break;
                    }
                }

                return null; // Return null if the received data is not recognized or cannot be processed
            }

            private byte[] ProcessRtCommandResponse(string receivedData)
            {
                string trimmedData = receivedData.TrimEnd('C', '\r');
                float temperature;
                float.TryParse(trimmedData, NumberStyles.Float, CultureInfo.InvariantCulture, out temperature);
                byte[] temperatureBytes = BitConverter.GetBytes(temperature);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(temperatureBytes);
                }

                return temperatureBytes;
            }

            private byte[] ProcessRufsCommandResponse(string receivedData)
            {
                string[] values = receivedData.TrimEnd('\r').Split(' ');
                byte[] valuesBytes = new byte[values.Length];

                for (int i = 0; i < values.Length; i++)
                {
                    valuesBytes[i] = byte.Parse(values[i]);
                }

                return valuesBytes;
            }
        }

        public class ReceivedDataWithOffset
        {
            public byte[] ReceivedData { get; set; }
            public int OffsetAddress { get; set; }
        }

        public interface IDataQueue
        {
            void Enqueue(ReceivedDataWithOffset item);
            bool TryDequeue(out ReceivedDataWithOffset item);
        }
        public class DataQueue : IDataQueue
        {
            private readonly ConcurrentQueue<ReceivedDataWithOffset> queue = new ();
            private readonly int maxSize;

            public DataQueue(int maxSize = 500) // Limit the queue size to prevent memory overloading
            {
                this.maxSize = maxSize;
            }

            public void Enqueue(ReceivedDataWithOffset item)
            {
                queue.Enqueue(item);

                // Remove the oldest item if the queue size exceeds the maximum size
                while (queue.Count > maxSize)
                {
                    queue.TryDequeue(out _);
                }
            }

            public bool TryDequeue(out ReceivedDataWithOffset item)
            {
                return queue.TryDequeue(out item);
            }
        }

        public interface IPlcCommunicationService
        {
            Task RunAsync(CancellationToken cancellationToken);
        }
        public class PlcCommunicationService : IPlcCommunicationService
        {
            private readonly IDataProcessor dataProcessor;
            private readonly IDataQueue dataQueue;
            private readonly DeviceSetting deviceSetting;
            private readonly ILogger logger;
            private readonly PlcCommunication plcComm;

            public PlcCommunicationService(IDataProcessor dataProcessor, IDataQueue dataQueue, DeviceSetting deviceSetting, ILogger logger)
            {
                this.dataProcessor = dataProcessor;
                this.dataQueue = dataQueue;
                this.deviceSetting = deviceSetting;
                this.logger = logger;
                this.plcComm = new PlcCommunication(deviceSetting.IpAddress, deviceSetting.Rack, deviceSetting.Slot);
            }

            public async Task RunAsync(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {

                            plcComm.Open();

                            while (!cancellationToken.IsCancellationRequested)
                            {
                                try
                                {
                                    if (dataQueue.TryDequeue(out var receivedDataWithOffset))
                                    {
                                        // Process the received data
                                        byte[] receivedData = receivedDataWithOffset.ReceivedData;
                                        if (receivedData == null)
                                        {
                                            logger.Log($"Error: Data processing failed for received data: {receivedData}");
                                            continue; // Skip this iteration and move on to the next
                                        }

                                    // Write the processed data to the PLC
                                    int startAddress = deviceSetting.StartAddress + receivedDataWithOffset.OffsetAddress;
                                        int result = plcComm.Client.WriteArea(S7Consts.S7AreaDB, deviceSetting.DbNumber, startAddress, receivedData.Length, S7Consts.S7WLByte, receivedData);

                                        if (result != 0)
                                        {
                                            logger.Log($"Error writing data to PLC: {plcComm.Client.ErrorText(result)}");
                                            break; // Break the inner loop to restart the PLC communication
                                        }
                                        else
                                        {
                                        logger.Log($"Data sent to PLC successfully: {receivedData}");
                                        }   
                                    }
                                    else
                                    {
                                        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken); // Wait before restart
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.Log($"PLC communication error: {ex.Message}");
                                    break; // Break the inner loop to restart the PLC communication
                                }
                            }

                            plcComm.Close();
                    
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"PLC communication error: {ex.Message}");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // Wait before reopening conncection
                }
            }

        }

        public interface ISerialCommunicationService
        {
            Task RunAsync(CancellationToken cancellationToken);
        }
        public class SerialCommunicationService : ISerialCommunicationService
        {
            private readonly ISerialCommunication serialComm;
            private readonly DataProcessor dataProcessor;
            private readonly DataMatcher dataMatcher;
            private readonly DataQueue dataQueue;
            private readonly DeviceSetting deviceSetting;
            private readonly List<SerialCommand> serialCommands;
            private readonly ILogger logger;

            public SerialCommunicationService(DataProcessor dataProcessor, DataQueue dataQueue, DeviceSetting deviceSetting, List<SerialCommand> serialCommands, ILogger logger, DataMatcher dataMatcher, bool useMock)
            {
                this.serialComm = useMock ? (ISerialCommunication)new MockSerialCommunication() : new SerialCommunication(deviceSetting.PortName, deviceSetting.BaudRate);
                this.dataProcessor = dataProcessor;
                this.dataMatcher = dataMatcher;
                this.dataQueue = dataQueue;
                this.deviceSetting = deviceSetting;
                this.serialCommands = serialCommands;
                this.logger = logger;
            }

            public async Task RunAsync(CancellationToken cancellationToken)
            {

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        
                        serialComm.Open();

                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                foreach (var commandPair in serialCommands)
                                {
                                    await serialComm.SendAsync(commandPair.SendCommand, cancellationToken);
                                    string receivedData = await serialComm.ReceiveAsync(cancellationToken);

                                    var matchedCommand = dataMatcher.MatchCommand(receivedData, serialCommands);
                                    if (matchedCommand != null)
                                    {
                                        var processedData = dataProcessor.ProcessReceivedData(receivedData, matchedCommand);
                                        dataQueue.Enqueue(new ReceivedDataWithOffset
                                        {
                                            ReceivedData = processedData,
                                            OffsetAddress = matchedCommand.OffsetAddress
                                        });
                                    }
                                    else
                                    {
                                        logger.Log($"Received invalid data: {receivedData}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Log($"Serial communication error: {ex.Message}");
                                break; // Break the inner loop to restart the serial communication
                            }
                        }

                        serialComm.Close();
                        
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"Serial communication error: {ex.Message}");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // Wait before restart
                }
                
            }
        }

        public interface ILogger
        {
            void Log(string message);
        }

        public class Logger : ILogger
        {
            private readonly TextBox logTextBox;
            private readonly Dispatcher dispatcher;

            public Logger(TextBox logTextBox, Dispatcher dispatcher)
            {
                this.logTextBox = logTextBox;
                this.dispatcher = dispatcher;
            }

            public void Log(string message)
            {
                dispatcher.Invoke(() =>
                {
                    logTextBox.AppendText($"{DateTime.Now}: {message}{Environment.NewLine}");
                    logTextBox.ScrollToEnd();
                });
            }
        }

        private List<DeviceSetting> LoadDeviceSettings(string jsonFilePath)
        {
            using (var reader = new StreamReader(jsonFilePath))
            {
                var json = reader.ReadToEnd();
                var settings = JsonConvert.DeserializeObject<DeviceSettings>(json);
                return settings.Devices;
            }
        }

        private List<SerialCommand> LoadSerialCommands(string jsonFilePath)
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

    public class PlcCommunication : IDisposable
    {
        private readonly S7Client client;

        public S7Client Client => client; 

        public PlcCommunication(string ipAddress, int rack, int slot)
        {
            client = new S7Client();
            client.ConnectTo(ipAddress, rack, slot);
        }

        public void Open()
        {
            int result = client.Connect();
            if (result != 0)
            {
                throw new Exception($"Error connecting to PLC: {client.ErrorText(result)}");
            }
        }

        public void Close()
        {
            client.Disconnect();
        }

        public void WriteData(int dbNumber, int start, byte[] buffer)
        {
            int result = client.DBWrite(dbNumber, start, buffer.Length, buffer);
            if (result != 0)
            {
                throw new Exception($"Error writing data to PLC: {client.ErrorText(result)}");
            }
        }

        public void Dispose()
        {
            Close();
        }
    }

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

    public class MockSerialCommunication : ISerialCommunication
    {
        private readonly Dictionary<string, string> responseMapping = new Dictionary<string, string>
    {
        { "RT\r", "20.0C\r" },
        { "RUFS\r", "0 0 0 13 64\r" },
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

    public class SerialCommand
    {
        public string SendCommand { get; set; }
        public string ValidationPattern { get; set; }
        public int OffsetAddress { get; set; }

        public bool ValidateResponse(string response)
        {
            return Regex.IsMatch(response, ValidationPattern);
        }
    }

    public class DeviceSetting
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public string IpAddress { get; set; }
        public int Rack { get; set; }
        public int Slot { get; set; }
        public int DbNumber { get; set; }
        public int StartAddress { get; set; }
    }

    public class DeviceSettings
    {
        public List<DeviceSetting> Devices { get; set; }
    }
}
