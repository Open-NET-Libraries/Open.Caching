using System;
using System.Runtime.Caching;

namespace Open.Caching.Runtime;

public static class Extensions
{
	public static ObjectCacheHelper Slide(this ObjectCache target, TimeSpan after, CacheItemPriority priority = CacheItemPriority.Default)
		=> new(target, ExpirationMode.Sliding, after, priority);

	public static ObjectCacheHelper Expire(this ObjectCache target, TimeSpan after, CacheItemPriority priority = CacheItemPriority.Default)
		=> new(target, ExpirationMode.Absolute, after, priority);

	public static ObjectCacheHelper Slide(this ObjectCache target, uint seconds, CacheItemPriority priority = CacheItemPriority.Default)
		=> Slide(target, TimeSpan.FromSeconds(seconds), priority);

	public static ObjectCacheHelper Expire(this ObjectCache target, uint seconds, CacheItemPriority priority = CacheItemPriority.Default)
		=> Expire(target, TimeSpan.FromSeconds(seconds), priority);
}
