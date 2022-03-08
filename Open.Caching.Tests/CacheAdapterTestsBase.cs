﻿using FluentAssertions;
using Xunit;

namespace Open.Caching.Tests;

public abstract class CacheAdapterTestsBase
{
	private readonly ICacheAdapterAndPolicyProvider<string, ExpirationPolicy> Cache;

	protected CacheAdapterTestsBase(
		ICacheAdapterAndPolicyProvider<string, ExpirationPolicy> cache)
	{
		Cache = cache ?? throw new ArgumentNullException(nameof(cache));
	}

	const string Key = "hello";

	[Fact]
	public async Task AwaitTest()
	{
		var item = Cache.CreateAsyncLazyItem(Key,
			async () =>
			{
				await Task.Delay(100);
				return 1;
			});
		item.Clear();

		(await item).Should().Be(1);
		(await item.Value).Should().Be(1);
	}

	[Fact]
	public void ExpireAbsoluteTest()
	{
		var policy = Cache.ExpireAfter(TimeSpan.FromSeconds(1));
		var item = policy.CreateItem(Key, 0);
		Cache.Set(Key, "nope");
		Assert.Throws<InvalidCastException>(() => item.Value);
		Cache.TryGetValue<int>(Key, out _).Should().BeFalse();
		item.Clear();
		item.Value.Should().Be(0);
		item.Value = 10;
		item.Value.Should().Be(10);
		Thread.Sleep(2000);
		item.Value.Should().Be(0);
	}

	[Fact]
	public void ExpireSlideTest()
	{
		var policy = Cache.Slide(TimeSpan.FromSeconds(2));
		var item = policy.CreateItem(Key, 0);
		item.Clear();
		item.Value = 10;

		for (var i = 0; i < 10; i++)
		{
			Thread.Sleep(300);
			item.Value.Should().Be(10, "i:{0}", i);
		}
		Thread.Sleep(4000);
		item.Value.Should().Be(0);
	}
}
