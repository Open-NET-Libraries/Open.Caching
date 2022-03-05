using System;

namespace Open.Caching;

/// <summary>
/// Modeling after Microsoft.Extensions.Caching, this provides the minimum required for adapting to any cache type.
/// </summary>
public interface ICacheEntry<out TKey> : IDisposable
{
	TKey Key { get; }
	object? Value { get; set; }
}