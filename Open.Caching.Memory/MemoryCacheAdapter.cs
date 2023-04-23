using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;

namespace Open.Caching;

/// <summary>
/// <see cref="IMemoryCache"/> adapter with functionality for simplifying cache item access.
/// </summary>
public class MemoryCacheAdapter<TKey>
	: CacheAdapterBase<TKey, IMemoryCache>
	where TKey : notnull
{
	/// <summary>
	/// Constructs a new instance of <see cref="MemoryCacheAdapter{T}"/>.
	/// </summary>
	public MemoryCacheAdapter(IMemoryCache cache) : base(cache) { }

	/// <inheritdoc />
	public override void Remove(TKey key)
		=> Cache.Remove(key);

	/// <inheritdoc />
	public override bool TryGetValue<TValue>(TKey key, out TValue item, bool throwIfUnexpectedType = false)
	{
		if (!Cache.TryGetValue(key, out object? o))
			goto NotFound;

		switch (o)
		{
			case null when IsNullableType<TValue>():
				item = default!;
				return true;

			case TValue v:
				item = v;
				return true;
		}

		if (throwIfUnexpectedType)
			throw UnexpectedTypeException<TValue>(o);

		NotFound:
		item = default!;
		return false;
	}

	/// <inheritdoc />
	public override void Set<TValue>(TKey key, TValue item)
		=> Cache.Set(key, item);

	/// <inheritdoc />
	public override void Set<TValue>(TKey key, TValue item, ExpirationPolicy expiration)
	{
		using var cacheItem = Cache.CreateEntry(key);
		if (expiration.HasAbsolute)
			cacheItem.AbsoluteExpirationRelativeToNow = expiration.Absolute;
		if (expiration.HasSliding)
			cacheItem.SlidingExpiration = expiration.Sliding;
		cacheItem.Value = item;
	}
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public class MemoryCacheAdapter : MemoryCacheAdapter<object>
{
	/// <summary>
	/// Constructs a new instance of <see cref="MemoryCacheAdapter"/>.
	/// </summary>
	public MemoryCacheAdapter(IMemoryCache cache) : base(cache) { }
}