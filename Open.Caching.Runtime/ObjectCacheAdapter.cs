using System.Runtime.Caching;

namespace Open.Caching.Memory;

/// <summary>
/// <see cref="ObjectCache"/> adapter with <see cref="CacheItemFactory{string}"/> functionality for simplifying cache item access.
/// Use <see cref="ObjectCacheAdapter.ExpirationPolicyProvider"/> to generate adapters with expiration behaviors.
/// </summary>s
public class ObjectCacheAdapter : CacheItemFactoryBase<string>, ICacheAdapter<string>
{
	public ObjectCache Cache { get; }

	protected override ICacheAdapter<string> CacheAdapter { get; }

	public ObjectCacheAdapter(ObjectCache cache)
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

	class ObjectCacheExpirationPolicy : ObjectCacheAdapter
	{
		public ExpirationPolicy Expiration;

		public ObjectCacheExpirationPolicy(
			ObjectCache cache,
			ExpirationPolicy policy)
			: base(cache)
		{
			Expiration = policy;
		}

		public override void Set<TValue>(string key, TValue item)
		{
			if (Expiration.Sliding == TimeSpan.Zero && Expiration.Absolute != TimeSpan.Zero)
			{
				Cache.Set(key, item, Expiration.AbsoluteRelativeToNow);
				return;
			}

			var policy = new CacheItemPolicy();
			if (Expiration.Absolute != TimeSpan.Zero)
				policy.AbsoluteExpiration = Expiration.AbsoluteRelativeToNow;
			if (Expiration.Sliding != TimeSpan.Zero)
				policy.SlidingExpiration = Expiration.Sliding;
			Cache.Set(key, item, policy);
		}
	}

	public class ExpirationPolicyProvider
		: ObjectCacheAdapter, ICachePolicyProvider<string, ExpirationPolicy>
	{
		public ExpirationPolicyProvider(ObjectCache cache) : base(cache) { }

		public ICacheAdapter<string> Policy(ExpirationPolicy policy)
			=> policy == default ? this : new ObjectCacheExpirationPolicy(Cache, policy);
	}
}
