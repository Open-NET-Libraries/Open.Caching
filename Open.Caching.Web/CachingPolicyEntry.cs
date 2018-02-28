using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;

namespace Open.Caching.Web
{
    public struct CachingPolicyEntry<T>
    {
        public string Key { get; private set; }
        public readonly CachingPolicy Policy;

        public CachingPolicyEntry(CachingPolicy policy, string key)
        {
            Policy = policy;
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public bool HasValue => HttpRuntime.Cache?[Key] != null;

        public T Value
        {
            get
            {
                var v = HttpRuntime.Cache?[Key];
                if (v is T typed) return typed;
                if (v == null) return default(T);
                throw new Exception($"Unexpected type: {v.GetType()} instead of expected: {typeof(T)}");
            }
            set => Policy.Insert(Key, value);
        }

        public void Insert(object value, CacheDependency dependencies = null, CacheItemRemovedCallback callback = null)
            => Policy.Insert(Key, value, dependencies, callback);
        public void Insert(object value, CacheItemRemovedCallback callback)
            => Insert(value, null, callback);

        public object Add(object value, CacheDependency dependencies = null, CacheItemRemovedCallback callback = null)
            => Policy.Add(Key, value, dependencies, callback);
        public object Add(object value, CacheItemRemovedCallback callback)
            => Add(value, null, callback);


        public T GetOrAdd(Func<T> factory) => Policy.GetOrAdd(Key, factory);
        public Task<T> GetOrAddAsync(Func<T> factory) => Policy.GetOrAddAsync(Key, factory);
    }
}
