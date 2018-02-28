using System;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace Open.Caching.Runtime
{
	public static class ObjectCacheExtensions
	{
		public static ObjectCachePolicy Slide(this ObjectCache target, TimeSpan after, CacheItemPriority priority = CacheItemPriority.Default)
			=> new ObjectCachePolicy(target, ExpirationMode.Sliding, after, priority);

		public static ObjectCachePolicy Expire(this ObjectCache target, TimeSpan after, CacheItemPriority priority = CacheItemPriority.Default)
			=> new ObjectCachePolicy(target, ExpirationMode.Absolute, after, priority);

		public static ObjectCachePolicy Slide(this ObjectCache target, uint seconds, CacheItemPriority priority = CacheItemPriority.Default)
			=> Slide(target, TimeSpan.FromSeconds(seconds), priority);

		public static ObjectCachePolicy Expire(this ObjectCache target, uint seconds, CacheItemPriority priority = CacheItemPriority.Default)
			=> Expire(target, TimeSpan.FromSeconds(seconds), priority);
	}
}
