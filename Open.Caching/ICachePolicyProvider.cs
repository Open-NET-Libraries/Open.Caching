namespace Open.Caching;

/// <summary>
/// Interface for defining a provider that returns a cache adapter with a specific policy.
/// </summary>
/// <typeparam name="TKey">The non-null key type.</typeparam>
/// <typeparam name="TPolicy">The type representing the policy.</typeparam>
public interface ICachePolicyProvider<TKey, TPolicy>
	where TKey : notnull
{
	/// <summary>
	/// Returns a cache adapter with the specified policy.
	/// </summary>
	ICacheAdapter<TKey> Policy(TPolicy policy);
}

/// <summary>
/// Interface for defining a provider that is an <see cref="ICacheAdapter{TKey}"/>
/// but can also return a cache adapter with a specific policy.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TPolicy"></typeparam>
public interface ICacheAdapterAndPolicyProvider<TKey, TPolicy>
	: ICacheAdapter<TKey>, ICachePolicyProvider<TKey, TPolicy>
	where TKey : notnull
{
}

/// <summary>
/// Extensions for <see cref="ICachePolicyProvider{TKey, TPolicy}"/>.
/// </summary>
public static class CachePolicyExtensions
{
	/// <summary>
	/// Returns a cache adapter with the specified policy and sliding expiration.
	/// </summary>
	public static ICacheAdapter<TKey> Slide<TKey>(
		this ICachePolicyProvider<TKey, ExpirationPolicy> provider,
		TimeSpan value)
		where TKey : notnull
		=> provider.Policy(Expire.Sliding(value));

	/// <summary>
	/// Returns a cache adapter with the specified policy and absolute expiration.
	/// </summary>
	public static ICacheAdapter<TKey> ExpireAfter<TKey>(
		this ICachePolicyProvider<TKey, ExpirationPolicy> provider,
		TimeSpan value)
		where TKey : notnull
		=> provider.Policy(Expire.Absolute(value));
}
