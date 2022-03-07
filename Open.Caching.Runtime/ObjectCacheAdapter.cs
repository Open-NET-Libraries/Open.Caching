using System.Runtime.Caching;

namespace Open.Caching;

/// <summary>
/// <see cref="ObjectCache"/> adapter with functionality for simplifying cache item access.
/// </summary>s
public class ObjectCacheAdapter
	: CacheAdapterBase<string, ObjectCache>
{
	public ObjectCacheAdapter(ObjectCache cache) : base(cache) { }

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

		var policy = new CacheItemPolicy();
		if (expiration.HasAbsolute)
			policy.AbsoluteExpiration = expiration.AbsoluteRelativeToNow;
		if (expiration.HasSliding)
			policy.SlidingExpiration = expiration.Sliding;
		Cache.Set(key, item, policy);
	}

	/// <inheritdoc />
	public override bool TryGetValue<TValue>(string key, out TValue item, bool throwIfUnexpectedType = false)
	{
		var o = Cache[key];
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
