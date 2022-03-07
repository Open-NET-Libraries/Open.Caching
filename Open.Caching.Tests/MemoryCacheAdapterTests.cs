using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Open.Caching.Tests;

public class MemoryCacheAdapterTests
{
	private readonly MemoryCacheAdapter Cache
		= new(new MemoryCache(new MemoryCacheOptions()));

	[Fact]
	public async Task AwaitTest()
	{
		var item = Cache.CreateAsyncLazyItem("hello",
			async () =>
			{
				await Task.Delay(100);
				return 1;
			});

		Assert.Equal(1, await item);

		Assert.Equal(1, await item.Value);
	}

	[Fact]
	public void ExpireTest()
	{
		var policy = Cache.ExpireAfter(TimeSpan.FromSeconds(1));

		var item = policy.CreateItem("hello", 0);
		item.Value = 10;
		Assert.Equal(10, item.Value);
		Thread.Sleep(2000);
		Assert.Equal(0, item.Value);
	}
}
