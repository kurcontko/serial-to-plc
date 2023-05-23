using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SerialToPlcApp.DataProcessing;
using SerialToPlcApp.Models;
using SerialToPlcApp.Queues;
using log4net;

namespace SerialToPlcApp.Services
{
    public interface ISerialCommunicationService
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
    public class SerialCommunicationService : ISerialCommunicationService
    {
        private readonly ISerialCommunication serialComm;
        private readonly IDataProcessor dataProcessor;
        private readonly IDataMatcher dataMatcher;
        private readonly IDataQueue dataQueue;
        private readonly List<SerialCommand> serialCommands;
        private readonly SerialSetting serialSetting;
        private static readonly ILog log = LogManager.GetLogger(typeof(SerialCommunicationService));

        public SerialCommunicationService(ISerialCommunication serialComm, IDataProcessor dataProcessor, IDataMatcher dataMatcher, IDataQueue dataQueue, SerialSetting serialSetting, List<SerialCommand> serialCommands)
        {
            this.serialComm = serialComm;
            this.dataProcessor = dataProcessor;
            this.dataMatcher = dataMatcher;
            this.dataQueue = dataQueue;
            this.serialCommands = serialCommands;
            this.serialSetting = serialSetting;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            const int timeoutMilliseconds = 3000;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Set timeout for opening the serial communication
                    var openTimeoutSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));
                    await serialComm.OpenAsync(timeoutMilliseconds, timeoutMilliseconds, openTimeoutSource.Token);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            foreach (var command in serialCommands)
                            {
                                // Set timeout for sending and receiving data
                                var sendReceiveTimeoutSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));

                                await serialComm.SendAsync(command.SendCommand, sendReceiveTimeoutSource.Token);
                                dataMatcher.SetLastSentCommand(command);
                                string receivedData = await serialComm.ReceiveAsync(sendReceiveTimeoutSource.Token);

                                var matchedCommand = dataMatcher.MatchCommand(receivedData, serialCommands);
                                if (matchedCommand != null)
                                {
                                    var processedData = dataProcessor.ProcessReceivedData(receivedData, matchedCommand);
                                    dataQueue.Enqueue(new ReceivedDataWithOffset
                                    {
                                        ReceivedData = processedData,
                                        OffsetAddress = matchedCommand.OffsetAddress
                                    });

                                    log.Info($"Serial port: {serialSetting.PortName} - Received correct data: {receivedData}");
                                }
                                else
                                {
                                    log.Error($"Serial port: {serialSetting.PortName} - Received invalid data: {receivedData}");
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            log.Error($"Serial port: {serialSetting.PortName} - Timeout occurred during sending/receiving data.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Serial port: {serialSetting.PortName} - Serial communication sending / receiving error: {ex.Message}");
                            break; // Break the inner loop to restart the serial communication
                        }
                    }

                    // Set timeout for closing the serial communication
                    var closeTimeoutSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));
                    await serialComm.CloseAsync(closeTimeoutSource.Token);

                }
                catch (OperationCanceledException)
                {
                    log.Error($"Serial port: {serialSetting.PortName} - Timeout occurred during opening/closing serial port.");
                }
                catch (Exception ex)
                {
                    log.Error($"Serial port: {serialSetting.PortName} - Serial port opening / closing error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // Wait before restart
            }

            serialComm.Dispose();
        }
    }
}