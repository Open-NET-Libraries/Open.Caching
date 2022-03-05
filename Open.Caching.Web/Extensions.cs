using System;
using System.Web.Caching;

namespace Open.Caching.Web;

public static class Extensions
{
	public static WebCacheHelper Slide(this Cache cache, TimeSpan after, CacheItemPriority priority = CacheItemPriority.Default)
		=> new(cache, ExpirationMode.Sliding, after, priority);

	public static WebCacheHelper Expire(this Cache cache, TimeSpan after, CacheItemPriority priority = CacheItemPriority.Default)
		=> new(cache, ExpirationMode.Absolute, after, priority);

	public static WebCacheHelper Slide(this Cache cache, uint seconds, CacheItemPriority priority = CacheItemPriority.Default)
		=> Slide(cache, TimeSpan.FromSeconds(seconds), priority);

	public static WebCacheHelper Expire(this Cache cache, uint seconds, CacheItemPriority priority = CacheItemPriority.Default)
		=> Expire(cache, TimeSpan.FromSeconds(seconds), priority);
}
