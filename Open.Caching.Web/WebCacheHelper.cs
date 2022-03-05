using System;
using System.Web.Caching;

namespace Open.Caching.Web;

public class WebCacheHelper : CachePolicyBase<CacheItemPriority, WebCacheEntryOptions, WebCacheHelper>
{
	public readonly Cache Cache;

	DateTime ExpiresAbsolute => Mode == ExpirationMode.Absolute ? DateTime.Now.Add(ExpiresAfter) : Cache.NoAbsoluteExpiration;
	TimeSpan ExpiresSliding => Mode == ExpirationMode.Sliding ? ExpiresAfter : Cache.NoSlidingExpiration;

	internal WebCacheHelper(Cache target,
		ExpirationMode mode,
		TimeSpan expiresAfter,
		CacheItemPriority priority)
		: base(mode, expiresAfter, priority)
	{
		Cache = target ?? throw new ArgumentNullException(nameof(target));
	}

	public override WebCacheHelper Create(ExpirationMode mode, TimeSpan expiresAfter, CacheItemPriority priority)
		=> new(Cache, mode, expiresAfter, priority);

	public override WebCacheHelper Create(ExpirationMode mode, TimeSpan expiresAfter)
		=> new(Cache, mode, expiresAfter, Priority);

	public override object Get(string key) => Cache.Get(key);

	public override object Add<T>(string key, T value, WebCacheEntryOptions options = null)
		=> Cache.Add(key, value, options?.Dependency, ExpiresAbsolute, ExpiresSliding, Priority, options?.CacheItemRemovedCallback);

	public override void Insert<T>(string key, T value, WebCacheEntryOptions options = null)
		=> Cache.Insert(key, value, options?.Dependency, ExpiresAbsolute, ExpiresSliding, Priority, options?.CacheItemRemovedCallback);

	public object Add<T>(string key, T value, CacheDependency dependency, CacheItemRemovedCallback callback = null)
		=> Add(key, value, new WebCacheEntryOptions { Dependency = dependency, CacheItemRemovedCallback = callback });
	public object Add<T>(string key, T value, CacheItemRemovedCallback callback, CacheDependency dependency = null)
		=> Add(key, value, dependency, callback);

	public void Insert<T>(string key, T value, CacheDependency dependency, CacheItemRemovedCallback callback = null)
		=> Insert(key, value, new WebCacheEntryOptions { Dependency = dependency, CacheItemRemovedCallback = callback });
	public void Insert<T>(string key, T value, CacheItemRemovedCallback callback, CacheDependency dependency = null)
		=> Insert(key, value, dependency, callback);
}
