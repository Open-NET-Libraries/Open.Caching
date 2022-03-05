using System;
using System.Threading.Tasks;

namespace Open.Caching;

public class CacheItemFactory<TKey>
{
	readonly ICacheAdapter<TKey> Cache;
	public CacheItemFactory(ICacheAdapter<TKey> cache)
		=> Cache = cache ?? throw new ArgumentNullException(nameof(cache));

	public CacheItem<TKey, TValue> CreateItem<TValue>(TKey key, TValue defaultValue = default!)
		=> new(Cache, key, defaultValue);

	public LazyCacheItem<TKey, TValue> CreateLazyItem<TValue>(
		TKey key,
		Func<TValue> factory)
		=> new(Cache, key, factory);

	public LazyCacheItem<TKey, TValue> CreateLazyItem<TValue>(
		TKey key,
		Func<TKey, TValue> factory)
		=> new(Cache, key, factory);

	public AsyncLazyCacheItem<TKey, TValue> CreateAsyncLazyItem<TValue>(
		TKey key,
		Func<Task<TValue>> factory)
		=> new(Cache, key, factory);

	public AsyncLazyCacheItem<TKey, TValue> CreateAsyncLazyItem<TValue>(
		TKey key,
		Func<TKey, Task<TValue>> factory)
		=> new(Cache, key, factory);
}