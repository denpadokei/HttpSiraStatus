using System.Collections.Generic;
using System.Linq;

namespace HttpSiraStatus.Extentions
{
    public static class QueueExtention
    {
        public static bool TryDequeue<T>(this Queue<T> queue, out T item)
        {
            if (queue.Any()) {
                item = queue.Dequeue();
                return true;
            }
            else {
                item = default;
                return false;
            }
        }
    }
}
