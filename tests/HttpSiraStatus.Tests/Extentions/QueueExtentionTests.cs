using System.Collections.Generic;
using HttpSiraStatus.Extentions;
using Xunit;

namespace HttpSiraStatus.Tests.Extentions
{
    public class QueueExtentionTests
    {
        [Fact]
        public void TryDequeue_EmptyQueue_ReturnsFalse()
        {
            var queue = new Queue<int>();

            Assert.False(queue.TryDequeue(out var item));
            Assert.Equal(default, item);
            Assert.Empty(queue);
        }

        [Fact]
        public void TryDequeue_ReturnsItemsInFifoOrder()
        {
            var queue = new Queue<string>(new[] { "first", "second" });

            Assert.True(queue.TryDequeue(out var first));
            Assert.Equal("first", first);

            Assert.True(queue.TryDequeue(out var second));
            Assert.Equal("second", second);

            Assert.False(queue.TryDequeue(out _));
        }

        [Fact]
        public void TryDequeue_CanBeCalledAgainAfterQueueIsRefilled()
        {
            var queue = new Queue<int>();

            Assert.False(queue.TryDequeue(out _));

            queue.Enqueue(1);

            Assert.True(queue.TryDequeue(out var item));
            Assert.Equal(1, item);
        }
    }
}
