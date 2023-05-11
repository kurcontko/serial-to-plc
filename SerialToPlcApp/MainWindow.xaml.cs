using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SerialToPlcApp.Configuration;
using SerialToPlcApp.DataProcessing;
using SerialToPlcApp.Logging;
using SerialToPlcApp.Queues;
using SerialToPlcApp.Services;

namespace SerialToPlcApp
{
    public partial class MainWindow : Window
    {
        private const string deviceSettingsFilePath = "devicesettings.json";
        private const string serialCommandsFilePath = "serialcommands.json";

        private CancellationTokenSource cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();

            // Load configuration files
            var deviceSettingsManager = new DeviceSettingsManager();
            var serialCommandsManager = new SerialCommandsManager();

            var deviceSettings = deviceSettingsManager.LoadDeviceSettings(deviceSettingsFilePath);
            var serialCommands = serialCommandsManager.LoadSerialCommands(serialCommandsFilePath);

            // Create services for each device
            foreach (var deviceSetting in deviceSettings)
            {
                // Set useMock to true for testing with the mock serial device, and false for testing with the actual serial device
                bool useMock = true;

                // Initialize objects
                var dataMatcher = new DataMatcher();
                var dataProcessor = new DataProcessor();
                var dataQueue = new DataQueue();
                var logger = new Logger(LogTextBox, Dispatcher);
                var serialCommunicationService = new SerialCommunicationService(dataProcessor, dataQueue, deviceSetting, serialCommands, logger, dataMatcher, useMock);
                var plcCommunicationService = new PlcCommunicationService(dataProcessor, dataQueue, deviceSetting, logger);

                // Start the SerialComm task
                Task.Run(async () =>
                {
                    try
                    {
                        await serialCommunicationService.RunAsync(cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
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
                    catch (Exception ex)
                    {
                        logger.Log($"An error occurred: {ex.Message}");
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
