using Microsoft.Extensions.Caching.Memory;

namespace Open.Caching;

/// <summary>
/// <see cref="IMemoryCache"/> adapter with functionality for simplifying cache item access.
/// </summary>
public class MemoryCacheAdapter<TKey>
	: CacheAdapterBase<TKey, IMemoryCache>
{
	public MemoryCacheAdapter(IMemoryCache cache) : base(cache)	{ }

	public override void Remove(TKey key)
		=> Cache.Remove(key);

	public override bool TryGetValue<TValue>(TKey key, out TValue item, bool throwIfUnexpectedType = false)
	{
		if (Cache.TryGetValue(key, out object o))
		{
			switch (o)
			{
				case null:
					item = default!;
					return true;

				case TValue v:
					item = v;
					return true;
			}

			if (throwIfUnexpectedType)
				throw new InvalidCastException($"Expected {typeof(TValue)} but actual type found was {o.GetType()}");
		}

		item = default!;
		return false;
	}

	public override void Set<TValue>(TKey key, TValue item)
		=> Cache.Set(key, item);

	public override void Set<TValue>(TKey key, TValue item, ExpirationPolicy expiration)
	{
		using var cacheItem = Cache.CreateEntry(key);
		if (expiration.Absolute != TimeSpan.Zero && expiration.Absolute != TimeSpan.MaxValue)
			cacheItem.AbsoluteExpirationRelativeToNow = expiration.Absolute;
		if (expiration.Sliding != TimeSpan.Zero)
			cacheItem.SlidingExpiration = expiration.Sliding;
		cacheItem.Value = item;
	}
}

public class MemoryCacheAdapter : MemoryCacheAdapter<object>
{
	public MemoryCacheAdapter(IMemoryCache cache) : base(cache) { }
}