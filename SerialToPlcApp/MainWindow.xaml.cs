using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Newtonsoft.Json;
using SerialToPlcApp.Models;
using SerialToPlcApp.Services;
using SerialToPlcApp.Logging;

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

        private List<DeviceSetting> LoadDeviceSettings(string jsonFilePath)
        {
            using (var reader = new StreamReader(jsonFilePath))
            {
                var json = reader.ReadToEnd();
                var settings = JsonConvert.DeserializeObject<DeviceSettings>(json);
                return settings.Devices;
            }
        }

        private List<SerialCommands> LoadSerialCommands(string jsonFilePath)
        {
            var json = File.ReadAllText(jsonFilePath);
            var commands = JsonConvert.DeserializeObject<SerialCommandsRoot>(json);
            return commands.SerialCommands;
        }

    }

}
