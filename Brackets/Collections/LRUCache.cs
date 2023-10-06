namespace Brackets.Collections
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    sealed class LRUCache<TKey, TValue> : IDisposable
        where TKey : notnull
        where TValue : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> cache = new();
        private readonly Queue<TKey> accessOrder = new();
        private readonly object syncRoot = new();
        private readonly int maxCapacity;

        public LRUCache(int maxCapacity)
        {
            this.maxCapacity = maxCapacity;
        }

        public void Dispose()
        {
            this.cache.Clear();
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            return this.cache.GetOrAdd(key, (k, self) =>
            {
                TValue value = valueFactory(k);
                lock (self.syncRoot)
                {
                    self.accessOrder.Enqueue(k);
                    while (self.accessOrder.Count > self.maxCapacity)
                    {
                        TKey oldestKey = self.accessOrder.Dequeue();
                        self.cache.TryRemove(oldestKey, out _);
                    }
                }
                return value;
            }, this);
        }
    }
}
