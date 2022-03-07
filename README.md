# Open.Caching

Useful set of DI/IoC agnostic interfaces, utilities and extensions for simplifying cache usage.

## Implementation

With the following libraries, you can build other libraries that sever their dependency from any cache and allow you to inject whichever you want.

### Core Interfaces & Extensions

[https://www.nuget.org/packages/Open.Cache](https://www.nuget.org/packages/Open.Cache/)  
[![NuGet](https://img.shields.io/nuget/v/Open.Cache.svg)](https://www.nuget.org/packages/Open.Cache/) Core package for interfaces and base classes.

[https://www.nuget.org/packages/Open.Cache.Memory](https://www.nuget.org/packages/Open.Cache.Memory/)  

### Library/Vendor Specific Implementations

[https://www.nuget.org/packages/Open.Cache.Memory](https://www.nuget.org/packages/Open.Cache.Memory/)  
[![NuGet](https://img.shields.io/nuget/v/Open.Cache.Memory.svg)](https://www.nuget.org/packages/Open.Cache.Memory/) Contains `MemoryCacheAdapter` for use with any `IMemoryCache`.

[https://www.nuget.org/packages/Open.Cache.Runtime](https://www.nuget.org/packages/Open.Cache.Runtime/)  
[![NuGet](https://img.shields.io/nuget/v/Open.Cache.Runtime.svg)](https://www.nuget.org/packages/Open.Cache.Runtime/) Contains `ObjectCacheAdapter` for use with any `System.Runtime.Caching.ObjectCache`.

[https://www.nuget.org/packages/Open.Cache.Web](https://www.nuget.org/packages/Open.Cache.Web/)  
[![NuGet](https://img.shields.io/nuget/v/Open.Cache.Web.svg)](https://www.nuget.org/packages/Open.Cache.Web/) Contains `WebCacheAdapter` for use with any `System.Web.Caching.Cache`.  
Useful when attempting to transition code away from legacy ASP.NET.

## Notable Similarities &amp; Differences

The above adapters all accept strings as keys, but only `MemoryCacheAdapter` will accept any type of key as `IMemoryCache` uses `object`s as keys.  If your dependency injection configuration uses `ICacheAdapter<string>` as its cache interface then any of the implementations can be used.  So if you are transitioning from a legacy ASP.NET environment, switching to `MemoryCacheAdapter<string>` will make things easy.

Every cache implementation listed handles absolute and sliding expiration.

## Not Yet Supported

Some of the reasons for not supporting certain features should be obvious. The intention of these utilities is to cover the 95%+ use case. Setting expiration is very common, but setting priority is not so common.

* At this time, 'priority' is not supported as each cache has a slightly different implementation.
* Eviction call backs, cache item or file system watchers.


## Interfaces, Classes, &amp; Structs

### `ICacheAdapter<TKey>` 

Modeled after `Microsoft.Extensions.Caching.Memory.IMemoryCache`, this interface facilitates cache access for all adapters and extensions.

```cs
namespace Open.Caching;

public interface ICacheAdapter<TKey>
{
	bool TryGetValue<TValue>(
		TKey key,
		out TValue item,
		bool throwIfUnexpectedType = false);

	void Set<TValue>(TKey key, TValue item);

	void Remove(TKey key);
}
```

It does not provide a mechanism for cache policy as that is provided by `CacheAdapterBase<TKey, TCache>` level.

### `ExpirationPolicy`

This read only struct combines both `.Absolute` and `.Sliding` expirations into `TimeSpan` values.

`.Absolute` is a `TimeSpan` as it is almost always the case that expiration happens relative from when the cache entry was inserted.

`DateTimeOffset AbsoluteRelativeToNow` is derived from the value of `.Absolute` and the `DateTimeOffset.Now`

### `ICachePolicyProvider<TKey, TPolicy>`

This interface allows for returning a specific `ICacheAdapter<TKey` that will default to that policy.

### `CacheAdapterBase<TKey, TCache>`

Every adapter derives from this base class and implements the `ICachePolicyProvider<TKey, ExpirationPolicy>` interface. Allowing for simple or policy driven cache access.

---

### `CacheItem<TKey, TValue>`

The intention of this and the following classes is to simplify access to a resource.  Much like a `Lazy<T>`, or any other container class, you can affix, or pass around these classes without the consumer having to know what the key is.


```cs
public MyClass {

    // Injected ICacheAdapter<string>.
    public MyClass(ICacheAdapter<string> cache)
    {
        // The key is defined in only one place.
        _value = cache
            .CreateItem(
                key: "a cache key",
                defaultValue: "[not set]");
    }

    readonly CacheItem<string, string> _value;
    public string Value {
        get => _value.Value;
        set => _value.Value = value;
    }
}
```

### `LazyCacheItem<TKey, TValue>`

The important idea here is to allow for the insertion of a `Lazy<T>` and any subsequent requests to that resource either wait for it to complete, or receive the already resolved value.

The underlying `.GetOrCreateLazy<T>` extension properly evicts the `Lazy<T>` if `.Value` throws an exception.

```cs
public MyClass {

    // Injected ICacheAdapter<string>.
    public MyClass(ICacheAdapter<string> cache)
    {
        // The key is defined in only one place.
        _value = cache
            .CreateLazyItem(
                key: "a cache key",
                ()=>{
                /* long running process. */
                });
    }

    public string Value => _value.Value;
}
```
