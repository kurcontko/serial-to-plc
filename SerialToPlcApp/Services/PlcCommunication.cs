using System;
using Sharp7;

namespace SerialToPlcApp.Services
{
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


}
