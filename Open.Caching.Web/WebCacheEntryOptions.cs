using System.Web.Caching;

namespace Open.Caching.Web;

public class WebCacheEntryOptions
{
	public CacheDependency Dependency;
	public CacheItemRemovedCallback CacheItemRemovedCallback;
}
