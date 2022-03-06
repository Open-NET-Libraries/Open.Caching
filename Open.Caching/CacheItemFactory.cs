namespace Open.Caching;

public abstract class CacheItemFactoryBase<TKey>
{
	protected abstract ICacheAdapter<TKey> CacheAdapter { get; }

	public CacheItem<TKey, TValue> CreateItem<TValue>(TKey key, TValue defaultValue = default!)
		=> new(CacheAdapter, key, defaultValue);

	public LazyCacheItem<TKey, TValue> CreateLazyItem<TValue>(
		TKey key,
		Func<TValue> factory)
		=> new(CacheAdapter, key, factory);

	public LazyCacheItem<TKey, TValue> CreateLazyItem<TValue>(
		TKey key,
		Func<TKey, TValue> factory)
		=> new(CacheAdapter, key, factory);

	public AsyncLazyCacheItem<TKey, TValue> CreateAsyncLazyItem<TValue>(
		TKey key,
		Func<Task<TValue>> factory)
		=> new(CacheAdapter, key, factory);

	public AsyncLazyCacheItem<TKey, TValue> CreateAsyncLazyItem<TValue>(
		TKey key,
		Func<TKey, Task<TValue>> factory)
		=> new(CacheAdapter, key, factory);
}

public class CacheItemFactory<TKey> : CacheItemFactoryBase<TKey>
{
	protected override ICacheAdapter<TKey> CacheAdapter { get; }

	public CacheItemFactory(ICacheAdapter<TKey> cache)
		=> CacheAdapter = cache ?? throw new ArgumentNullException(nameof(cache));
}