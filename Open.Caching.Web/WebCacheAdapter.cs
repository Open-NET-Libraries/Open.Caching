using System.Web;
using System.Web.Caching;

namespace Open.Caching;

/// <summary>
/// <see cref="System.Web.Caching"/>.<see cref="Cache"/> adapter with functionality for simplifying cache item access.
/// </summary>s
public sealed class WebCacheAdapter
	: CacheAdapterBase<string, Cache>
{
	/// <summary>
	/// Constructs a new instance of <see cref="WebCacheAdapter"/>.
	/// </summary>
	public WebCacheAdapter(Cache cache) : base(cache) { }

	/// <summary>
	/// Constructs a new instance of <see cref="WebCacheAdapter"/>.
	/// </summary>
	public WebCacheAdapter() : base(HttpRuntime.Cache) { }

	static WebCacheAdapter? _instance;

	/// <summary>
	/// Returns the shared default instance of <see cref="WebCacheAdapter"/>.
	/// </summary>
	public static WebCacheAdapter Default 
		=> LazyInitializer.EnsureInitialized(ref _instance)!;


	/// <inheritdoc />
	public override void Remove(string key)
		=> Cache.Remove(key);

	/// <summary>
	/// The null value instance.
	/// </summary>
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
		if (ReferenceEquals(o, NullValue))
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
