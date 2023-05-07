﻿using SerialToPlcApp.Logging;
using SerialToPlcApp.Models;
using Sharp7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialToPlcApp.Services
{
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
                                    logger.Log($"Data sent to PLC successfully");
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
}