using System;
using System.Threading;
using System.Threading.Tasks;
using Sharp7;
using SerialToPlcApp.Models;
using SerialToPlcApp.DataProcessing;
using SerialToPlcApp.Queues;
using log4net;

namespace SerialToPlcApp.Services
{
    public interface IPlcCommunicationService
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
    public class PlcCommunicationService : IPlcCommunicationService
    {
        private readonly IDataQueue dataQueue;
        private readonly PlcSetting plcSetting;
        private readonly PlcCommunication plcComm;
        private static readonly ILog log = LogManager.GetLogger(typeof(PlcCommunicationService));

        public PlcCommunicationService(DataQueue dataQueue, PlcSetting plcSetting)
        {
            this.dataQueue = dataQueue;
            this.plcSetting = plcSetting;
            this.plcComm = new PlcCommunication(plcSetting.IpAddress, plcSetting.Rack, plcSetting.Slot);
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
                                    log.Error($"IP: {plcSetting.IpAddress} - Error: Data processing failed for received data: {receivedData}");
                                    continue; // Skip this iteration and move on to the next
                                }

                                // Write the processed data to the PLC
                                int startAddress = plcSetting.StartAddress + receivedDataWithOffset.OffsetAddress;
                                int result = plcComm.WriteData(plcSetting.DbNumber, startAddress, receivedData);

                                if (result == 0)
                                {
                                    log.Info($"IP: {plcSetting.IpAddress} - Data to PLC sent successfully");
                                }


                            }
                            else
                            {
                                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken); // Wait before restart
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error($"IP: {plcSetting.IpAddress} - PLC sending error: {ex.Message}");
                            break; // Break the inner loop to restart the PLC communication
                        }
                    }

                    plcComm.Close();


                }
                catch (Exception ex)
                {
                    log.Error($"IP: {plcSetting.IpAddress} - PLC communication error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // Wait before reopening conncection
            }

            plcComm.Dispose();

        }

    }
}
