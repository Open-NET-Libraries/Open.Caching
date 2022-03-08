using System.Runtime.Caching;

namespace Open.Caching;

/// <summary>
/// <see cref="ObjectCache"/> adapter with functionality for simplifying cache item access.
/// </summary>s
public class ObjectCacheAdapter
	: CacheAdapterBase<string, ObjectCache>
{
	public ObjectCacheAdapter(ObjectCache cache) : base(cache) { }

	public ObjectCacheAdapter() : this(MemoryCache.Default) { }

	private static ObjectCacheAdapter? _default;
	public static ObjectCacheAdapter Default
		=> LazyInitializer.EnsureInitialized(ref _default)!;

	/// <inheritdoc />
	public override void Remove(string key)
		=> Cache.Remove(key);

	/// <inheritdoc />
	public override void Set<TValue>(string key, TValue item)
		=> Cache[key] = item;

	/// <inheritdoc />
	public override void Set<TValue>(string key, TValue item, ExpirationPolicy expiration)
	{
		if (expiration.Sliding == TimeSpan.Zero && expiration.Absolute != TimeSpan.Zero)
		{
			Cache.Set(key, item, expiration.AbsoluteRelativeToNow);
			return;
		}

		Cache.Set(key, item, new CacheItemPolicy
		{
			AbsoluteExpiration = expiration.HasAbsolute
				? expiration.AbsoluteRelativeToNow
				: ObjectCache.InfiniteAbsoluteExpiration,
			SlidingExpiration = expiration.HasSliding
				? expiration.Sliding
				: ObjectCache.NoSlidingExpiration
		});
	}

	/// <inheritdoc />
	public override bool TryGetValue<TValue>(string key, out TValue item, bool throwIfUnexpectedType = false)
	{
		var o = Cache.Get(key);
		switch (o)
		{
			case null:
				item = default!;
				return false;

			case TValue v:
				item = v;
				return true;
		}

		if (throwIfUnexpectedType)
			throw new InvalidCastException($"Expected {typeof(TValue)} but actual type found was {o.GetType()}");

		item = default!;
		return false;
	}
}
