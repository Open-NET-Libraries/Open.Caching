using System;
using System.Threading.Tasks;

namespace Open.Caching
{
	public class CacheEntry<T> : ICacheEntry<T>
	{
		public string Key { get; }
		public T DefaultValue { get; }

		public readonly ICacheHelper Policy;

		public CacheEntry(ICacheHelper policy, string key, T defaultValue = default)
		{
			Policy = policy;
			Key = key ?? throw new ArgumentNullException(nameof(key));
			DefaultValue = defaultValue;
		}

		public bool HasValue => Policy[Key] != null;

		public T Value
		{
			get => GetOrDefault(DefaultValue);
			set => Policy[Key] = value!;
		}

		public Task<T> GetOrAddAsync(Func<T> factory, bool runSynchronously)
			=> Policy.GetOrAddAsync(Key, factory, runSynchronously);

		public Task<T> GetOrAddAsync(Func<Task<T>> factory)
			=> Policy.GetOrAddAsync(Key, factory);

		public T GetOrAdd(Func<T> factory)
			=> Policy.GetOrAdd(Key, factory);

		public T GetOrDefault(T defaultValue)
		{
			var v = Policy[Key];
			if (v is T typed) return typed;
			if (v is null) return defaultValue;
			throw new Exception($"Unexpected type: {v.GetType()} instead of expected: {typeof(T)}");
		}
	}
}
