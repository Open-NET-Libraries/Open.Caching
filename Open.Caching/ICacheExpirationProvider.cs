using System;

namespace Open.Caching;

public interface ICacheExpirationProvider<TKey>
{
	ICacheAdapter<TKey> Expire(ExpirationPolicy expiration);
}

public interface ICacheExpirationProvider : ICacheExpirationProvider<object>
{
	new ICacheAdapter Expire(ExpirationPolicy expiration);
}

public interface ICachePriorityProvider<TKey, TPriority>
{
	ICacheAdapter<TKey> Prioritize(TPriority priority);
}

public interface ICachePriorityProvider<TPriority> : ICachePriorityProvider<object, TPriority>
{
	new ICacheAdapter Prioritize(TPriority priority);
}

//public static class CachePolicyExtensions
//{
//	public static TPolicy Slide<TPolicy>(this TPolicy policy, TimeSpan value)
//		where TPolicy : ICachePolicy<TPolicy>
//		=> policy.Expire(Expire.Sliding(value));

//	public static TPolicy ExpireAfter<TPolicy>(this TPolicy policy, TimeSpan value)
//		where TPolicy : ICachePolicy<TPolicy>
//		=> policy.Expire(Expire.Absolute(value));
//}
