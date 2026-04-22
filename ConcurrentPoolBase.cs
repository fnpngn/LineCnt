using System.Collections.Concurrent;

namespace LineCnt
{
    public class ConcurrentPool<T> : ConcurrentPoolBase<T, ConcurrentPool<T>>
        where T : new()
    {
        protected override T Create()
        {
            return new T();
        }
    }

    public class ConcurrentPoolBase<T, TSelf> where TSelf : ConcurrentPoolBase<T, TSelf>, new()
    {
        private static readonly TSelf _pool;
        public static TSelf Get() => _pool;

        public int Capacity => _pool.Capacity;
        public virtual int InitialCapacity => 10;

        private ConcurrentBag<T> _values;

        static ConcurrentPoolBase()
        {
            _pool = new TSelf();
        }

        public ConcurrentPoolBase()
        {
            Create();
            _values = new ConcurrentBag<T>(Enumerable.Range(0, InitialCapacity).Select(_ => this.Create()));
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

        protected virtual T Create()
        {
            Console.WriteLine("NEW DEFAULT");
            return default(T);
        }

        public virtual void Return(T value)
        {
            _values.Add(value);
        }
    }
}
