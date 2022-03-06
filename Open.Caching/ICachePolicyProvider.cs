namespace Open.Caching;

public interface ICachePolicyProvider<TKey, TPolicy>
{
	ICacheAdapter<TKey> Policy(TPolicy policy);
}

public interface ICachePriorityProvider<TKey, TPriority>
{
	ICacheAdapter<TKey> Policy(TPriority priority);
}

public static class CachePolicyExtensions
{
	public static ICacheAdapter<TKey> Slide<TKey>(
		this ICachePolicyProvider<TKey, ExpirationPolicy> provider,
		TimeSpan value)
		=> provider.Policy(Expire.Sliding(value));

	public static ICacheAdapter<TKey> ExpireAfter<TKey>(
		this ICachePolicyProvider<TKey, ExpirationPolicy> provider,
		TimeSpan value)
		=> provider.Policy(Expire.Absolute(value));
}
