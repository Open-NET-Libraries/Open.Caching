using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Open.Caching.Tests;

public class MemoryCacheAdapterTests : CacheAdapterTestsBase
{
	public MemoryCacheAdapterTests()
		: base(new MemoryCacheAdapter<string>(new MemoryCache(new MemoryCacheOptions())))
	{ }
}
