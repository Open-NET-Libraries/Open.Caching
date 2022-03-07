using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using Xunit;

namespace Open.Caching.Tests;

public class MemoryCacheAdapterTests
{
	private readonly MemoryCacheAdapter Cache = new(new MemoryCache(new MemoryCacheOptions()));

	[Fact]
	public async Task AwaitTest()
	{
		var value = await CacheAdapterExtensions.CreateAsyncLazyItem(Cache, "hello",
			async () =>
			{
				await Task.Delay(100);
				return 1;
			});

		Assert.Equal(1, value);
	}
}
