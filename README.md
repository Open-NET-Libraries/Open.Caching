# Open.Caching

[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://www.nuget.org/packages/Open.Caching/blob/master/LICENSE)
![100% code coverage](https://img.shields.io/badge/coverage-100%25-green)

Useful set of DI/IoC agnostic interfaces, utilities and extensions for simplifying cache usage.

## Implementation

With the following libraries, you can build other libraries that sever their dependency from any cache and allow you to inject whichever you want.

---

## Core Interfaces & Extensions

### Open.Caching

[https://www.nuget.org/packages/Open.Caching](https://www.nuget.org/packages/Open.Caching/)  
[![NuGet](https://img.shields.io/nuget/v/Open.Caching.svg)](https://www.nuget.org/packages/Open.Caching/) Core package for interfaces and base classes.

## Library/Vendor Specific Implementations

### Open.Caching.Memory

[https://www.nuget.org/packages/Open.Caching.Memory](https://www.nuget.org/packages/Open.Caching.Memory/)  
[![NuGet](https://img.shields.io/nuget/v/Open.Caching.Memory.svg)](https://www.nuget.org/packages/Open.Caching.Memory/) Contains `MemoryCacheAdapter` for use with any `IMemoryCache`.

### Open.Caching.Runtime

[https://www.nuget.org/packages/Open.Caching.Runtime](https://www.nuget.org/packages/Open.Caching.Runtime/)  
[![NuGet](https://img.shields.io/nuget/v/Open.Caching.Runtime.svg)](https://www.nuget.org/packages/Open.Caching.Runtime/) Contains `ObjectCacheAdapter` for use with any `System.Runtime.Caching.ObjectCache`.

### Open.Caching.Web

[https://www.nuget.org/packages/Open.Caching.Web](https://www.nuget.org/packages/Open.Caching.Web/)  
[![NuGet](https://img.shields.io/nuget/v/Open.Caching.Web.svg)](https://www.nuget.org/packages/Open.Caching.Web/) Contains `WebCacheAdapter` for use with any `System.Web.Caching.Cache`.  
Useful when attempting to transition code away from legacy ASP.NET.

---

## Notable Similarities &amp; Differences

The above adapters all accept strings as keys, but only `MemoryCacheAdapter` will accept any type of key as `IMemoryCache` uses `object`s as keys.  If your dependency injection configuration uses `ICacheAdapter<string>` as its cache interface then any of the implementations can be used.  So if you are transitioning from a legacy ASP.NET environment, switching to `MemoryCacheAdapter<string>` will make things easy.

Every cache implementation listed handles absolute and sliding expiration.

Because `IMemoryCache` allows for `null` values to be inserted, the other implementations use a placeholder `NullValue` to indicate null and retain parity for all implementations.

## Not Yet Supported

Some of the reasons for not supporting certain features should be obvious. The intention of these utilities is to cover the 95%+ use case.  
Setting expiration is very common, but setting priority is not so common.

* At this time, 'priority' is not supported as each cache has a slightly different implementation.
* Eviction call backs, cache item or file system watchers.

---

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

It does not offer a mechanism for a cache policy as that is provided by `CacheAdapterBase<TKey, TCache>`.

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

The intention of this and the following classes is to simplify access to a cached resource.  
Much like a `Lazy<T>`, or any other container class, you can affix, or pass around these classes without the consumer having to know what the key is.

```cs
public class MyClass {

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
        get => _value; // Implicit
        set => _value.Value = value;
    }
}
```

### `LazyCacheItem<TKey, TValue>`

The important idea here is to allow for the insertion of a `Lazy<T>` so that any subsequent requests to that resource either wait for it to complete, or receive the already resolved value.

The underlying `.GetOrCreateLazy<T>` extension properly evicts the `Lazy<T>` if the `Value` property throws an exception.

```cs
public class MyClass {

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

    public string Value => _value; // Implicit
}
```


### `AsyncLazyCacheItem<TKey, TValue>`

This class implements `IAsyncCacheItem<TValue>` and therefore is awaitable.

Similar to the above, the underlying `.GetOrCreateLazyAsync` method uses a `Lazy<Task<T>>>` to initialize the method and asynchronously produce a result.  Any exceptions thrown by the the `Task<T>` or its factory method will evict the entry from the cache.

```cs
public class MyClass {

    // Injected ICacheAdapter<string>.
    public MyClass(ICacheAdapter<string> cache)
    {
        // The key is defined in only one place.
        _value = cache
            .CreateAsyncLazyItem(
                key: "a cache key",
                async ()=>{
                /* long running async process. */
                });
    }

    public Task<string> Value => _value; // Implicit
}
```
