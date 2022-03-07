using System.Web.Caching;

namespace Open.Caching;

/// <summary>
/// <see cref="System.Web.Caching"/>.<see cref="Cache"/> adapter with functionality for simplifying cache item access.
/// </summary>s
public class WebCacheAdapter
	: CacheAdapterBase<string, Cache>
{
	public WebCacheAdapter(Cache cache) : base(cache) { }

	/// <inheritdoc />
	public override void Remove(string key)
		=> Cache.Remove(key);

	/// <inheritdoc />
	public override void Set<TValue>(string key, TValue item)
		=> Cache[key] = item;

	/// <inheritdoc />
	public override void Set<TValue>(string key, TValue item, ExpirationPolicy expiration)
		=> Cache.Insert(key, item, null,
			expiration.HasSliding ? expiration.AbsoluteRelativeToNow.DateTime : Cache.NoAbsoluteExpiration,
			expiration.Sliding);

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
