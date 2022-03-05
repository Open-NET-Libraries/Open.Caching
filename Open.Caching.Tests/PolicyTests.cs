using Xunit;

namespace Open.Caching.Tests;

public static class PolicyTests
{
	static ICacheItemFactory _factory = default!;
	static ICacheExpirationProvider<string> _factory = default!;
	[Fact]
	public static void ApiTest1()
	{
		var item = _factory.CreateItem("key", 0);
		_ = item.Exists;
	}
}
