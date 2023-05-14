using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SerialToPlcApp.Configuration;
using SerialToPlcApp.DataProcessing;
using SerialToPlcApp.Queues;
using SerialToPlcApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.IO.Ports;
using log4net.Config;
using System.IO;
using log4net;

namespace SerialToPlcApp
{
    public partial class MainWindow : Window
    {
        private const string deviceSettingsFilePath = "DeviceSettings.json";
        private const string plcSettingsFilePath = "PlcSettings.json";
        private const string serialCommandsFilePath = "SerialCommands.json";
        

        private CancellationTokenSource cancellationTokenSource;

        private static readonly ILog log = LogManager.GetLogger(typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            XmlConfigurator.Configure(new FileInfo("log4net.config"));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();

            // Load configuration files
            var serialSettingsManager = new SerialSettingsManager();
            var plcSettingsManager = new PlcSettingsManager();
            var serialCommandsManager = new SerialCommandsManager();

            var serialSettings = serialSettingsManager.LoadDeviceSettings(deviceSettingsFilePath);
            var plcSettings = plcSettingsManager.LoadDeviceSettings(plcSettingsFilePath);  
            var serialCommands = serialCommandsManager.LoadSerialCommands(serialCommandsFilePath);

            var settingsPairs = serialSettings.Zip(plcSettings, (s, p) => new { Serial = s, Plc = p });

            foreach (var pair in settingsPairs)
            {
                var serialSetting = pair.Serial;
                var plcSetting = pair.Plc;
                // Set useMock to true for testing with the mock serial device, and false for testing with the actual serial device
                // Have to be replaced with some different solution
                bool useMock = true;

                // Initialize objects
                var dataMatcher = new DataMatcher();
                var dataProcessor = new DataProcessor();
                var dataQueue = new DataQueue();
                var serialComm = useMock ? (ISerialCommunication)new SerialCommunicationMock() : new SerialCommunication(serialSetting.PortName, serialSetting.BaudRate, (Parity)serialSetting.Parity, serialSetting.DataBits, (StopBits)serialSetting.StopBits);
                var serialCommunicationService = new SerialCommunicationService(serialComm, dataProcessor, dataMatcher, dataQueue, serialSetting, serialCommands);
                var plcCommunicationService = new PlcCommunicationService(dataQueue, plcSetting);

                // Start the SerialComm task
                Task.Run(async () =>
                {
                    try
                    {
                        await serialCommunicationService.RunAsync(cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"An error occurred: {ex.Message}");
                    }
                });

                // Start the PlcComm task
                Task.Run(async () =>
                {
                    try
                    {
                        await plcCommunicationService.RunAsync(cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"An error occurred: {ex.Message}");
                    }
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }

    }

}
