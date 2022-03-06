namespace Open.Caching.Memory;

public abstract class CacheAdapterBase<TKey, TCache>
	: ICacheAdapter<TKey>, ICachePolicyProvider<TKey, ExpirationPolicy>
{
	private readonly TCache Cache;

	protected CacheAdapterBase(TCache cache)
		=> Cache = cache ?? throw new ArgumentNullException(nameof(cache));

	protected CacheAdapterBase(CacheAdapterBase<TKey, TCache> parent)
		: this((parent ?? throw new ArgumentNullException(nameof(parent))).Cache)
	{
	}

	public abstract bool TryGetValue<TValue>(TKey key, out TValue item, bool throwIfUnexpectedType = false);

	public abstract void Set<TValue>(TKey key, TValue item);

	public abstract void Set<TValue>(TKey key, TValue item, ExpirationPolicy policy);

	public abstract void Remove(TKey key);

	public virtual ICacheAdapter<TKey> Policy(ExpirationPolicy policy)
		=> policy == default ? this : new CacheExpirationPolicy(this, policy);

	protected class CacheExpirationPolicy : CacheAdapterBase<TKey, TCache>
	{
		public ExpirationPolicy Expiration;

		protected readonly CacheAdapterBase<TKey, TCache> Parent;

		public CacheExpirationPolicy(
			CacheAdapterBase<TKey, TCache> adapter,
			ExpirationPolicy policy)
			: base(adapter)
		{
			Parent = adapter;
			Expiration = policy;
		}

		public override void Set<TValue>(TKey key, TValue item)
			=> Parent.Set(key, item, Expiration);
		public override bool TryGetValue<TValue>(TKey key, out TValue item, bool throwIfUnexpectedType = false)
			=> Parent.TryGetValue(key, out item, throwIfUnexpectedType);
		public override void Set<TValue>(TKey key, TValue item, ExpirationPolicy policy)
			=> Parent.Set(key, item, policy);
		public override void Remove(TKey key)
			=> Parent.Remove(key);
		public override ICacheAdapter<TKey> Policy(ExpirationPolicy policy)
			=> policy == default ? this : new CacheExpirationPolicy(Parent, policy);
	}
}
