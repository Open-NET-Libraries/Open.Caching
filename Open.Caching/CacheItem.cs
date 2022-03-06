using System.Runtime.CompilerServices;

namespace Open.Caching;

public abstract class CacheItemBase<TKey, TValue>
{
	public TKey Key { get; }

	protected internal ICacheAdapter<TKey> Cache { get; }

	protected CacheItemBase(ICacheAdapter<TKey> cache, TKey key)
	{
		Cache = cache;
		Key = key ?? throw new ArgumentNullException(nameof(key));
	}

	public bool Exists => Cache.TryGetValue(Key, out TValue _);

	public void Clear() => Cache.Remove(Key);
}

public class CacheItem<TKey, TValue> : CacheItemBase<TKey, TValue>
{
	readonly TValue _defaultValue;

	public CacheItem(ICacheAdapter<TKey> cache, TKey key, TValue defaultValue = default!)
		: base(cache, key)
	{
		_defaultValue = defaultValue;
	}

	public TValue Value
	{
		get => Cache.GetOrDefault(Key, _defaultValue);
		set => Cache.Set(Key, value);
	}

	public static implicit operator TValue(CacheItem<TKey, TValue> item) => item.Value;
}

public class LazyCacheItem<TKey, TValue> : CacheItemBase<TKey, TValue>
{
	readonly Func<TValue> _factory;

	public LazyCacheItem(
		ICacheAdapter<TKey> cache,
		TKey key,
		Func<TKey, TValue> factory)
		: base(cache, key)
	{
		if (factory is null) throw new ArgumentNullException(nameof(factory));
		_factory = () => factory(key);
	}

	public LazyCacheItem(
		ICacheAdapter<TKey> cache,
		TKey key,
		Func<TValue> factory)
		: base(cache, key)
	{
		_factory = factory ?? throw new ArgumentNullException(nameof(factory));
	}

	public TValue Value => Cache.GetOrCreateLazy(Key, _factory);

	public static implicit operator TValue(LazyCacheItem<TKey, TValue> item) => item.Value;
}

public class AsyncLazyCacheItem<TKey, TValue> : CacheItemBase<TKey, TValue>
{
	readonly Func<Task<TValue>> _factory;

	public AsyncLazyCacheItem(
		ICacheAdapter<TKey> cache,
		TKey key,
		Func<TKey, Task<TValue>> factory)
		: base(cache, key)
	{
		if (factory is null) throw new ArgumentNullException(nameof(factory));
		_factory = () => factory(key);
	}

	public AsyncLazyCacheItem(
		ICacheAdapter<TKey> cache,
		TKey key,
		Func<Task<TValue>> factory)
		: base(cache, key)
	{
		_factory = factory ?? throw new ArgumentNullException(nameof(factory));
	}

	public Task<TValue> Value => Cache.GetOrCreateLazyAsync(Key, _factory);

	public static implicit operator Task<TValue>(AsyncLazyCacheItem<TKey, TValue> item) => item.Value;
}

public static class AsyncLazyCacheItemExtensions
{
	public static TaskAwaiter<TValue> GetAwaiter<TKey, TValue>(
		this AsyncLazyCacheItem<TKey, TValue> item)
		=> item.Value.GetAwaiter();
}
