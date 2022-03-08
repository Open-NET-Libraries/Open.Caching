using Microsoft.Extensions.Caching.Memory;

namespace Open.Caching.Tests;

public class ObjectCacheAdapterTests : CacheAdapterTestsBase
{
	public ObjectCacheAdapterTests()
		: base(ObjectCacheAdapter.Default)
	{ }
}
