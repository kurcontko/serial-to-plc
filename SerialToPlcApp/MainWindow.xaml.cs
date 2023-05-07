using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SerialToPlcApp.Services;
using SerialToPlcApp.Logging;

namespace SerialToPlcApp
{
    public partial class MainWindow : Window
    {
        private const string DeviceSettingsFilePath = "devicesettings.json";
        private const string SerialCommandsFilePath = "serialcommands.json";

        private CancellationTokenSource cancellationTokenSource;
        private DeviceSettingsManager deviceSettingsManager;
        private SerialCommandsManager serialCommandsManager;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();
            deviceSettingsManager = new DeviceSettingsManager();
            serialCommandsManager = new SerialCommandsManager();

            // Load configuration files
            var deviceSettings = deviceSettingsManager.LoadDeviceSettings(DeviceSettingsFilePath);
            var serialCommands = serialCommandsManager.LoadSerialCommands(SerialCommandsFilePath);

            // Initialize objects
            var dataMatcher = new DataMatcher();
            var dataProcessor = new DataProcessor();
            var dataQueue = new DataQueue();
            var logger = new Logger(LogTextBox, Dispatcher);

            foreach (var deviceSetting in deviceSettings)
            {
                // Set useMock to true for testing with the mock serial device, and false for testing with the actual serial device
                bool useMock = true;

                var serialCommunicationService = new SerialCommunicationService(dataProcessor, dataQueue, deviceSetting, serialCommands, logger, dataMatcher, useMock);
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

    }

}
