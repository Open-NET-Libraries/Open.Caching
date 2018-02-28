using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;

namespace Open.Caching.Web
{
	public static class Extensions
	{
		public static CachingPolicy Slide(this Cache cache, TimeSpan after, CacheItemPriority priority = CacheItemPriority.Default)
			=> CachingPolicy.Slide(after, priority);

		public static CachingPolicy Slide(this Cache cache, uint seconds, CacheItemPriority priority = CacheItemPriority.Default)
			=> CachingPolicy.Slide(seconds, priority);

		public static CachingPolicy Expire(this Cache cache, TimeSpan after, CacheItemPriority priority = CacheItemPriority.Default)
			=> CachingPolicy.Expire(after, priority);

		public static CachingPolicy Expire(this Cache cache, uint seconds, CacheItemPriority priority = CacheItemPriority.Default)
			=> CachingPolicy.Expire(seconds, priority);

	}
}
