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

	public static readonly object NullValue = new();

	/// <inheritdoc />
	public override void Set<TValue>(string key, TValue item)
		=> Cache[key] = item ?? NullValue;

	/// <inheritdoc />
	public override void Set<TValue>(string key, TValue item, ExpirationPolicy expiration)
		=> Cache.Insert(key, item ?? NullValue, null,
			expiration.HasSliding ? expiration.AbsoluteRelativeToNow.DateTime : Cache.NoAbsoluteExpiration,
			expiration.Sliding);

	/// <inheritdoc />
	public override bool TryGetValue<TValue>(string key, out TValue item, bool throwIfUnexpectedType = false)
	{
		var o = Cache.Get(key);
		if (o is null) goto notFound;
		if (o == NullValue)
		{
			if (IsNullableType<TValue>())
			{
				item = default!;
				return true;
			}
		}
		else if (o is TValue v)
		{
			item = v;
			return true;
		}

		if (throwIfUnexpectedType)
			throw UnexpectedTypeException<TValue>(o);

		notFound:
		item = default!;
		return false;
	}
}
