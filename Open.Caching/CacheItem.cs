using System.Runtime.CompilerServices;

namespace Open.Caching;

/// <summary>
/// An interface for creating a simple container for a cache item.
/// </summary>
public interface ICacheItem<TValue>
{
	/// <summary>
	/// The value of the cache item.
	/// </summary>
	TValue Value { get; }
}

/// <summary>
/// An interface for creating a simple container for an asynchronous cache item.
/// </summary>
public interface IAsyncCacheItem<TValue>
{
	/// <summary>
	/// The <see cref="Task{TResult}"/> of the asynchronous cache item.
	/// </summary>
	Task<TValue> Value { get; }
}

/// <summary>
/// A base class for creating a simple container for a cache item.
/// </summary>
public abstract class CacheItemBase<TKey, TValue>
	where TKey : notnull
{
	/// <summary>
	/// The key that identifies this item in the cache.
	/// </summary>
	public TKey Key { get; }

	/// <summary>
	/// The <see cref="ICacheAdapter{TKey}"/> that this item is associated with.
	/// </summary>
	protected internal ICacheAdapter<TKey> Cache { get; }

	/// <summary>
	/// Constructs a new <see cref="CacheItemBase{TKey, TValue}"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException">If the <typeparamref name="TKey"/> is <see langword="null"/>.</exception>
	protected CacheItemBase(ICacheAdapter<TKey> cache, TKey key)
	{
		Cache = cache;
		Key = key ?? throw new ArgumentNullException(nameof(key));
	}

	/// <summary>
	/// Returns <see langword="true"/> if the item exists in the cache.
	/// </summary>
	public bool Exists => Cache.TryGetValue(Key, out TValue _);

	/// <summary>
	/// Removes this item from the cache.
	/// </summary>
	public void Clear() => Cache.Remove(Key);
}

/// <summary>
/// A simple container for a cache item.
/// </summary>
public class CacheItem<TKey, TValue>
	: CacheItemBase<TKey, TValue>, ICacheItem<TValue>
	where TKey : notnull
{
	readonly TValue _defaultValue;

	/// <summary>
	/// Constructs a new <see cref="CacheItem{TKey, TValue}"/>.
	/// </summary>
	public CacheItem(ICacheAdapter<TKey> cache, TKey key, TValue defaultValue = default!)
		: base(cache, key)
	{
		_defaultValue = defaultValue;
	}

	/// <summary>
	/// Gets or sets the value of the cache item.
	/// </summary>
	public TValue Value
	{
		get => Cache.GetOrDefault(Key, _defaultValue);
		set => Cache.Set(Key, value);
	}

	/// <summary>
	/// Implicitly converts the <see cref="CacheItem{TKey, TValue}"/> to its value.
	/// </summary>
	/// <remarks>
	/// Makes it easy to use the <see cref="CacheItem{TKey, TValue}"/> as a value.
	/// </remarks>
	public static implicit operator TValue(CacheItem<TKey, TValue> item) => item.Value;
}

/// <summary>
/// A simple container for a lazy cache item.
/// </summary>
public class LazyCacheItem<TKey, TValue>
	: CacheItemBase<TKey, TValue>, ICacheItem<TValue>
	where TKey : notnull
{
	readonly Func<TValue> _factory;

	/// <summary>
	/// Constructs a new <see cref="LazyCacheItem{TKey, TValue}"/>.
	/// </summary>
	public LazyCacheItem(
		ICacheAdapter<TKey> cache,
		TKey key,
		Func<TKey, TValue> factory)
		: base(cache, key)
	{
		if (factory is null) throw new ArgumentNullException(nameof(factory));
		_factory = () => factory(key);
	}

	/// <summary>
	/// Constructs a new <see cref="LazyCacheItem{TKey, TValue}"/>.
	/// </summary>
	public LazyCacheItem(
		ICacheAdapter<TKey> cache,
		TKey key,
		Func<TValue> factory)
		: base(cache, key)
	{
		_factory = factory ?? throw new ArgumentNullException(nameof(factory));
	}

	/// <inheritdoc />
	public TValue Value => Cache.GetOrCreateLazy(Key, _factory);

	/// <summary>
	/// Implicitly converts the <see cref="LazyCacheItem{TKey, TValue}"/> to its value.
	/// </summary>
	/// <remarks>
	/// Makes it easy to use the <see cref="LazyCacheItem{TKey, TValue}"/> as a value.
	/// </remarks>
	public static implicit operator TValue(LazyCacheItem<TKey, TValue> item) => item.Value;
}

/// <summary>
/// A simple container for an asynchronous cache item.
/// </summary>
public class AsyncLazyCacheItem<TKey, TValue>
	: CacheItemBase<TKey, TValue>, ICacheItem<Task<TValue>>, IAsyncCacheItem<TValue>
	where TKey : notnull
{
	readonly Func<Task<TValue>> _factory;

	/// <summary>
	/// Constructs a new <see cref="AsyncLazyCacheItem{TKey, TValue}"/>.
	/// </summary>
	public AsyncLazyCacheItem(
		ICacheAdapter<TKey> cache,
		TKey key,
		Func<TKey, Task<TValue>> factory)
		: base(cache, key)
	{
		if (factory is null) throw new ArgumentNullException(nameof(factory));
		_factory = () => factory(key);
	}

	/// <summary>
	/// Constructs a new <see cref="AsyncLazyCacheItem{TKey, TValue}"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException">If the <paramref name="factory"/> is <see langword="null"/>.</exception>
	public AsyncLazyCacheItem(
		ICacheAdapter<TKey> cache,
		TKey key,
		Func<Task<TValue>> factory)
		: base(cache, key)
	{
		_factory = factory ?? throw new ArgumentNullException(nameof(factory));
	}

	/// <inheritdoc/>
	public Task<TValue> Value => Cache.GetOrCreateLazyAsync(Key, _factory);

	/// <summary>
	/// Implicitly converts the <see cref="AsyncLazyCacheItem{TKey, TValue}"/> to its value.
	/// </summary>
	/// <remarks>
	/// Makes it easy to use the <see cref="AsyncLazyCacheItem{TKey, TValue}"/> as a value.
	/// </remarks>
	public static implicit operator Task<TValue>(AsyncLazyCacheItem<TKey, TValue> item) => item.Value;
}

/// <summary>
/// Extension methods for <see cref="ICacheItem{TValue}"/>.
/// </summary>
public static class AsyncCacheItemExtensions
{
	/// <summary>
	/// The awaiter method for <see cref="IAsyncCacheItem{TValue}"/>.
	/// </summary>
	public static TaskAwaiter<TValue> GetAwaiter<TValue>(
		this IAsyncCacheItem<TValue> item)
		=> item.Value.GetAwaiter();
}
