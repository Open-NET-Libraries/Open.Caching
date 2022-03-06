using System.Web.Caching;

namespace Open.Caching.Web;

/// <summary>
/// <see cref="System.Web.Caching.Cache"/> adapter with <see cref="CacheItemFactory{string}"/> functionality for simplifying cache item access.
/// Use <see cref="WebCacheAdapter.ExpirationPolicyProvider"/> to generate adapters with expiration behaviors.
/// </summary>s
public class WebCacheAdapter : CacheItemFactoryBase<string>, ICacheAdapter<string>
{
	public Cache Cache { get; }

	protected override ICacheAdapter<string> CacheAdapter { get; }

	public WebCacheAdapter(Cache cache)
	{
		Cache = cache ?? throw new ArgumentNullException(nameof(cache));
		CacheAdapter = this;
	}

	public void Remove(string key)
		=> Cache.Remove(key);

	public virtual void Set<TValue>(string key, TValue item)
		=> Cache[key] = item;

	public bool TryGetValue<TValue>(string key, out TValue item, bool throwIfUnexpectedType = false)
	{
		var o = Cache[key];
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

		item = default!;
		return false;
	}

	class CacheExpirationPolicy : WebCacheAdapter
	{
		public ExpirationPolicy Expiration;

		public CacheExpirationPolicy(
			Cache cache,
			ExpirationPolicy policy)
			: base(cache)
		{
			Expiration = policy;
		}

		public override void Set<TValue>(string key, TValue item)
			=> Cache.Insert(key, item, null,
				Expiration.Absolute == TimeSpan.Zero ? Cache.NoAbsoluteExpiration : Expiration.AbsoluteRelativeToNow.DateTime,
				Expiration.Sliding);
	}

	public class ExpirationPolicyProvider
		: WebCacheAdapter, ICachePolicyProvider<string, ExpirationPolicy>
	{
		public ExpirationPolicyProvider(Cache cache) : base(cache) { }

		public ICacheAdapter<string> Policy(ExpirationPolicy policy)
			=> policy == default ? this : new CacheExpirationPolicy(Cache, policy);
	}
}
