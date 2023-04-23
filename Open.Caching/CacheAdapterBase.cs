namespace Open.Caching;

/// <summary>
/// The base class for cache adapters.
/// </summary>
public abstract class CacheAdapterBase<TKey, TCache>
	: ICacheAdapterAndPolicyProvider<TKey, ExpirationPolicy>
	where TKey : notnull
{
	/// <summary>
	/// Helper method for determining if a type is nullable.
	/// </summary>
	protected static bool IsNullableType<T>()
		=> CacheAdapterExtensions.IsNullableType<T>();

	/// <summary>
	/// Returns a <see cref="InvalidCastException"/> for the provided object.
	/// </summary>
	protected static InvalidCastException UnexpectedTypeException<T>(object? o)
		=> CacheAdapterExtensions.UnexpectedTypeException<T>(o);

	/// <summary>
	/// The underlying cache.
	/// </summary>
	public TCache Cache { get; }

	/// <summary>
	/// Base constructor for cache adapters that accesses the cache directly.
	/// </summary>
	protected CacheAdapterBase(TCache cache)
		=> Cache = cache ?? throw new ArgumentNullException(nameof(cache));

	/// <summary>
	/// Base constructor for cache adapters that access a parent cache adapter.
	/// </summary>
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

	/// <summary>
	/// A cache adapter that defaults all expiration to the provided policy.
	/// </summary>
	protected class CacheExpirationPolicy : CacheAdapterBase<TKey, TCache>
	{
		/// <summary>
		/// The expiration policy.
		/// </summary>
		public ExpirationPolicy Expiration { get; }

		/// <summary>
		/// The parent cache adapter.
		/// </summary>
		protected readonly CacheAdapterBase<TKey, TCache> Parent;

		/// <summary>
		/// Constructs a new instance of <see cref="CacheExpirationPolicy"/>.
		/// </summary>
		public CacheExpirationPolicy(
			CacheAdapterBase<TKey, TCache> adapter,
			ExpirationPolicy policy)
			: base(adapter)
		{
			Parent = adapter;
			Expiration = policy;
		}

		/// <inheritdoc />
		public override void Set<TValue>(TKey key, TValue item)
			=> Parent.Set(key, item, Expiration);

		/// <inheritdoc />
		public override bool TryGetValue<TValue>(TKey key, out TValue item, bool throwIfUnexpectedType = false)
			=> Parent.TryGetValue(key, out item, throwIfUnexpectedType);

		/// <inheritdoc />
		public override void Set<TValue>(TKey key, TValue item, ExpirationPolicy policy)
			=> Parent.Set(key, item, policy);

		/// <inheritdoc />
		public override void Remove(TKey key)
			=> Parent.Remove(key);

		/// <inheritdoc />
		public override ICacheAdapter<TKey> Policy(ExpirationPolicy policy)
			=> policy == default ? this : new CacheExpirationPolicy(Parent, policy);
	}
}
