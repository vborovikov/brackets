// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

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
*/

#define BRACKETS_64BIT

namespace Brackets.Collections;

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

sealed class StringSet<T> : IStringSet<T> where T : notnull
{
    private const int DefaultCapacity = 256;
    private const int StartOfFreeList = -3;

    private readonly StringComparison comparison;
    private Entry[]? entries;
    private int[]? buckets;
#if BRACKETS_64BIT
    private ulong fastModMultiplier;
#endif

    private int count;
    private int freeList;
    private int freeCount;
    private int version;

    public StringSet(StringComparison comparison) : this(comparison, DefaultCapacity) { }

    public StringSet(StringComparison comparison, int capacity)
    {
        this.comparison = comparison;
        Initialize(capacity);
    }

    public int Count => this.count - this.freeCount;

    public bool Add(string key, T value) => AddIfNotPresent(key, value, out _);

    public bool TryRemove(string key) => Remove(key.AsSpan());

    public bool Contains(string key) => FindItemIndex(key) >= 0;

    public bool Contains(ReadOnlySpan<char> key) => FindItemIndex(key) >= 0;

    public bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out T value)
    {
        if (this.buckets != null)
        {
            int index = FindItemIndex(key);
            if (index >= 0)
            {
                value = this.entries![index].Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public bool Remove(ReadOnlySpan<char> key)
    {
        if (this.buckets != null)
        {
            Entry[]? entries = this.entries;
            Debug.Assert(entries != null, "entries should be non-null");

            uint collisionCount = 0;
            int last = -1;

            int hashCode = string.GetHashCode(key, this.comparison);

            ref int bucket = ref GetBucketRef(hashCode);
            int i = bucket - 1; // Value in buckets is 1-based

            while (i >= 0)
            {
                ref Entry entry = ref entries[i];

                if (entry.HashCode == hashCode && MemoryExtensions.Equals(entry.Key, key, this.comparison))
                {
                    if (last < 0)
                    {
                        bucket = entry.Next + 1; // Value in buckets is 1-based
                    }
                    else
                    {
                        entries[last].Next = entry.Next;
                    }

                    Debug.Assert((StartOfFreeList - this.freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) this.freelist underflow threshold 2147483646");
                    entry.Next = StartOfFreeList - this.freeList;

                    entry.Key = default!;
                    entry.Value = default!;

                    this.freeList = i;
                    this.freeCount++;
                    return true;
                }

                last = i;
                i = entry.Next;

                collisionCount++;
                if (collisionCount > (uint)entries.Length)
                {
                    // The chain of entries forms a loop; which means a concurrent update has happened.
                    // Break out of the loop and throw, rather than looping forever.
                    throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.");
                }
            }
        }

        return false;
    }

    public void Clear()
    {
        int count = this.count;
        if (count > 0)
        {
            Debug.Assert(this.buckets != null, "_buckets should be non-null");
            Debug.Assert(this.entries != null, "_entries should be non-null");

            Array.Clear(this.buckets);
            this.count = 0;
            this.freeList = -1;
            this.freeCount = 0;
            Array.Clear(this.entries, 0, count);
        }
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        this.Count == 0 ? Enumerable.Empty<T>().GetEnumerator() :
        GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();


    [MemberNotNull(nameof(buckets), nameof(entries))]
    private int Initialize(int capacity)
    {
        var size = HashHelpers.GetPrime(capacity);
        var buckets = new int[size];
        var entries = new Entry[size];

        // Assign member variables after both arrays are allocated to guard against corruption from OOM if second fails.
        this.freeList = -1;
        this.buckets = buckets;
        this.entries = entries;
#if BRACKETS_64BIT
        this.fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
#endif

        return size;
    }

    /// <summary>Gets the index of the item in <see cref="entries"/>, or -1 if it's not in the set.</summary>
    private int FindItemIndex(ReadOnlySpan<char> key)
    {
        var buckets = this.buckets;
        if (buckets != null)
        {
            Entry[]? entries = this.entries;
            Debug.Assert(entries != null, "entries should be non-null");
            uint collisionCount = 0;

            var hashCode = string.GetHashCode(key, this.comparison);
            var i = GetBucketRef(hashCode) - 1; // Value in buckets is 1-based
            while (i >= 0)
            {
                ref var entry = ref entries[i];
                if (entry.HashCode == hashCode && MemoryExtensions.Equals(entry.Key, key, this.comparison))
                {
                    return i;
                }
                i = entry.Next;

                collisionCount++;
                if (collisionCount > (uint)entries.Length)
                {
                    // The chain of entries forms a loop, which means a concurrent update has happened.
                    throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.");
                }
            }
        }

        return -1;
    }

    /// <summary>Gets a reference to the specified hashcode's bucket, containing an index into <see cref="entries"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref int GetBucketRef(int hashCode)
    {
        var buckets = this.buckets!;
#if BRACKETS_64BIT
        return ref buckets[HashHelpers.FastMod((uint)hashCode, (uint)buckets.Length, this.fastModMultiplier)];
#else
        return ref buckets[(uint)hashCode % (uint)buckets.Length];
#endif
    }

    private bool AddIfNotPresent(string key, T value, out int location)
    {
        if (this.buckets is null)
        {
            Initialize(0);
        }

        Entry[]? entries = this.entries;
        Debug.Assert(entries != null, "entries should be non-null");
        uint collisionCount = 0;
        ref var bucket = ref Unsafe.NullRef<int>();
        int hashCode;

        hashCode = string.GetHashCode(key, this.comparison);
        bucket = ref GetBucketRef(hashCode);
        var i = bucket - 1; // Value in _buckets is 1-based
        while (i >= 0)
        {
            ref var entry = ref entries[i];
            if (entry.HashCode == hashCode && MemoryExtensions.Equals(entry.Key, key, this.comparison))
            {
                location = i;
                return false;
            }
            i = entry.Next;

            collisionCount++;
            if (collisionCount > (uint)entries.Length)
            {
                // The chain of entries forms a loop, which means a concurrent update has happened.
                throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.");
            }
        }

        int index;
        if (this.freeCount > 0)
        {
            index = this.freeList;
            this.freeCount--;
            this.freeList = StartOfFreeList - entries[this.freeList].Next;
        }
        else
        {
            var count = this.count;
            if (count == entries.Length)
            {
                Resize();
                bucket = ref GetBucketRef(hashCode);
            }
            index = count;
            this.count = count + 1;
            entries = this.entries;
        }

        {
            ref var entry = ref entries![index];
            entry.HashCode = hashCode;
            entry.Next = bucket - 1; // Value in _buckets is 1-based
            entry.Key = key;
            entry.Value = value;
            bucket = index + 1;
            this.version++;
            location = index;
        }

        return true;
    }

    private void Resize() => Resize(HashHelpers.ExpandPrime(this.count));

    private void Resize(int newSize)
    {
        Debug.Assert(this.entries != null, "_entries should be non-null");
        Debug.Assert(newSize >= this.entries.Length);

        var entries = new Entry[newSize];

        var count = this.count;
        Array.Copy(this.entries, entries, count);

        // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
        this.buckets = new int[newSize];
#if BRACKETS_64BIT
        this.fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
#endif
        for (var i = 0; i < count; i++)
        {
            ref var entry = ref entries[i];
            if (entry.Next >= -1)
            {
                ref var bucket = ref GetBucketRef(entry.HashCode);
                entry.Next = bucket - 1; // Value in _buckets is 1-based
                bucket = i + 1;
            }
        }

        this.entries = entries;
    }

    private struct Entry
    {
        public int HashCode;
        /// <summary>
        /// 0-based index of next entry in chain: -1 means end of chain
        /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
        /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
        /// </summary>
        public int Next;
        public string Key;
        public T Value;
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly StringSet<T> stringDir;
        private readonly int version;
        private int index;
        private T current;

        internal Enumerator(StringSet<T> stringDir)
        {
            this.stringDir = stringDir;
            this.version = stringDir.version;
            this.index = 0;
            this.current = default!;
        }

        public bool MoveNext()
        {
            if (this.version != this.stringDir.version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
            // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
            while ((uint)this.index < (uint)this.stringDir.count)
            {
                ref Entry entry = ref this.stringDir.entries![this.index++];
                if (entry.Next >= -1)
                {
                    this.current = entry.Value;
                    return true;
                }
            }

            this.index = this.stringDir.count + 1;
            this.current = default!;
            return false;
        }

        public readonly T Current => this.current;

        public readonly void Dispose() { }

        readonly object? IEnumerator.Current
        {
            get
            {
                if (this.index == 0 || (this.index == this.stringDir.count + 1))
                {
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                }

                return this.current;
            }
        }

        void IEnumerator.Reset()
        {
            if (this.version != this.stringDir.version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            this.index = 0;
            this.current = default!;
        }
    }
}

static class HashHelpers
{
    public const uint HashCollisionThreshold = 100;
    // This is the maximum prime smaller than Array.MaxLength.
    private const int MaxPrimeArrayLength = 0x7FFFFFC3;
    private const int HashPrime = 101;

    // Table of prime numbers to use as hash table sizes.
    // A typical resize algorithm would pick the smallest prime number in this array
    // that is larger than twice the previous capacity.
    // Suppose our Hashtable currently has capacity x and enough elements are added
    // such that a resize needs to occur. Resizing first computes 2x then finds the
    // first prime in the table greater than 2x, i.e. if primes are ordered
    // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n.
    // Doubling is important for preserving the asymptotic complexity of the
    // hashtable operations such as add.  Having a prime guarantees that double
    // hashing does not lead to infinite loops.  IE, your hash function will be
    // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
    // We prefer the low computation costs of higher prime numbers over the increased
    // memory allocation of a fixed prime number i.e. when right sizing a HashSet.
    private static readonly int[] s_primes =
    {
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    };

    private static bool IsPrime(int candidate)
    {
        if ((candidate & 1) != 0)
        {
            var limit = (int)Math.Sqrt(candidate);
            for (var divisor = 3; divisor <= limit; divisor += 2)
            {
                if ((candidate % divisor) == 0)
                    return false;
            }
            return true;
        }
        return candidate == 2;
    }

    public static int GetPrime(int min)
    {
        if (min < 0)
            throw new ArgumentException("Overflow");

        foreach (var prime in s_primes)
        {
            if (prime >= min)
                return prime;
        }

        // Outside of our predefined table. Compute the hard way.
        for (var i = (min | 1); i < int.MaxValue; i += 2)
        {
            if (IsPrime(i) && ((i - 1) % HashPrime != 0))
                return i;
        }
        return min;
    }

    // Returns size of hashtable to grow to.
    public static int ExpandPrime(int oldSize)
    {
        var newSize = 2 * oldSize;

        // Allow the hashtables to grow to maximum possible size (~2G elements) before encountering capacity overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
        {
            Debug.Assert(MaxPrimeArrayLength == GetPrime(MaxPrimeArrayLength), "Invalid MaxPrimeArrayLength");
            return MaxPrimeArrayLength;
        }

        return GetPrime(newSize);
    }

    /// <summary>Returns approximate reciprocal of the divisor: ceil(2**64 / divisor).</summary>
    /// <remarks>This should only be used on 64-bit.</remarks>
    public static ulong GetFastModMultiplier(uint divisor) =>
        ulong.MaxValue / divisor + 1;

    /// <summary>Performs a mod operation using the multiplier pre-computed with <see cref="GetFastModMultiplier"/>.</summary>
    /// <remarks>This should only be used on 64-bit.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FastMod(uint value, uint divisor, ulong multiplier)
    {
        // We use modified Daniel Lemire's fastmod algorithm (https://github.com/dotnet/runtime/pull/406),
        // which allows to avoid the long multiplication if the divisor is less than 2**31.
        Debug.Assert(divisor <= int.MaxValue);

        // This is equivalent of (uint)Math.BigMul(multiplier * value, divisor, out _). This version
        // is faster than BigMul currently because we only need the high bits.
        uint highbits = (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);

        Debug.Assert(highbits == value % divisor);
        return highbits;
    }
}
