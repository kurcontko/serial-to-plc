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
            const int timeoutMilliseconds = 5000; 

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {

                    // Open the connection with a timeout
                    await OpenWithTimeout(timeoutMilliseconds);

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

                                // Write the processed data to the PLC with a timeout
                                int startAddress = plcSetting.StartAddress + receivedDataWithOffset.OffsetAddress;
                                int result = await WriteDataWithTimeout(plcSetting.DbNumber, startAddress, receivedData, timeoutMilliseconds);

                                if (result == 0)
                                {
                                    log.Info($"IP: {plcSetting.IpAddress} - Data to PLC sent successfully");
                                }
                                else
                                {
                                    log.Info($"IP: {plcSetting.IpAddress} - Sending data to PLC failed");
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

                    // Close the connection with a timeout
                    await CloseWithTimeout(timeoutMilliseconds);


                }
                catch (Exception ex)
                {
                    log.Error($"IP: {plcSetting.IpAddress} - PLC communication error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // Wait before reopening conncection
            }

            plcComm.Dispose();

        }

        public async Task OpenWithTimeout(int timeoutMilliseconds)
        {
            var cts = new CancellationTokenSource(); // Create a cancellation token source
            cts.CancelAfter(timeoutMilliseconds); // Set the timeout

            // Start a new task to run the potentially long-running operation
            var task = Task.Run(() => plcComm.Open(), cts.Token);

            try
            {
                // Wait for the task to complete or the timeout to expire
                await task;
            }
            catch (OperationCanceledException)
            {
                // Handle the timeout here
                log.Error($"IP: {plcSetting.IpAddress} - Timeout after {timeoutMilliseconds}ms");
            }
        }

        public async Task<int> WriteDataWithTimeout(int dbNumber, int startAddress, byte[] receivedData, int timeoutMilliseconds)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeoutMilliseconds);

            var task = Task.Run(() => plcComm.WriteData(dbNumber, startAddress, receivedData), cts.Token);

            try
            {
                return await task;
            }
            catch (OperationCanceledException)
            {
                log.Error($"IP: {plcSetting.IpAddress} - WriteData timeout after {timeoutMilliseconds}ms");
                return -1; // Indicate an error
            }
        }

        public async Task CloseWithTimeout(int timeoutMilliseconds)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeoutMilliseconds);

            var task = Task.Run(() => plcComm.Close(), cts.Token);

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                log.Error($"IP: {plcSetting.IpAddress} - Close timeout after {timeoutMilliseconds}ms");
            }
        }
    }
}
