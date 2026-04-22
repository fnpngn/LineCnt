using System.Collections.Concurrent;

namespace LineCnt
{
    public class ConcurrentPool<T>
    {
        private static readonly ConcurrentPool<T> _pool;
        public static ConcurrentPool<T> Get() => _pool;

        public int Capacity => _pool.Capacity;
        public virtual int InitialCapacity => 10;

        private ConcurrentBag<T> _values;

        static ConcurrentPool()
        {
            _pool = new ConcurrentPool<T>();
        }

        public ConcurrentPool()
        {
            _values = new ConcurrentBag<T>(Enumerable.Range(0, InitialCapacity).Select(_ => Create()));
        }


        public T Take()
        {
            T result = default;
            if (!_values.TryTake(out result))
            {
                result = Create();
            }

            return result;

        }

        public virtual T Create()
        {
            return default(T);
        }

        public virtual void Return(T value)
        {
            _values.Add(value);
        }
    }
}
