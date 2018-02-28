using System;
using System.Runtime.Caching;

namespace Open.Caching.Runtime
{
	public static class CacheItemPolicyExtensions
	{
		public static CacheItemPolicy Clone(this CacheItemPolicy source)
			=> new CacheItemPolicy
			{
				AbsoluteExpiration = source.AbsoluteExpiration,
				Priority = source.Priority,
				RemovedCallback = source.RemovedCallback,
				SlidingExpiration = source.SlidingExpiration,
				UpdateCallback = source.UpdateCallback
			};

		public static CacheItemPolicy Slide(this CacheItemPolicy source, TimeSpan after, CacheItemPriority? priority = null)
		{
			if (!Validation.IsValidExpiresSliding(after))
				throw new ArgumentOutOfRangeException(nameof(after));

			var clone = source.Clone();
			clone.AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration;
			clone.SlidingExpiration = after;
			if (priority.HasValue) clone.Priority = priority.Value;
			return clone;
		}

		public static CacheItemPolicy Slide(this CacheItemPolicy source, uint seconds, CacheItemPriority? priority = null)
			=> Slide(source, TimeSpan.FromSeconds(seconds), priority);

		public static CacheItemPolicy Expire(this CacheItemPolicy source, DateTimeOffset after, CacheItemPriority? priority = null)
		{
			var clone = source.Clone();
			clone.AbsoluteExpiration = after;
			clone.SlidingExpiration = ObjectCache.NoSlidingExpiration;
			if (priority.HasValue) clone.Priority = priority.Value;
			return clone;
		}

		public static CacheItemPolicy Expire(this CacheItemPolicy source, TimeSpan after, CacheItemPriority? priority = null)
		{
			if (!Validation.IsValidExpiresSliding(after))
				throw new ArgumentOutOfRangeException(nameof(after));

			return Expire(source, DateTimeOffset.Now.Add(after), priority);
		}

		public static CacheItemPolicy Expire(this CacheItemPolicy source, uint seconds, CacheItemPriority? priority = null)
			=> Expire(source, TimeSpan.FromSeconds(seconds), priority);

		public static CacheItemPolicy WithPriority(this CacheItemPolicy source, CacheItemPriority priority)
		{
			var clone = source.Clone();
			clone.Priority = priority;
			return clone;
		}

	}
}
