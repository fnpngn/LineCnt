// I guess I reused some SortedList code
// This class contains code adapted from the .NET Runtime, licensed under the MIT License.
// See the Ext/DOTNET_LICENSE.txt file for full license text.


namespace LineCnt
{
    // SortedList clone that keeps the references local and allows span iteration
    public struct SortedStore<TKey, TValue>
    {
        private const int MIN_COUNT = 5;
        private const int GROW = 2;

        public int Count => _count;
        public ReadOnlySpan<TValue> Values => _values.AsSpan(0, _count);
        public ReadOnlySpan<TKey> Keys => _keys.AsSpan(0, _count);


        private IComparer<TKey> _comparer;
        private TValue[] _values;
        private TKey[] _keys;
        private int _count;


        public SortedStore(int reserve, IComparer<TKey> comparer)
        {
            _comparer = comparer;
            _count = 0;

            if (reserve <= 0)
            {
                _keys = Array.Empty<TKey>();
                _values = Array.Empty<TValue>();
            }
            else
            {
                _keys = new TKey[reserve];
                _values = new TValue[reserve];
            }
        }

        public SortedStore(int reserve = MIN_COUNT) : this(reserve, Comparer<TKey>.Default) { }

        public void Add(in TKey key, in TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);

            int index = Array.BinarySearch<TKey>(_keys, 0, _count, key, _comparer);

            if (index >= 0)
            {
                throw new ArgumentException("Duplicate key" + key, nameof(key));
            }

            Insert(~index, key, value);
        }

        public void Insert(int index, in TKey key, in TValue value)
        {
            if (_count == _keys.Length)
            {
                EnsureCapacity(_count + 1);
            }

            if (index < _count)
            {
#if DEBUG
                Console.WriteLine($"File store insert had to copy elements: {_count - index}");
#endif
                Array.Copy(_keys, index, _keys, index + 1, _count - index);
                Array.Copy(_values, index, _values, index + 1, _count - index);
            }

            _values[index] = value;
            _keys[index] = key;
            _count++;
        }

        public void EnsureCapacity(int capacity)
        {
            capacity = capacity < MIN_COUNT ? MIN_COUNT : capacity;

            int newCapacity = GROW * _keys.Length;

            if (newCapacity < capacity)
            {
                newCapacity = capacity;
            }

            Array.Resize(ref _values, newCapacity);
            Array.Resize(ref _keys, newCapacity);
        }


        [Obsolete("Warning: leaks memory if used with reference types")]
        public void ClearSoft()
        {
            _count = 0;

#if DEBUG
            bool isKey = System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<TKey>();
            bool isValue = System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<TValue>();
            if (isKey || isValue)
            {
                Console.WriteLine($"Possible memory leak on {nameof(SortedStore<TKey, TValue>)}.{nameof(ClearSoft)}: structure contains reference types");
            }
#endif
        }
        public void Clear()
        {
            _count = 0;
            Array.Clear(_values);
            Array.Clear(_keys);
        }
    }
}
