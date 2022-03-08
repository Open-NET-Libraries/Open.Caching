namespace Open.Caching;

public abstract class CacheAdapterBase<TKey, TCache>
	: ICacheAdapterAndPolicyProvider<TKey, ExpirationPolicy>
{
	public TCache Cache { get; }

	protected CacheAdapterBase(TCache cache)
		=> Cache = cache ?? throw new ArgumentNullException(nameof(cache));

	protected CacheAdapterBase(CacheAdapterBase<TKey, TCache> parent)
		: this((parent ?? throw new ArgumentNullException(nameof(parent))).Cache) { }

	/// <inheritdoc />
	public abstract bool TryGetValue<TValue>(TKey key, out TValue item, bool throwIfUnexpectedType = false);

	/// <inheritdoc />
	public abstract void Set<TValue>(TKey key, TValue item);

	/// <inheritdoc cref="Set{TValue}(TKey, TValue)" />
	public abstract void Set<TValue>(TKey key, TValue item, ExpirationPolicy policy);

	/// <inheritdoc />
	public abstract void Remove(TKey key);

	/// <summary>
	/// Creates a cache adapter that defaults all expiration to the provided policy.
	/// </summary>
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
