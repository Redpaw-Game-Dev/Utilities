using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LazyRedpaw.Utilities.CollectionThrowHelper;

namespace LazyRedpaw.Utilities
{
    [Serializable]
    public class HashPile<T> : ICollection<T>
    {
        private readonly IEqualityComparer<T> _comparer;

        private const int DefaultCapacity = 7;
        private const float LoadFactor = 0.72f;

        [SerializeField] private int[] _buckets;
        [SerializeField] private Slot[] _slots;
        [SerializeField] private int _count;
        [SerializeField] private int _lastIndex;
        [SerializeField] private int _freeList;
        [SerializeField] private int _version;
        [SerializeField] private int _threshold;

        public int Count => _count;
        public bool IsReadOnly => false;

        [Serializable]
        private struct Slot
        {
            public int HashCode;
            public int Next;
            public T Value;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly HashPile<T> _pile;
            private readonly int _version;
            private int _index;
            private T _current;

            internal Enumerator(HashPile<T> pile)
            {
                _pile = pile;
                _version = pile._version;
                _index = 0;
                _current = default!;
            }

            public T Current => _current;
            object IEnumerator.Current => _current!;

            public bool MoveNext()
            {
                if (_version != _pile._version) ThrowInvalidOperation("Collection was modified during enumeration.");
                while (_index < _pile._lastIndex)
                {
                    ref var slot = ref _pile._slots[_index++];
                    if (slot.HashCode >= 0) { _current = slot.Value; return true; }
                }
                _current = default!;
                return false;
            }

            public void Reset()
            {
                if (_version != _pile._version) ThrowInvalidOperation("Collection was modified during enumeration.");
                _index = 0;
                _current = default!;
            }

            public void Dispose() { }
        }

        public Enumerator GetEnumerator() => new(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public HashPile() : this(EqualityComparer<T>.Default) { }

        public HashPile(IEqualityComparer<T> comparer)
        {
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _buckets = Array.Empty<int>();
            _slots = Array.Empty<Slot>();
            _freeList = -1;
            _lastIndex = 0;
            _count = 0;
            _threshold = 0;
        }

        public void Add(T item) => AddInternal(item);
        public void Add(object item) => AddInternal((T)item);
        bool ICollection<T>.Remove(T item) => Remove(item);
        public bool Contains(T item) => FindSlot(item) >= 0;
        public bool Contains(object item) => Contains((T)item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) ThrowArgumentNull(nameof(array));
            if ((uint)arrayIndex > (uint)array.Length) ThrowArgumentOutOfRange(nameof(arrayIndex));
            if (array.Length - arrayIndex < _count) ThrowArgumentException("Target array is too small.");
            var copied = 0;
            for (var i = 0; i < _lastIndex && copied < _count; i++)
            {
                if (_slots[i].HashCode >= 0) array[arrayIndex + copied++] = _slots[i].Value;
            }
        }

        public void Clear()
        {
            if (_lastIndex == 0 && _freeList == -1) return;
            Array.Clear(_buckets, 0, _buckets.Length);
            Array.Clear(_slots, 0, _lastIndex);
            _freeList = -1;
            _lastIndex = 0;
            _count = 0;
            _version++;
        }

        public void EnsureCapacity(int min)
        {
            if (min < 0) ThrowArgumentOutOfRange(nameof(min));
            min = Math.Max(min, DefaultCapacity);
            if (_buckets.Length == 0) { Initialize(min); return; }
            if (_slots.Length >= min) return;
            ResizeToCapacity(HashHelpers.GetPrime(min));
        }

        public bool Remove(object item)
        {
            return Remove((T)item);
        }
        public bool Remove(T item)
        {
            if (_buckets.Length == 0) return false;
            var hash = GetItemHashCode(item);
            var bucket = hash % _buckets.Length;
            var prev = -1;
            var i = _buckets[bucket] - 1;
            while (i >= 0)
            {
                ref var slot = ref _slots[i];
                if (slot.HashCode == hash && _comparer.Equals(slot.Value, item))
                {
                    var next = slot.Next;
                    if (prev < 0) _buckets[bucket] = next;
                    else _slots[prev].Next = next;
                    slot.HashCode = -1;
                    slot.Value = default!;
                    slot.Next = _freeList + 1;
                    _freeList = i;
                    _count--;
                    _version++;
                    return true;
                }
                prev = i;
                i = slot.Next - 1;
            }
            return false;
        }

        public int RemoveAll(T item)
        {
            if (_buckets.Length == 0) return 0;
            var hash = GetItemHashCode(item);
            var bucket = hash % _buckets.Length;
            var removed = 0;
            var prev = -1;
            var i = _buckets[bucket] - 1;
            while (i >= 0)
            {
                ref var slot = ref _slots[i];
                if (slot.HashCode == hash && _comparer.Equals(slot.Value, item))
                {
                    var next = slot.Next;
                    if (prev < 0) _buckets[bucket] = next;
                    else _slots[prev].Next = next;
                    slot.HashCode = -1;
                    slot.Value = default!;
                    slot.Next = _freeList + 1;
                    _freeList = i;
                    removed++;
                    _count--;
                    i = next - 1;
                    continue;
                }
                prev = i;
                i = slot.Next - 1;
            }
            if (removed > 0) _version++;
            return removed;
        }

        public int CountOf(T item)
        {
            if (_buckets.Length == 0) return 0;
            var hash = GetItemHashCode(item);
            var count = 0;
            var i = _buckets[hash % _buckets.Length] - 1;
            while (i >= 0)
            {
                ref var slot = ref _slots[i];
                if (slot.HashCode == hash && _comparer.Equals(slot.Value, item)) count++;
                i = slot.Next - 1;
            }
            return count;
        }

        public void TrimExcess()
        {
            if (_count == 0) { Clear(); return; }
            var newSize = HashHelpers.GetPrime(_count);
            if (newSize >= _buckets.Length) return;
            ShrinkTo(newSize);
        }

        private void AddInternal(T item)
        {
            if (_buckets.Length == 0) Initialize(1);
            var hash = GetItemHashCode(item);
            var bucket = hash % _buckets.Length;
            if (_lastIndex >= _threshold || _lastIndex == _slots.Length)
            {
                Resize();
                bucket = hash % _buckets.Length;
            }
            int index;
            if (_freeList >= 0) { index = _freeList; _freeList = _slots[index].Next - 1; }
            else index = _lastIndex++;
            _slots[index].HashCode = hash;
            _slots[index].Value = item;
            _slots[index].Next = _buckets[bucket];
            _buckets[bucket] = index + 1;
            _count++;
            _version++;
        }

        private int FindSlot(T item)
        {
            if (_buckets.Length == 0) return -1;
            var hash = GetItemHashCode(item);
            var i = _buckets[hash % _buckets.Length] - 1;
            while (i >= 0)
            {
                ref var slot = ref _slots[i];
                if (slot.HashCode == hash && _comparer.Equals(slot.Value, item)) return i;
                i = slot.Next - 1;
            }
            return -1;
        }

        private static int NonNegativeHash(int hash) => hash & 0x7FFFFFFF;
        private int GetItemHashCode(T item) => item is null ? 0 : NonNegativeHash(_comparer.GetHashCode(item));

        private void Initialize(int capacity)
        {
            var size = HashHelpers.GetPrime(capacity);
            _buckets = new int[size];
            _slots = new Slot[size];
            _freeList = -1;
            _lastIndex = 0;
            _count = 0;
            _threshold = (int)(size * LoadFactor);
            _version++;
        }

        private void Resize()
        {
            var newSize = _buckets.Length == 0 ? HashHelpers.GetPrime(DefaultCapacity) : HashHelpers.ExpandPrime(_buckets.Length);
            var newBuckets = new int[newSize];
            var newSlots = new Slot[newSize];
            Array.Copy(_slots, 0, newSlots, 0, _lastIndex);
            for (var i = 0; i < _lastIndex; i++)
            {
                var hash = newSlots[i].HashCode;
                if (hash < 0) continue;
                var bucket = hash % newSize;
                newSlots[i].Next = newBuckets[bucket];
                newBuckets[bucket] = i + 1;
            }
            _buckets = newBuckets;
            _slots = newSlots;
            _threshold = (int)(newSize * LoadFactor);
            _freeList = -1;
        }

        private void ShrinkTo(int newSize)
        {
            var newBuckets = new int[newSize];
            var newSlots = new Slot[newSize];
            var newIndex = 0;
            for (var i = 0; i < _lastIndex; i++)
            {
                if (_slots[i].HashCode < 0) continue;
                ref var oldSlot = ref _slots[i];
                var bucket = oldSlot.HashCode % newSize;
                newSlots[newIndex].HashCode = oldSlot.HashCode;
                newSlots[newIndex].Value = oldSlot.Value;
                newSlots[newIndex].Next = newBuckets[bucket];
                newBuckets[bucket] = newIndex + 1;
                newIndex++;
            }
            _buckets = newBuckets;
            _slots = newSlots;
            _lastIndex = newIndex;
            _freeList = -1;
            _threshold = (int)(newSize * LoadFactor);
            _version++;
        }

        private void ResizeToCapacity(int newSize)
        {
            var newBuckets = new int[newSize];
            var newSlots = new Slot[newSize];
            Array.Copy(_slots, 0, newSlots, 0, _lastIndex);
            for (var i = 0; i < _lastIndex; i++)
            {
                var hash = newSlots[i].HashCode;
                if (hash < 0) continue;
                var bucket = hash % newSize;
                newSlots[i].Next = newBuckets[bucket];
                newBuckets[bucket] = i + 1;
            }
            _buckets = newBuckets;
            _slots = newSlots;
            _threshold = (int)(newSize * LoadFactor);
            _freeList = -1;
            _version++;
        }

        internal static class HashHelpers
        {
            private static readonly int[] Primes =
            {
                3, 7, 11, 17, 23, 29, 37, 47, 59, 71,
                89, 107, 131, 163, 197, 239, 293, 353,
                431, 521, 631, 761, 919, 1103, 1327,
                1597, 1931, 2333, 2801, 3371, 4049,
                4861, 5839, 7013, 8419, 10103, 12143,
                14591, 17519, 21023, 25229, 30293,
                36353, 43627, 52361, 62851, 75431,
                90523, 108631, 130363, 156437, 187751,
                225307, 270371, 324449, 389357, 467237,
                560689, 672827, 807403, 968897, 1162687,
                1395263, 1674319, 2009191, 2411033,
                2893249, 3471899, 4166287, 4999559,
                5999471, 7199369
            };

            public static int GetPrime(int min)
            {
                foreach (var prime in Primes)
                {
                    if (prime >= min) return prime;
                }
                for (var candidate = (min | 1); candidate < int.MaxValue; candidate += 2)
                {
                    if (IsPrime(candidate)) return candidate;
                }
                return min;
            }

            public static int ExpandPrime(int oldSize)
            {
                var newSize = 2 * oldSize;
                if ((uint)newSize > int.MaxValue && int.MaxValue > oldSize) return int.MaxValue;
                return GetPrime(newSize);
            }

            private static bool IsPrime(int candidate)
            {
                if ((candidate & 1) == 0) return candidate == 2;
                var limit = (int)Math.Sqrt(candidate);
                for (var i = 3; i <= limit; i += 2)
                {
                    if (candidate % i == 0) return false;
                }
                return true;
            }
        }
    }
}