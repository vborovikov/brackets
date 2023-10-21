/*
MIT License

Copyright (c) 2019 Bar Arnon

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

==============================================================================

CoreFX (https://github.com/dotnet/corefx)
The MIT License (MIT)
Copyright (c) .NET Foundation and Contributors
*/

namespace Brackets.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

/// <summary>
/// Represents a thread-safe hash-based unique collection.
/// </summary>
/// <typeparam name="T">The type of the items in the collection.</typeparam>
/// <remarks>
/// All public members of <see cref="ConcurrentStringSet{T}"/> are thread-safe and may be used
/// concurrently from multiple threads.
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
sealed class ConcurrentStringSet<T> : IStringSet<T>
    where T : notnull
{
    private const int DefaultCapacity = 256;
    private const int MaxLockNumber = 1024;

    private readonly StringComparison comparison;
    private readonly bool growLockArray;

    private int budget;
    private volatile Tables tables;

    private static int DefaultConcurrencyLevel => Environment.ProcessorCount;

    /// <summary>
    /// Gets the number of items contained in the <see
    /// cref="ConcurrentStringSet{T}"/>.
    /// </summary>
    /// <value>The number of items contained in the <see
    /// cref="ConcurrentStringSet{T}"/>.</value>
    /// <remarks>Count has snapshot semantics and represents the number of items in the <see
    /// cref="ConcurrentStringSet{T}"/>
    /// at the moment when Count was accessed.</remarks>
    public int Count
    {
        get
        {
            var count = 0;
            var acquiredLocks = 0;
            try
            {
                AcquireAllLocks(ref acquiredLocks);

                var countPerLocks = this.tables.CountPerLock;
                for (var i = 0; i < countPerLocks.Length; i++)
                {
                    count += countPerLocks[i];
                }
            }
            finally
            {
                ReleaseLocks(0, acquiredLocks);
            }

            return count;
        }
    }

    /// <summary>
    /// Gets a value that indicates whether the <see cref="ConcurrentStringSet{T}"/> is empty.
    /// </summary>
    /// <value>true if the <see cref="ConcurrentStringSet{T}"/> is empty; otherwise,
    /// false.</value>
    public bool IsEmpty
    {
        get
        {
            if (!AreAllBucketsEmpty())
            {
                return false;
            }

            var acquiredLocks = 0;
            try
            {
                AcquireAllLocks(ref acquiredLocks);

                return AreAllBucketsEmpty();
            }
            finally
            {
                ReleaseLocks(0, acquiredLocks);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentStringSet{T}"/>
    /// class that is empty, has the specified concurrency level and capacity, and uses the specified
    /// <see cref="StringComparison"/>.
    /// </summary>
    /// <param name="comparison">The <see cref="StringComparison"/>
    /// implementation to use when comparing items.</param>
    public ConcurrentStringSet(StringComparison comparison)
        : this(DefaultConcurrencyLevel, DefaultCapacity, true, comparison)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentStringSet{T}"/>
    /// class that contains elements copied from the specified <see
    /// cref="IDictionary{TKey, TValue}"/>, has the default concurrency level, has the default
    /// initial capacity, and uses the specified
    /// <see cref="StringComparison"/>.
    /// </summary>
    /// <param name="collection">The <see
    /// cref="IDictionary{TKey, TValue}"/> whose elements are copied to
    /// the new
    /// <see cref="ConcurrentStringSet{T}"/>.</param>
    /// <param name="comparison">The <see cref="StringComparison"/>
    /// implementation to use when comparing items.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is a null reference
    /// (Nothing in Visual Basic).
    /// </exception>
    public ConcurrentStringSet(IDictionary<string, T> collection, StringComparison comparison)
        : this(comparison)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));

        InitializeFromCollection(collection);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentStringSet{T}"/> 
    /// class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>, 
    /// has the specified concurrency level, has the specified initial capacity, and uses the specified 
    /// <see cref="StringComparison"/>.
    /// </summary>
    /// <param name="concurrencyLevel">The estimated number of threads that will update the 
    /// <see cref="ConcurrentStringSet{T}"/> concurrently.</param>
    /// <param name="collection">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new 
    /// <see cref="ConcurrentStringSet{T}"/>.</param>
    /// <param name="comparison">The <see cref="StringComparison"/> implementation to use 
    /// when comparing items.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collection"/> is a null reference.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="concurrencyLevel"/> is less than 1.
    /// </exception>
    public ConcurrentStringSet(int concurrencyLevel, IDictionary<string, T> collection, StringComparison comparison)
        : this(concurrencyLevel, DefaultCapacity, false, comparison)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));

        InitializeFromCollection(collection);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentStringSet{T}"/>
    /// class that is empty, has the specified concurrency level, has the specified initial capacity, and
    /// uses the specified <see cref="StringComparison"/>.
    /// </summary>
    /// <param name="concurrencyLevel">The estimated number of threads that will update the
    /// <see cref="ConcurrentStringSet{T}"/> concurrently.</param>
    /// <param name="capacity">The initial number of elements that the <see
    /// cref="ConcurrentStringSet{T}"/>
    /// can contain.</param>
    /// <param name="comparison">The <see cref="StringComparison"/>
    /// implementation to use when comparing items.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="concurrencyLevel"/> is less than 1. -or-
    /// <paramref name="capacity"/> is less than 0.
    /// </exception>
    public ConcurrentStringSet(int concurrencyLevel, int capacity, StringComparison comparison)
        : this(concurrencyLevel, capacity, false, comparison)
    {
    }

    private ConcurrentStringSet(int concurrencyLevel, int capacity, bool growLockArray, StringComparison comparer)
    {
        if (concurrencyLevel < 1) throw new ArgumentOutOfRangeException(nameof(concurrencyLevel));
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));

        // The capacity should be at least as large as the concurrency level. Otherwise, we would have locks that don't guard
        // any buckets.
        if (capacity < concurrencyLevel)
        {
            capacity = concurrencyLevel;
        }

        var locks = new object[concurrencyLevel];
        for (var i = 0; i < locks.Length; i++)
        {
            locks[i] = new object();
        }

        var countPerLock = new int[locks.Length];
        var buckets = new Node[capacity];
        this.tables = new Tables(buckets, locks, countPerLock);

        this.growLockArray = growLockArray;
        this.budget = buckets.Length / locks.Length;
        this.comparison = comparer;
    }

    /// <summary>
    /// Adds the specified item to the <see cref="ConcurrentStringSet{T}"/>.
    /// </summary>
    /// <param name="key">The key of the item to add.</param>
    /// <param name="value">The value of the item to add.</param>
    /// <returns>true if the item was added to the <see cref="ConcurrentStringSet{T}"/>
    /// successfully; false if it already exists.</returns>
    /// <exception cref="OverflowException">The <see cref="ConcurrentStringSet{T}"/>
    /// contains too many items.</exception>
    public bool Add(string key, T value) =>
        AddInternal(key, value, true);

    /// <summary>
    /// Removes all items from the <see cref="ConcurrentStringSet{T}"/>.
    /// </summary>
    public void Clear()
    {
        var locksAcquired = 0;
        try
        {
            AcquireAllLocks(ref locksAcquired);

            if (AreAllBucketsEmpty())
            {
                return;
            }

            var tables = this.tables;
            var newTables = new Tables(new Node[DefaultCapacity], tables.Locks, new int[tables.CountPerLock.Length]);
            this.tables = newTables;
            this.budget = Math.Max(1, newTables.Buckets.Length / newTables.Locks.Length);
        }
        finally
        {
            ReleaseLocks(0, locksAcquired);
        }
    }

    /// <summary>
    /// Determines whether the <see cref="ConcurrentStringSet{T}"/> contains the specified item.
    /// </summary>
    /// <param name="key">The key of the item to locate in the <see cref="ConcurrentStringSet{T}"/>.</param>
    /// <returns>true if the <see cref="ConcurrentStringSet{T}"/> contains the item; otherwise, false.</returns>
    public bool Contains(ReadOnlySpan<char> key) => TryGetValue(key, out _);

    /// <summary>
    /// Searches the <see cref="ConcurrentStringSet{T}"/> for a given value and returns the equal value it finds, if any.
    /// </summary>
    /// <param name="key">The key of an item to search for.</param>
    /// <param name="value">The value of an item from the set that the search found, or the default value of <typeparamref name="T"/> when the search yielded no match.</param>
    /// <returns>A value indicating whether the search was successful.</returns>
    public bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out T value)
    {
        var hashcode = string.GetHashCode(key, this.comparison);

        // We must capture the _buckets field in a local variable. It is set to a new table on each table resize.
        var tables = this.tables;

        var bucketNo = GetBucket(hashcode, tables.Buckets.Length);

        // We can get away w/out a lock here.
        // The Volatile.Read ensures that the load of the fields of 'n' doesn't move before the load from buckets[i].
        var current = Volatile.Read(ref tables.Buckets[bucketNo]);

        while (current != null)
        {
            if (hashcode == current.Hashcode && key.Equals(current.Key, this.comparison))
            {
                value = current.Value;
                return true;
            }

            current = current.Next;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to remove the item from the <see cref="ConcurrentStringSet{T}"/>.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    /// <returns>true if an item was removed successfully; otherwise, false.</returns>
    public bool TryRemove(ReadOnlySpan<char> key)
    {
        var hashcode = string.GetHashCode(key, this.comparison);
        while (true)
        {
            var tables = this.tables;

            GetBucketAndLockNo(hashcode, out int bucketNo, out int lockNo, tables.Buckets.Length, tables.Locks.Length);

            lock (tables.Locks[lockNo])
            {
                // If the table just got resized, we may not be holding the right lock, and must retry.
                // This should be a rare occurrence.
                if (tables != this.tables)
                {
                    continue;
                }

                Node? previous = null;
                for (var current = tables.Buckets[bucketNo]; current != null; current = current.Next)
                {
                    Debug.Assert((previous == null && current == tables.Buckets[bucketNo]) || previous!.Next == current);

                    if (hashcode == current.Hashcode && key.Equals(current.Key, this.comparison))
                    {
                        if (previous == null)
                        {
                            Volatile.Write(ref tables.Buckets[bucketNo], current.Next);
                        }
                        else
                        {
                            previous.Next = current.Next;
                        }

                        tables.CountPerLock[lockNo]--;
                        return true;
                    }
                    previous = current;
                }
            }

            return false;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    /// <summary>Returns an enumerator that iterates through the <see
    /// cref="ConcurrentStringSet{T}"/>.</summary>
    /// <returns>An enumerator for the <see cref="ConcurrentStringSet{T}"/>.</returns>
    /// <remarks>
    /// The enumerator returned from the collection is safe to use concurrently with
    /// reads and writes to the collection, however it does not represent a moment-in-time snapshot
    /// of the collection.  The contents exposed through the enumerator may contain modifications
    /// made to the collection after <see cref="IEnumerable{T}.GetEnumerator"/> was called.
    /// </remarks>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    /// <summary>Returns a value-type enumerator that iterates through the <see
    /// cref="ConcurrentStringSet{T}"/>.</summary>
    /// <returns>An enumerator for the <see cref="ConcurrentStringSet{T}"/>.</returns>
    /// <remarks>
    /// The enumerator returned from the collection is safe to use concurrently with
    /// reads and writes to the collection, however it does not represent a moment-in-time snapshot
    /// of the collection.  The contents exposed through the enumerator may contain modifications
    /// made to the collection after <see cref="GetEnumerator"/> was called.
    /// </remarks>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Represents an enumerator for <see cref="ConcurrentStringSet{T}" />.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        // Provides a manually-implemented version of (approximately) this iterator:
        //     Node?[] buckets = _tables.Buckets;
        //     for (int i = 0; i < buckets.Length; i++)
        //         for (Node? current = Volatile.Read(ref buckets[i]); current != null; current = current.Next)
        //             yield return new current.Item;

        private readonly ConcurrentStringSet<T> _set;

        private Node?[]? _buckets;
        private Node? _node;
        private int _i;
        private int _state;

        private const int StateUninitialized = 0;
        private const int StateOuterloop = 1;
        private const int StateInnerLoop = 2;
        private const int StateDone = 3;

        /// <summary>
        /// Constructs an enumerator for <see cref="ConcurrentStringSet{T}" />.
        /// </summary>
        public Enumerator(ConcurrentStringSet<T> set)
        {
            this._set = set;
            this._buckets = null;
            this._node = null;
            this.Current = default!;
            this._i = -1;
            this._state = StateUninitialized;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value>The element in the collection at the current position of the enumerator.</value>
        public T Current { get; private set; }

        object? IEnumerator.Current => this.Current;

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            this._buckets = null;
            this._node = null;
            this.Current = default!;
            this._i = -1;
            this._state = StateUninitialized;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            switch (this._state)
            {
                case StateUninitialized:
                    this._buckets = this._set.tables.Buckets;
                    this._i = -1;
                    goto case StateOuterloop;

                case StateOuterloop:
                    Node?[]? buckets = this._buckets;
                    Debug.Assert(buckets != null);

                    int i = ++this._i;
                    if ((uint)i < (uint)buckets!.Length)
                    {
                        // The Volatile.Read ensures that we have a copy of the reference to buckets[i]:
                        // this protects us from reading fields ('_key', '_value' and '_next') of different instances.
                        this._node = Volatile.Read(ref buckets[i]);
                        this._state = StateInnerLoop;
                        goto case StateInnerLoop;
                    }
                    goto default;

                case StateInnerLoop:
                    Node? node = this._node;
                    if (node != null)
                    {
                        this.Current = node.Value;
                        this._node = node.Next;
                        return true;
                    }
                    goto case StateOuterloop;

                default:
                    this._state = StateDone;
                    return false;
            }
        }
    }

    private void InitializeFromCollection(IDictionary<string, T> collection)
    {
        foreach (var item in collection)
        {
            AddInternal(item.Key, item.Value, false);
        }

        if (this.budget == 0)
        {
            var tables = this.tables;
            this.budget = tables.Buckets.Length / tables.Locks.Length;
        }
    }

    private bool AddInternal(string key, T value, bool acquireLock)
    {
        var hashcode = string.GetHashCode(key, this.comparison);
        while (true)
        {
            var tables = this.tables;

            GetBucketAndLockNo(hashcode, out int bucketNo, out int lockNo, tables.Buckets.Length, tables.Locks.Length);

            var resizeDesired = false;
            var lockTaken = false;
            try
            {
                if (acquireLock)
                    Monitor.Enter(tables.Locks[lockNo], ref lockTaken);

                // If the table just got resized, we may not be holding the right lock, and must retry.
                // This should be a rare occurrence.
                if (tables != this.tables)
                {
                    continue;
                }

                // Try to find this item in the bucket
                Node? previous = null;
                for (var current = tables.Buckets[bucketNo]; current != null; current = current.Next)
                {
                    Debug.Assert(previous == null && current == tables.Buckets[bucketNo] || previous!.Next == current);
                    if (hashcode == current.Hashcode && key.Equals(current.Key, this.comparison))
                    {
                        return false;
                    }
                    previous = current;
                }

                // The item was not found in the bucket. Insert the new item.
                Volatile.Write(ref tables.Buckets[bucketNo], new Node(key, value, hashcode, tables.Buckets[bucketNo]));
                checked
                {
                    tables.CountPerLock[lockNo]++;
                }

                //
                // If the number of elements guarded by this lock has exceeded the budget, resize the bucket table.
                // It is also possible that GrowTable will increase the budget but won't resize the bucket table.
                // That happens if the bucket table is found to be poorly utilized due to a bad hash function.
                //
                if (tables.CountPerLock[lockNo] > this.budget)
                {
                    resizeDesired = true;
                }
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(tables.Locks[lockNo]);
            }

            //
            // The fact that we got here means that we just performed an insertion. If necessary, we will grow the table.
            //
            // Concurrency notes:
            // - Notice that we are not holding any locks at when calling GrowTable. This is necessary to prevent deadlocks.
            // - As a result, it is possible that GrowTable will be called unnecessarily. But, GrowTable will obtain lock 0
            //   and then verify that the table we passed to it as the argument is still the current table.
            //
            if (resizeDesired)
            {
                GrowTable(tables);
            }

            return true;
        }
    }

    private static int GetBucket(int hashcode, int bucketCount)
    {
        var bucketNo = (hashcode & 0x7fffffff) % bucketCount;
        Debug.Assert(bucketNo >= 0 && bucketNo < bucketCount);
        return bucketNo;
    }

    private static void GetBucketAndLockNo(int hashcode, out int bucketNo, out int lockNo, int bucketCount, int lockCount)
    {
        bucketNo = (hashcode & 0x7fffffff) % bucketCount;
        lockNo = bucketNo % lockCount;

        Debug.Assert(bucketNo >= 0 && bucketNo < bucketCount);
        Debug.Assert(lockNo >= 0 && lockNo < lockCount);
    }

    private bool AreAllBucketsEmpty()
    {
        var countPerLock = this.tables.CountPerLock;
        for (var i = 0; i < countPerLock.Length; i++)
        {
            if (countPerLock[i] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private void GrowTable(Tables tables)
    {
        const int maxArrayLength = 0X7FEFFFFF;
        var locksAcquired = 0;
        try
        {
            // The thread that first obtains _locks[0] will be the one doing the resize operation
            AcquireLocks(0, 1, ref locksAcquired);

            // Make sure nobody resized the table while we were waiting for lock 0:
            if (tables != this.tables)
            {
                // We assume that since the table reference is different, it was already resized (or the budget
                // was adjusted). If we ever decide to do table shrinking, or replace the table for other reasons,
                // we will have to revisit this logic.
                return;
            }

            // Compute the (approx.) total size. Use an Int64 accumulation variable to avoid an overflow.
            long approxCount = 0;
            for (var i = 0; i < tables.CountPerLock.Length; i++)
            {
                approxCount += tables.CountPerLock[i];
            }

            //
            // If the bucket array is too empty, double the budget instead of resizing the table
            //
            if (approxCount < tables.Buckets.Length / 4)
            {
                this.budget = 2 * this.budget;
                if (this.budget < 0)
                {
                    this.budget = int.MaxValue;
                }
                return;
            }

            // Compute the new table size. We find the smallest integer larger than twice the previous table size, and not divisible by
            // 2,3,5 or 7. We can consider a different table-sizing policy in the future.
            var newLength = 0;
            var maximizeTableSize = false;
            try
            {
                checked
                {
                    // Double the size of the buckets table and add one, so that we have an odd integer.
                    newLength = tables.Buckets.Length * 2 + 1;

                    // Now, we only need to check odd integers, and find the first that is not divisible
                    // by 3, 5 or 7.
                    while (newLength % 3 == 0 || newLength % 5 == 0 || newLength % 7 == 0)
                    {
                        newLength += 2;
                    }

                    Debug.Assert(newLength % 2 != 0);

                    if (newLength > maxArrayLength)
                    {
                        maximizeTableSize = true;
                    }
                }
            }
            catch (OverflowException)
            {
                maximizeTableSize = true;
            }

            if (maximizeTableSize)
            {
                newLength = maxArrayLength;

                // We want to make sure that GrowTable will not be called again, since table is at the maximum size.
                // To achieve that, we set the budget to int.MaxValue.
                //
                // (There is one special case that would allow GrowTable() to be called in the future: 
                // calling Clear() on the ConcurrentHashSet will shrink the table and lower the budget.)
                this.budget = int.MaxValue;
            }

            // Now acquire all other locks for the table
            AcquireLocks(1, tables.Locks.Length, ref locksAcquired);

            var newLocks = tables.Locks;

            // Add more locks
            if (this.growLockArray && tables.Locks.Length < MaxLockNumber)
            {
                newLocks = new object[tables.Locks.Length * 2];
                Array.Copy(tables.Locks, newLocks, tables.Locks.Length);
                for (var i = tables.Locks.Length; i < newLocks.Length; i++)
                {
                    newLocks[i] = new object();
                }
            }

            var newBuckets = new Node[newLength];
            var newCountPerLock = new int[newLocks.Length];

            // Copy all data into a new table, creating new nodes for all elements
            for (var i = 0; i < tables.Buckets.Length; i++)
            {
                var current = tables.Buckets[i];
                while (current != null)
                {
                    var next = current.Next;
                    GetBucketAndLockNo(current.Hashcode, out int newBucketNo, out int newLockNo, newBuckets.Length, newLocks.Length);

                    newBuckets[newBucketNo] = new Node(current.Key, current.Value, current.Hashcode, newBuckets[newBucketNo]);

                    checked
                    {
                        newCountPerLock[newLockNo]++;
                    }

                    current = next;
                }
            }

            // Adjust the budget
            this.budget = Math.Max(1, newBuckets.Length / newLocks.Length);

            // Replace tables with the new versions
            this.tables = new ConcurrentStringSet<T>.Tables(newBuckets, newLocks, newCountPerLock);
        }
        finally
        {
            // Release all locks that we took earlier
            ReleaseLocks(0, locksAcquired);
        }
    }

    private void AcquireAllLocks(ref int locksAcquired)
    {
        // First, acquire lock 0
        AcquireLocks(0, 1, ref locksAcquired);

        // Now that we have lock 0, the _locks array will not change (i.e., grow),
        // and so we can safely read _locks.Length.
        AcquireLocks(1, this.tables.Locks.Length, ref locksAcquired);
        Debug.Assert(locksAcquired == this.tables.Locks.Length);
    }

    private void AcquireLocks(int fromInclusive, int toExclusive, ref int locksAcquired)
    {
        Debug.Assert(fromInclusive <= toExclusive);
        var locks = this.tables.Locks;

        for (var i = fromInclusive; i < toExclusive; i++)
        {
            var lockTaken = false;
            try
            {
                Monitor.Enter(locks[i], ref lockTaken);
            }
            finally
            {
                if (lockTaken)
                {
                    locksAcquired++;
                }
            }
        }
    }

    private void ReleaseLocks(int fromInclusive, int toExclusive)
    {
        Debug.Assert(fromInclusive <= toExclusive);

        for (var i = fromInclusive; i < toExclusive; i++)
        {
            Monitor.Exit(this.tables.Locks[i]);
        }
    }

    private sealed class Tables
    {
        public readonly Node?[] Buckets;
        public readonly object[] Locks;

        public readonly int[] CountPerLock;

        public Tables(Node?[] buckets, object[] locks, int[] countPerLock)
        {
            this.Buckets = buckets;
            this.Locks = locks;
            this.CountPerLock = countPerLock;
        }
    }

    private sealed class Node
    {
        public readonly string Key;
        public readonly T Value;
        public readonly int Hashcode;

        public volatile Node? Next;

        public Node(string key, T value, int hashcode, Node? next)
        {
            this.Key = key;
            this.Value = value;
            this.Hashcode = hashcode;
            this.Next = next;
        }
    }
}