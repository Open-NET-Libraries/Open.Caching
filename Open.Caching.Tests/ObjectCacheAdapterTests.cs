using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Open.Caching.Tests;

public class ObjectCacheAdapterTests : CacheAdapterTestsBase
{
	public ObjectCacheAdapterTests()
		: base(ObjectCacheAdapter.Default)
	{ }
}
