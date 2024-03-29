﻿using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Open.Caching.Extensions.Memory;

[SuppressMessage("Roslynator", "RCS1229:Use async/await when necessary.")]
public static class MemoryCacheExtensions
{
	private static bool IsNullableType<T>()
	{
		var type = typeof(T);
		return type.IsClass || Nullable.GetUnderlyingType(type) != null;
	}

	private static InvalidCastException UnexpectedTypeException<T>(object? o)
		=> new(o is null
		? $"Expected type {typeof(T)} but actual value was null."
		: $"Expected type {typeof(T)} but actual type found was {o.GetType()}.");

	/// <remarks>
	/// If <paramref name="throwIfUnexpectedType"/> is <see langword="true"/>
	/// and the value found does not match either <typeparamref name="TValue"/>
	/// or a <see cref="Lazy{T}"/> of <typeparamref name="TValue"/>
	/// then an <see cref="InvalidCastException"/> will be thrown.
	/// </remarks>
	/// <exception cref="InvalidCastException">
	/// If <paramref name="throwIfUnexpectedType"/> is <see langword="true"/>
	/// and the value does not resolve to the expected type.
	/// </exception>
	/// <inheritdoc cref="IMemoryCache.TryGetValue(object, out object)"/>
	public static bool TryGetValue<TValue>(
		this IMemoryCache cache,
		object key,
		out TValue value,
		bool throwIfUnexpectedType)
	{
		if (!cache.TryGetValue(key, out object? o))
			goto NotFound;

		switch (o)
		{
			case null when IsNullableType<TValue>():
				value = default!;
				return true;

			case TValue item:
				value = item;
				return true;
		}

		if (throwIfUnexpectedType)
			throw UnexpectedTypeException<TValue>(o);

		NotFound:
		value = default!;
		return false;
	}

	/// <summary>
	/// Gets the value from the cache otherwise returns the <paramref name="defaultValue"/> of <typeparamref name="TValue"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">If the type does not match the <typeparamref name="TValue"/>.</exception>
	public static TValue GetOrDefault<TValue>(
		this IMemoryCache cache,
		object key,
		TValue defaultValue = default!)
		=> cache.TryGetValue(key, out TValue value, true) ? value : defaultValue;

	private const string CannotProcessNullFactory = "Cannot process a null factory.";
	private const string CannotProcessNullLazy = "Cannot process a null Lazy.";

	/// <remarks>
	/// If <paramref name="throwIfUnexpectedType"/> is <see langword="true"/>
	/// and the value found does not match either <typeparamref name="TValue"/>
	/// or a <see cref="Lazy{T}"/> of <typeparamref name="TValue"/>
	/// then an <see cref="InvalidCastException"/> will be thrown.
	/// </remarks>
	/// <exception cref="InvalidCastException">
	/// If <paramref name="throwIfUnexpectedType"/> is <see langword="true"/>
	/// and the value does not resolve to the expected type.
	/// </exception>
	/// <inheritdoc cref="IMemoryCache.TryGetValue(object, out object)"/>
	public static bool TryGetLazy<TValue>(
		this IMemoryCache cache,
		object key,
		out Lazy<TValue> value,
		bool throwIfUnexpectedType = false)
	{
		if (!cache.TryGetValue(key, out object? o))
			goto NotFound;

		switch (o)
		{
			case null when IsNullableType<TValue>():
				value = Lazy.Default<TValue>();
				return true;

			case Lazy<TValue> lazy:
				value = lazy;
				return true;

			case TValue item:
				value = Lazy.FromValue(item);
				return true;
		}

		if (throwIfUnexpectedType)
			throw UnexpectedTypeException<Lazy<TValue>>(o);

		NotFound:
		value = default!;
		return false;
	}

	private static TValue GetOrCreateLazyCore<TValue>(
		this IMemoryCache cache,
		object key, Func<object, Lazy<TValue>> valueFactory)
	{
		if (!cache.TryGetValue(key, out object? o))
		{
			var lazy = valueFactory(key) ?? throw new InvalidOperationException(CannotProcessNullLazy);
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

		return o switch
		{
			null when IsNullableType<TValue>() => default!,
			Lazy<TValue> lz => lz.Value,
			TValue item => item,
			_ => throw UnexpectedTypeException<Lazy<TValue>>(o)
		};
	}

	/// <exception cref="InvalidOperationException">
	/// If <paramref name="valueFactory"/> returns <see langword="null"/>.
	/// </exception>
	/// <inheritdoc cref="GetOrCreateLazy{TValue}(IMemoryCache, object, Func{object, TValue})"/>
	public static TValue GetOrCreateLazy<TValue>(
		this IMemoryCache cache,
		object key,
		Func<ICacheEntry, Lazy<TValue>> valueFactory)
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

	/// <exception cref="InvalidOperationException">
	/// If <paramref name="valueFactory"/> returns <see langword="null"/>.
	/// </exception>
	/// <inheritdoc cref="GetOrCreateLazy{TValue}(IMemoryCache, object, Func{object, TValue})"/>
	public static TValue GetOrCreateLazy<TValue>(
		this IMemoryCache cache,
		object key,
		Func<ICacheEntry, Func<TValue>> valueFactory)
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
				var factory = valueFactory(cacheEntry) ?? throw new InvalidOperationException(CannotProcessNullFactory);
				lazy = Lazy.Create(factory);
				cacheEntry.Value = lazy;
			}
			return lazy;
		});
	}

	/// <exception cref="InvalidOperationException">
	/// If <paramref name="valueFactory"/> returns <see langword="null"/>.
	/// </exception>
	/// <inheritdoc cref="GetOrCreateLazy{TValue}(IMemoryCache, object, Func{object, TValue})"/>
	public static TValue GetOrCreateLazy<TValue>(
		this IMemoryCache cache,
		object key,
		Func<object, Func<TValue>> valueFactory)
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyCore(cache, key, k =>
		{
			var factory = valueFactory(k) ?? throw new InvalidOperationException(CannotProcessNullFactory);
			var lazy = Lazy.Create(factory);
			cache.Set(k, lazy);
			return lazy;
		});
	}

	/// <summary>
	/// Attempts to retrieve the item associated with the provided <paramref name="key"/>.
	/// If it is not present, it inserts a <see cref="Lazy{T}"/> of <typeparamref name="TValue"/>
	/// from the <paramref name="valueFactory"/>.
	/// If the result of the Lazy causes an exception to be thrown, the item is evicted from the cache.
	/// </summary>
	/// <remarks>
	/// The benefit of this method is that regardless if cache insertion is optimistic,
	/// the time it takes to create a Lazy is potentially minuscule compared to how long it takes
	/// to complete the <paramref name="valueFactory"/> therefore reducing any contention or wasted cycles.
	/// But it is important to understand that it is still possible (although much less likely)
	/// to execute the <paramref name="valueFactory"/> more than once before the value is returned.
	/// </remarks>
	/// <exception cref="InvalidCastException">
	/// If a value is found but the type
	/// does not match either <typeparamref name="TValue"/>
	/// or a its container type.
	/// </exception>
	public static TValue GetOrCreateLazy<TValue>(
		this IMemoryCache cache,
		object key,
		Func<object, TValue> valueFactory)
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

	/// <inheritdoc cref="GetOrCreateLazy{TValue}(IMemoryCache, object, Func{object, TValue})"/>
	public static TValue GetOrCreateLazy<TValue>(
		this IMemoryCache cache,
		object key,
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

	private static Task<TValue> GetOrCreateLazyAsyncCore<TValue>(
		this IMemoryCache cache,
		object key, Func<object, Lazy<Task<TValue>>> valueFactory)
	{
		if (!cache.TryGetValue(key, out object? o))
		{
			var lazy = valueFactory(key) ?? throw new InvalidOperationException(CannotProcessNullLazy);
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

		return o switch
		{
			null when IsNullableType<TValue>() => Task.FromResult(default(TValue)!),
			Lazy<Task<TValue>> lz => lz.Value,
			Task<TValue> task => task,
			Lazy<TValue> lz => Task.FromResult(lz.Value),
			TValue item => Task.FromResult(item),
			_ => throw UnexpectedTypeException<Lazy<Task<TValue>>>(o)
		};
	}

	/// <exception cref="InvalidOperationException">
	/// If <paramref name="valueFactory"/> returns <see langword="null"/>.
	/// </exception>
	/// <inheritdoc cref="GetOrCreateLazyAsync{TValue}(IMemoryCache, object, Func{object, Task{TValue}})"/>
	public static Task<TValue> GetOrCreateLazyAsync<TValue>(
		this IMemoryCache cache,
		object key,
		Func<ICacheEntry, Lazy<Task<TValue>>> valueFactory)
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
				lazy = valueFactory(cacheEntry);
				if (lazy is null) throw new InvalidOperationException(CannotProcessNullLazy);
				cacheEntry.Value = lazy;
			}
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazyAsync{TValue}(IMemoryCache, object, Func{ICacheEntry, Lazy{Task{TValue}}})"/>
	public static Task<TValue> GetOrCreateLazyAsync<TValue>(
		this IMemoryCache cache,
		object key,
		Func<ICacheEntry, Func<Task<TValue>>> valueFactory)
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
				var factory = valueFactory(cacheEntry) ?? throw new InvalidOperationException(CannotProcessNullFactory);
				lazy = Lazy.Create(factory);
				cacheEntry.Value = lazy;
			}
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazyAsync{TValue}(IMemoryCache, object, Func{ICacheEntry, Lazy{Task{TValue}}})"/>
	public static Task<TValue> GetOrCreateLazyAsync<TValue>(
		this IMemoryCache cache,
		object key,
		Func<object, Func<Task<TValue>>> valueFactory)
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyAsyncCore(cache, key, key =>
		{
			var factory = valueFactory(key) ?? throw new InvalidOperationException(CannotProcessNullFactory);
			var lazy = Lazy.Create(factory);
			cache.Set(key, lazy);
			return lazy;
		});
	}

	/// <summary>
	/// Attempts to retrieve the task associated with the provided <paramref name="key"/>.
	/// If it is not present it inserts a <see cref="Lazy{TValue}"/> of a <see cref="Task{TValue}"/> of <typeparamref name="TValue"/>
	/// from the <paramref name="valueFactory"/>.
	/// If the result of the Lazy or the Task causes an exception to be thrown, the item is evicted from the cache.
	/// </summary>
	/// <inheritdoc cref="GetOrCreateLazy{TValue}(IMemoryCache, object, Func{object, TValue})"/>
	public static Task<TValue> GetOrCreateLazyAsync<TValue>(
		this IMemoryCache cache,
		object key,
		Func<object, Task<TValue>> valueFactory)
	{
		if (cache is null) throw new ArgumentNullException(nameof(cache));
		if (key is null) throw new ArgumentNullException(nameof(key));
		if (valueFactory is null) throw new ArgumentNullException(nameof(valueFactory));
		Contract.EndContractBlock();

		return GetOrCreateLazyAsyncCore(cache, key, key =>
		{
			var lazy = Lazy.Create(() => valueFactory(key));
			cache.Set(key, lazy);
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazyAsync{TValue}(IMemoryCache, object, Func{object, Task{TValue}})"/>
	public static Task<TValue> GetOrCreateLazyAsync<TValue>(
		this IMemoryCache cache,
		object key,
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
}