using HttpSiraStatus.Models;
using Xunit;

namespace HttpSiraStatus.Tests.Models
{
    public class ObjectMemoryPoolTests
    {
        private sealed class Pooled
        {
            public int InitializeCount;
            public int ReinitializeCount;
        }

        [Fact]
        public void Constructor_PrefillsPoolWithInitializedItems()
        {
            var initialized = 0;

            _ = new ObjectMemoryPool<Pooled>(_ => initialized++, _ => { }, 3);

            Assert.Equal(3, initialized);
        }

        [Fact]
        public void Spawn_ReusesPooledItemAndReinitializesIt()
        {
            var pool = new ObjectMemoryPool<Pooled>(_ => { }, item => item.ReinitializeCount++, 1);

            var item = pool.Spawn();
            Assert.Equal(1, item.ReinitializeCount);

            pool.Despawn(item);

            Assert.Same(item, pool.Spawn());
            Assert.Equal(2, item.ReinitializeCount);
        }

        [Fact]
        public void Spawn_WhenEmpty_CreatesAndInitializesNewItem()
        {
            var pool = new ObjectMemoryPool<Pooled>(
                item => item.InitializeCount++,
                item => item.ReinitializeCount++,
                1);

            var pooled = pool.Spawn();
            var created = pool.Spawn();

            Assert.NotSame(pooled, created);
            Assert.Equal(1, created.InitializeCount);
            Assert.Equal(1, created.ReinitializeCount);
        }

        [Fact]
        public void Despawn_ReturnsItemToPool()
        {
            var pool = new ObjectMemoryPool<Pooled>(_ => { }, _ => { }, 0);

            var item = pool.Spawn();
            pool.Despawn(item);

            Assert.Same(item, pool.Spawn());
        }
    }
}
