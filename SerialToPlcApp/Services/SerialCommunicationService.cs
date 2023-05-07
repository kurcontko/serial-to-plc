﻿using SerialToPlcApp.Logging;
using SerialToPlcApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialToPlcApp.Services
{
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
        private readonly List<SerialCommands> serialCommands;
        private readonly ILogger logger;

        public SerialCommunicationService(DataProcessor dataProcessor, DataQueue dataQueue, DeviceSetting deviceSetting, List<SerialCommands> serialCommands, ILogger logger, DataMatcher dataMatcher, bool useMock)
        {
            this.serialComm = useMock ? (ISerialCommunication)new SerialCommunicationMock() : new SerialCommunication(deviceSetting.PortName, deviceSetting.BaudRate);
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
}