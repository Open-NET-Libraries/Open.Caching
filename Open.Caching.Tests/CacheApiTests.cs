using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Open.Caching.Tests;
public class CacheApiTests
{
	readonly IMemoryCache Cache
		= new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public void MemoryCacheVerify()
    {
		Cache.Get
		var key = new object();
		using (var entry = Cache
			.CreateEntry(key)
			.SetValue("hello"))
		{
			Assert.Equal("hello", entry.Value);
		}

		Assert.True(Cache.TryGetValue(key, out _));
		var value = Cache.Get(key);
		Assert.Equal("hello", value);
		Cache.Remove(key);
		value = Cache.Get(key);
		Assert.Null(value);

		Cache.Set(key, "hi there");
		value = Cache.Get(key);
		Assert.Equal("hi there", value);
	}

	[Fact]
	public void MemoryCacheVerifyFaulted()
	{
		var key = new object();
		try
		{
			using var entry = Cache
				.CreateEntry(key)
				.SetValue("hello");
			Assert.Equal("hello", entry.Value);
			throw new System.Exception();
		}
		catch
		{
			Assert.False(Cache.TryGetValue(key, out _));
		}
	}
}