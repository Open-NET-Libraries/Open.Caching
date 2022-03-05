using Open.Disposable;
using System;

namespace Open.Caching;

/// <summary>
/// Base class for configuring a cache entry before it enters the cache.
/// </summary>
public abstract class CacheEntryBase<TKey, TValue>
	: DisposableBase, ICacheEntry<TKey>
{
	protected CacheEntryBase(TKey key)
	{
		Key = key ?? throw new ArgumentNullException(nameof(key));
	}

	public virtual TKey Key { get; }

	public abstract object? Value { get; set; }

	protected override void OnDispose() => OnInsert();

	protected abstract void OnInsert();
}
