using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace Open.Caching;

public interface ICacheAdapter<TKey>
{
	/// <summary>
	/// Gets the item associated with this key if present.
	/// </summary>
	/// <param name="key">A <typeparamref name="TKey"/> identifying the requested item.</param>
	/// <param name="item">The located item or default for <typeparamref name="TValue"/>.</param>
	/// <param name="throwIfUnexpectedType"></param>
	/// <returns>True if the key was found; otherwise false.</returns>
	/// <exception cref="InvalidCastException">
	/// If <paramref name="throwIfUnexpectedType"/> is true
	/// and the value does not resolve to the expected type.
	/// </exception>
	bool TryGetValue<TValue>(
		TKey key,
		out TValue item,
		bool throwIfUnexpectedType = false);

	/// <summary>
	/// Adds or overwrites an item in the cache.
	/// </summary>
	/// <param name="key">A <typeparamref name="TKey"/> identifying the item.</param>
	/// <param name="item">The item to insert.</param>
	void Set<TValue>(TKey key, TValue item);

	/// <summary>
	/// Removes an item from the cache.
	/// </summary>
	/// <param name="key"></param>
	void Remove(TKey key);
}

public interface ICacheAdapter : ICacheAdapter<object> { }

public interface ICacheAdapter<TKey, TEntry> : ICacheAdapter<TKey>
	where TEntry : ICacheEntry<TKey>
{
	/// <summary>
	/// Create or overwrite an entry in the cache.
	/// </summary>
	/// <param name="key">A <typeparamref name="TKey"/> identifying the entry.</param>
	/// <returns>A disposable <typeparamref name="TEntry"/> for configuring the entry before insertion.</returns>
	TEntry CreateEntry(TKey key);
}

public static class CacheAdapterExtensions
{
	public static TValue GetOrDefault<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		TValue defaultValue = default!)
		=> cache.TryGetValue(key, out TValue value, true) ? value : defaultValue;

	public static TValue GetOrCreate<TKey, TValue, TEntry>(
		this ICacheAdapter<TKey, TEntry> cache,
		TKey key,
		Func<TEntry, TValue> factory)
		where TEntry : ICacheEntry<TKey>
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (factory is null) throw new ArgumentNullException(nameof(factory));

		if (cache.TryGetValue(key, out TValue value, true))
			return value;

		using (var entry = cache.CreateEntry(key))
		{
			value = factory(entry);
			entry.Value = value;
		}
		return value;
	}

	private const string CannotProcessNullFactory = "Cannot process a null factory.";
	private const string CannotProcessNullLazy = "Cannot process a null Lazy.";

	/// <remarks>
	/// If <paramref name="throwIfUnexpectedType"/> is true
	/// and the value found does not match either <typeparamref name="TValue"/>
	/// or a Lazy of <typeparamref name="TValue"/>
	/// then an <see cref="InvalidCastException"/> will be thrown.
	/// </remarks>
	/// <exception cref="InvalidCastException">
	/// If <paramref name="throwIfUnexpectedType"/> is true
	/// and the value does not resolve to the expected type.
	/// </exception>
	/// <inheritdoc cref="IMemoryCache.TryGetValue(object, out object)"/>
	public static bool TryGetLazy<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		out Lazy<TValue> value,
		bool throwIfUnexpectedType = false)
	{
		if (cache.TryGetValue(key, out object o))
		{
			switch (o)
			{
				case null:
					value = Lazy.Default<TValue>();
					return true;

				case Lazy<TValue> lazy:
					value = lazy;
					return true;

				case TValue item:
					value = Lazy.Create(() => item);
					return true;
			}
		}

		if (throwIfUnexpectedType)
			throw new InvalidCastException($"Expected a Lazy<{typeof(TValue)}> but actual type found was {o.GetType()}");

		value = default!;
		return false;
	}

	private static TValue GetOrCreateLazyCore<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key, Func<TKey, Lazy<TValue>> valueFactory)
	{
		if (cache.TryGetValue(key, out object o))
		{
			return o switch
			{
				null => default!,
				Lazy<TValue> lz => lz.Value,
				TValue item => item,
				_ => throw new InvalidCastException($"Expected a Lazy<{typeof(TValue)}> but actual type found was {o.GetType()}")
			};
		}

		var lazy = valueFactory(key);
		if (lazy is null) throw new InvalidOperationException(CannotProcessNullLazy);

		try
		{
			return lazy.Value;
		}
		catch
		{
			cache.Remove(key); // Fail safe.
			throw;
		}

	}

	/// <summary>
	/// Attempts to retrieve the item associated with the provide key.
	/// If it is not present, it inserts a Lazy of <typeparamref name="TValue"/>
	/// from the <paramref name="valueFactory"/>.
	/// If the result of the Lazy causes an exception to be thrown, the item is evicted from the cache.
	/// </summary>
	/// <remarks>
	/// The benefit of this method is that regardless if cache insertion is optimisitc,
	/// the time it takes to create a Lazy is potentially miniscule in comparison to how long it takes
	/// to complete the <paramref name="valueFactory"/> therefore reducing any contention or wasted cycles.
	/// But it is important to understand that it is still possible (although much less likely) to execute the <paramref name="valueFactory"/> more than once before the value is returned.
	/// </remarks>
	/// <exception cref="InvalidCastException">
	/// If a value is found but the type
	/// does not match either <typeparamref name="TValue"/>
	/// or a its container type.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// If <paramref name="valueFactory"/> returns null.
	/// </exception>
	public static TValue GetOrCreateLazy<TKey, TValue, TEntry>(
		this ICacheAdapter<TKey, TEntry> cache,
		TKey key,
		Func<TEntry, Lazy<TValue>> valueFactory)
		where TEntry : ICacheEntry<TKey>
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyCore(cache, key, k =>
		{
			Lazy<TValue> lazy;
			using (var cacheEntry = cache.CreateEntry(k))
			{
				lazy = valueFactory(cacheEntry);
				if (lazy is null) throw new InvalidOperationException(CannotProcessNullLazy);
				cacheEntry.Value = lazy;
			}
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazy{TKey, TValue, TEntry}(ICacheAdapter{TKey, TEntry}, TKey, Func{TEntry, Lazy{TValue}})"/>
	public static TValue GetOrCreateLazy<TKey, TValue, TEntry>(
		this ICacheAdapter<TKey, TEntry> cache,
		TKey key,
		Func<TEntry, Func<TValue>> valueFactory)
		where TEntry : ICacheEntry<TKey>
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyCore(cache, key, k =>
		{
			Lazy<TValue> lazy;
			using (var cacheEntry = cache.CreateEntry(k))
			{
				var factory = valueFactory(cacheEntry);
				if (factory is null) throw new InvalidOperationException(CannotProcessNullFactory);
				lazy = Lazy.Create(factory);
				cacheEntry.Value = lazy;
			}
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazy{TKey, TValue, TEntry}(ICacheAdapter{TKey, TEntry}, TKey, Func{TEntry, Lazy{TValue}})"/>
	public static TValue GetOrCreateLazy<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		Func<TKey, Func<TValue>> valueFactory)
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyCore(cache, key, k =>
		{
			var factory = valueFactory(k);
			if (factory is null) throw new InvalidOperationException(CannotProcessNullFactory);
			var lazy = Lazy.Create(factory);
			cache.Set(k, lazy);
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazy{TKey, TValue, TEntry}(ICacheAdapter{TKey, TEntry}, TKey, Func{TEntry, Lazy{TValue}})"/>
	public static TValue GetOrCreateLazy<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		Func<TKey, TValue> valueFactory)
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyCore(cache, key, k =>
		{
			var lazy = Lazy.Create(() => valueFactory(k));
			cache.Set(k, lazy);
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazy{TKey, TValue, TEntry}(ICacheAdapter{TKey, TEntry}, TKey, Func{TEntry, Lazy{TValue}})"/>
	public static TValue GetOrCreateLazy<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		Func<TValue> valueFactory)
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyCore(cache, key, k =>
		{
			var lazy = Lazy.Create(valueFactory);
			cache.Set(k, lazy);
			return lazy;
		});
	}

	private static Task<TValue> GetOrCreateLazyAsyncCore<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key, Func<TKey, Lazy<Task<TValue>>> valueFactory)
	{
		if (cache.TryGetValue(key, out object o))
		{
			return o switch
			{
				null => Task.FromResult(default(TValue)!),
				Lazy<Task<TValue>> lz => lz.Value,
				Task<TValue> task => task,
				Lazy<TValue> lz => Task.FromResult(lz.Value),
				TValue item => Task.FromResult(item),
				_ => throw new InvalidCastException($"Expected a Lazy<{typeof(TValue)}> but actual type found was {o.GetType()}")
			};
		}

		var lazy = valueFactory(key);
		if (lazy is null) throw new InvalidOperationException(CannotProcessNullLazy);

		try
		{
			var task = lazy.Value;
			return task.ContinueWith(t =>
			{
				if (t.IsFaulted || t.IsCanceled)
					cache.Remove(key); // Fail safe.
				return t;
			}).Unwrap();
		}
		catch
		{
			cache.Remove(key); // Fail safe.
			throw;
		}
	}

	/// <summary>
	/// Attempts to retrieve the task associated with the provide key.
	/// If it is not present it inserts a Lazy of a Task of <typeparamref name="TValue"/>
	/// from the <paramref name="valueFactory"/>.
	/// If the result of the Lazy or the Task causes an exception to be thrown, the item is evicted from the cache.
	/// </summary>
	/// <inheritdoc cref="GetOrCreateLazy{TKey, TValue, TEntry}(ICacheAdapter{TKey, TEntry}, TKey, Func{TEntry, Lazy{TValue}})"/>
	public static Task<TValue> GetOrCreateLazyAsync<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		Func<TKey, Func<Task<TValue>>> valueFactory)
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyAsyncCore(cache, key, key =>
		{
			var factory = valueFactory(key);
			if (factory is null) throw new InvalidOperationException(CannotProcessNullFactory);
			var lazy = Lazy.Create(factory);
			cache.Set(key, lazy);
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazyAsync{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, Func{Task{TValue}}})"/>
	public static Task<TValue> GetOrCreateLazyAsync<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		Func<TKey, Task<TValue>> valueFactory)
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyAsyncCore(cache, key, key =>
		{
			var lazy = Lazy.Create(()=>valueFactory(key));
			cache.Set(key, lazy);
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazyAsync{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, Func{Task{TValue}}})"/>
	public static Task<TValue> GetOrCreateLazyAsync<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		Func<Task<TValue>> valueFactory)
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyAsyncCore(cache, key, key =>
		{
			var lazy = Lazy.Create(valueFactory);
			cache.Set(key, lazy);
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazyAsync{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, Func{Task{TValue}}})"/>
	public static Task<TValue> GetOrCreateLazyAsync<TKey, TValue, TEntry>(
		this ICacheAdapter<TKey, TEntry> cache,
		TKey key,
		Func<TEntry, Func<Task<TValue>>> valueFactory)
		where TEntry : ICacheEntry<TKey>
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyAsyncCore(cache, key, k =>
		{
			Lazy<Task<TValue>> lazy;
			using (var cacheEntry = cache.CreateEntry(k))
			{
				var factory = valueFactory(cacheEntry);
				if (factory is null) throw new InvalidOperationException(CannotProcessNullFactory);
				lazy = Lazy.Create(factory);
				cacheEntry.Value = lazy;
			}
			return lazy;
		});
	}
}