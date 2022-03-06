using Microsoft.Extensions.Caching.Memory;

namespace Open.Caching.Memory;
public class MemoryCacheAdapter<TKey> : ICacheAdapter<TKey>
{
	public IMemoryCache Cache { get; }

	public MemoryCacheAdapter(IMemoryCache cache)
	{
		Cache = cache ?? throw new ArgumentNullException(nameof(cache));
	}

	public void Remove(TKey key)
		=> Cache.Remove(key);

	public virtual void Set<TValue>(TKey key, TValue item)
		=> Cache.Set(key, item);

	public bool TryGetValue<TValue>(TKey key, out TValue item, bool throwIfUnexpectedType = false)
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
		}

		if (throwIfUnexpectedType)
			throw new InvalidCastException($"Expected {typeof(TValue)} but actual type found was {o.GetType()}");

		item = default!;
		return false;
	}

	class MemoryCacheExpirationPolicy : MemoryCacheAdapter<TKey>
	{
		public ExpirationPolicy Expiration;

		public MemoryCacheExpirationPolicy(
			IMemoryCache cache,
			ExpirationPolicy policy)
			: base(cache)
		{
			Expiration = policy;
		}

		public override void Set<TValue>(TKey key, TValue item)
		{
			using var cacheItem = Cache.CreateEntry(key);
			if (Expiration.Absolute != TimeSpan.Zero)
				cacheItem.AbsoluteExpirationRelativeToNow = Expiration.Absolute;
			if (Expiration.Sliding != TimeSpan.Zero)
				cacheItem.SlidingExpiration = Expiration.Sliding;
			cacheItem.Value = item;
		}
	}

	public class ExpirationPolicyProvider
		: MemoryCacheAdapter<TKey>, ICachePolicyProvider<TKey, ExpirationPolicy>
	{
		public ExpirationPolicyProvider(IMemoryCache cache) : base(cache) { }

		public ICacheAdapter<TKey> Policy(ExpirationPolicy policy)
			=> policy == default ? this : new MemoryCacheExpirationPolicy(Cache, policy);
	}
}
