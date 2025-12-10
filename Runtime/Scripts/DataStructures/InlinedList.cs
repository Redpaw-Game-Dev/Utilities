using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LazyRedpaw.Utilities
{
    [Serializable]
    public class InlinedList<T> : IList<T>, ISerializationCallbackReceiver where T : IEquatable<T>
    {
        [SerializeReference] private T[] _s = Array.Empty<T>();

        private T _firstItem;
        private T[]? _additionalItems;
        private int _count;
        private bool _initialized;

        private const int InlineCapacity = 1;
        private static readonly EqualityComparer<T> _comparer = EqualityComparer<T>.Default;

        public int Count
        {
            get
            {
                Init();
                return _count;
            }
        }

        bool ICollection<T>.IsReadOnly => false;

        public int Capacity
        {
            get
            {
                Init();
                return InlineCapacity + (_additionalItems?.Length ?? 0);
            }
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Init();
                if ((uint)index >= (uint)_count) CollectionThrowHelper.ThrowArgumentOutOfRange();
                return index == 0 ? _firstItem : _additionalItems![index - 1];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Init();
                if ((uint)index >= (uint)_count) CollectionThrowHelper.ThrowArgumentOutOfRange();
                if (index == 0) _firstItem = value;
                else _additionalItems![index - 1] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InlinedList()
        {
            _s = Array.Empty<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InlinedList(int capacity)
        {
            _s = Array.Empty<T>();
            _firstItem = default!;
            _additionalItems = capacity > InlineCapacity ? new T[capacity - InlineCapacity] : null;
            _count = 0;
            _initialized = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InlinedList(IEnumerable<T> collection)
        {
            _s = null;
            if (collection == null) CollectionThrowHelper.ThrowArgumentNull(nameof(collection));
            _firstItem = default!;
            _additionalItems = null;
            _count = 0;
            _initialized = true;
            foreach (var item in collection)
            {
                Add(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Init()
        {
            if (_initialized) return;
            _firstItem = default!;
            _additionalItems = null;
            _count = 0;
            _initialized = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int min)
        {
            Init();
            int current = Capacity;
            if (current >= min) return;
            int newCap = current == 0 ? InlineCapacity : current << 1;
            if (newCap < min) newCap = min;
            int overflow = newCap - InlineCapacity;
            T[] arr = new T[overflow];
            if (_additionalItems != null) Array.Copy(_additionalItems, arr, Math.Min(_count - 1, overflow));
            _additionalItems = arr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetItem(int index)
        {
            return index == 0 ? _firstItem : _additionalItems![index - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetItem(int index, T value)
        {
            if (index == 0) _firstItem = value;
            else _additionalItems![index - 1] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            Init();
            int c = _count;
            if (c == 0)
            {
                _firstItem = item;
                _count = 1;
                return;
            }
            int idx = c - 1;
            if (_additionalItems == null || idx >= _additionalItems.Length)
            {
                EnsureCapacity(c + 1);
            }
            _additionalItems![idx] = item;
            _count = c + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            Init();
            if ((uint)index > (uint)_count) CollectionThrowHelper.ThrowArgumentOutOfRange();
            int c = _count;
            EnsureCapacity(c + 1);
            if (index == 0)
            {
                if (c > 0)
                {
                    if (c > 1)
                    {
                        _additionalItems.AsSpan(0, c - 1).CopyTo(_additionalItems.AsSpan(1));
                    }
                    _additionalItems[0] = _firstItem;
                }

                _firstItem = item;
                _count = c + 1;
                return;
            }
            int spanCount = c - index;
            if (spanCount > 0)
            {
                _additionalItems.AsSpan(index - 1, spanCount).CopyTo(_additionalItems.AsSpan(index));
            }
            _additionalItems![index - 1] = item;
            _count = c + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
            Init();
            if ((uint)index >= (uint)_count) CollectionThrowHelper.ThrowArgumentOutOfRange();
            int c = _count;
            if (index == 0)
            {
                if (c > 1) _firstItem = _additionalItems![0];
                if (c > 2)
                {
                    _additionalItems.AsSpan(1, c - 2).CopyTo(_additionalItems.AsSpan(0));
                }
                _count = c - 1;
                return;
            }
            int spanCount = c - index - 1;
            if (spanCount > 0)
            {
                _additionalItems.AsSpan(index, spanCount).CopyTo(_additionalItems.AsSpan(index - 1));
            }
            _count = c - 1;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            Init();
            int i = IndexOf(item);
            if (i < 0) return false;
            RemoveAt(i);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item)
        {
            Init();
            if (_count == 0) return -1;
            if (_comparer.Equals(_firstItem, item)) return 0;
            if (_additionalItems == null) return -1;
            int searchCount = _count - 1;
            if (searchCount <= 0) return -1;
            int index = _additionalItems.AsSpan(0, searchCount).IndexOf(item);
            return index < 0 ? -1 : index + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Init();
            if (_count == 0) return;
            _firstItem = default!;
            if (_additionalItems != null && _count > 1) Array.Clear(_additionalItems, 0, _count - 1);
            _count = 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Init();
            if (array == null) CollectionThrowHelper.ThrowArgumentNull(nameof(array));
            if ((uint)arrayIndex > (uint)array.Length) CollectionThrowHelper.ThrowArgumentOutOfRange(nameof(arrayIndex));
            if (arrayIndex + _count > array.Length) CollectionThrowHelper.ThrowArgumentException("Destination array too small.");
            if (_count == 0) return;
            array[arrayIndex] = _firstItem;
            if (_count > 1)
            {
                _additionalItems.AsSpan(0, _count - 1).CopyTo(array.AsSpan(arrayIndex + 1));
            }
        }


        public T[] ToArray()
        {
            Init();
            if (_count == 0) return Array.Empty<T>();
            T[] result = new T[_count];
            result[0] = _firstItem;
            if (_count > 1)
            {
                _additionalItems.AsSpan(0, _count - 1).CopyTo(result.AsSpan(1));
            }
            return result;
        }


        public void Reverse()
        {
            Reverse(0, _count);
        }

        public void Reverse(int index, int count)
        {
            Init();
            if (index < 0 || count < 0 || index + count > _count) CollectionThrowHelper.ThrowArgumentOutOfRange(nameof(index));
            int i = index;
            int j = index + count - 1;
            while (i < j)
            {
                T tmp = GetItem(i);
                SetItem(i, GetItem(j));
                SetItem(j, tmp);
                i++;
                j--;
            }
        }

        public void Sort()
        {
            Sort(0, _count, null);
        }

        public void Sort(IComparer<T>? cmp)
        {
            Sort(0, _count, cmp);
        }

        public void Sort(int index, int count, IComparer<T>? cmp)
        {
            Init();
            if (index < 0 || count < 0 || index + count > _count) CollectionThrowHelper.ThrowArgumentOutOfRange(nameof(index));
            if (count <= 1) return;
            T[] tmp = new T[count];
            for (int i = 0; i < count; i++)
            {
                tmp[i] = GetItem(index + i);
            }

            Array.Sort(tmp, 0, count, cmp);
            for (int i = 0; i < count; i++)
            {
                SetItem(index + i, tmp[i]);
            }
        }

        public Enumerator GetEnumerator()
        {
            Init();
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            Init();
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Init();
            return new Enumerator(this);
        }

        public void OnBeforeSerialize()
        {
            Init();
            _s = _count == 0 ? Array.Empty<T>() : ToArray();
        }

        public void OnAfterDeserialize()
        {
            _initialized = true;
            _firstItem = default!;
            _additionalItems = null;
            _count = 0;
            if (_s == null || _s.Length == 0) return;
            EnsureCapacity(_s.Length);
            for (int i = 0; i < _s.Length; i++)
            {
                Add(_s[i]);
            }
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly InlinedList<T> _list;
            private int _index;
            private T _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(InlinedList<T> list)
            {
                _list = list;
                _index = 0;
                _current = default!;
            }

            public T Current => _current;

            object IEnumerator.Current => _current!;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if ((uint)_index < (uint)_list._count)
                {
                    _current = _list.GetItem(_index);
                    _index++;
                    return true;
                }

                _current = default!;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                _index = 0;
                _current = default!;
            }

            public void Dispose()
            {
            }
        }
    }
}