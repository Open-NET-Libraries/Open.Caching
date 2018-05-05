﻿using System;
using System.Runtime.Caching;

namespace Open.Caching.Runtime
{
	public class ObjectCacheHelper : CachePolicyBase<CacheItemPriority, CacheItemPolicy, ObjectCacheHelper>
	{
		public readonly ObjectCache Cache;

		public DateTimeOffset ExpiresAbsolute => Mode == ExpirationMode.Absolute ? DateTimeOffset.Now.Add(ExpiresAfter) : ObjectCache.InfiniteAbsoluteExpiration;
		public TimeSpan ExpiresSliding => Mode == ExpirationMode.Sliding ? ExpiresAfter : ObjectCache.NoSlidingExpiration;

		internal ObjectCacheHelper(ObjectCache target,
			ExpirationMode mode,
			TimeSpan expiresAfter,
			CacheItemPriority priority)
			:base(mode, expiresAfter, priority)
		{
			Cache = target ?? throw new ArgumentNullException(nameof(target));
		}

		public CacheItemPolicy GetItemPolicy(
				CacheEntryUpdateCallback updateCallback = null,
				CacheEntryRemovedCallback removedCallback = null)
			=> new CacheItemPolicy
			{
				AbsoluteExpiration = ExpiresAbsolute,
				SlidingExpiration = ExpiresSliding,
				Priority = Priority,
				UpdateCallback = updateCallback,
				RemovedCallback = removedCallback
			};

		public CacheItemPolicy GetItemPolicy(
				CacheEntryRemovedCallback removedCallback,
				CacheEntryUpdateCallback updateCallback = null)
			=> GetItemPolicy(updateCallback, removedCallback);

		public override ObjectCacheHelper Create(ExpirationMode mode, TimeSpan expiresAfter, CacheItemPriority priority)
			=> new ObjectCacheHelper(Cache, mode, expiresAfter, priority);

		public override ObjectCacheHelper Create(ExpirationMode mode, TimeSpan expiresAfter)
			=> new ObjectCacheHelper(Cache, mode, expiresAfter, this.Priority);

		public override object Get(string key) => Cache.Get(key);

		public override object Add<T>(string key, T value, CacheItemPolicy options = null)
			=> Cache.AddOrGetExisting(key, value, options ?? GetItemPolicy());

		public override void Insert<T>(string key, T value, CacheItemPolicy options = null)
			=> Cache.Set(key, value, options ?? GetItemPolicy());

		public object Add<T>(string key, T value, CacheEntryUpdateCallback updateCallback, CacheEntryRemovedCallback removedCallback = null)
			=> Add(key, value, GetItemPolicy(updateCallback, removedCallback));
		public object Add<T>(string key, T value, CacheEntryRemovedCallback removedCallback, CacheEntryUpdateCallback updateCallback = null)
			=> Add(key, value, updateCallback, removedCallback);

		public void Insert<T>(string key, T value, CacheEntryUpdateCallback updateCallback, CacheEntryRemovedCallback removedCallback = null)
			=> Insert(key, value, GetItemPolicy(updateCallback, removedCallback));
		public void Insert<T>(string key, T value, CacheEntryRemovedCallback removedCallback, CacheEntryUpdateCallback updateCallback = null)
			=> Insert(key, value, updateCallback, removedCallback);
	}

}