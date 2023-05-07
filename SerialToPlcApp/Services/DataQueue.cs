using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SerialToPlcApp.Models;

namespace SerialToPlcApp.Services
{
    public interface IDataQueue
    {
        void Enqueue(ReceivedDataWithOffset item);
        bool TryDequeue(out ReceivedDataWithOffset item);
    }
    public class DataQueue : IDataQueue
    {
        private readonly ConcurrentQueue<ReceivedDataWithOffset> queue = new();
        private readonly int maxSize;

        public DataQueue(int maxSize = 500) // Limit the queue size to prevent memory overloading
        {
            this.maxSize = maxSize;
        }

        public void Enqueue(ReceivedDataWithOffset item)
        {
            queue.Enqueue(item);

            // Remove the oldest item if the queue size exceeds the maximum size
            while (queue.Count > maxSize)
            {
                queue.TryDequeue(out _);
            }
        }

        public bool TryDequeue(out ReceivedDataWithOffset item)
        {
            return queue.TryDequeue(out item);
        }
    }
}
