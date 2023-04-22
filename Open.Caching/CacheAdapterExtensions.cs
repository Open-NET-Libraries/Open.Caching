using System.Diagnostics.Contracts;

namespace Open.Caching;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1229:Use async/await when necessary.")]
public static class CacheAdapterExtensions
{
	internal static bool IsNullableType<T>()
	{
		var type = typeof(T);
		return type.IsClass || Nullable.GetUnderlyingType(type) != null;
	}

	internal static InvalidCastException UnexpectedTypeException<T>(object? o)
		=> new(o is null
		? $"Expected type {typeof(T)} but actual value was null."
		: $"Expected type {typeof(T)} but actual type found was {o.GetType()}.");

	/// <summary>
	/// Gets the value from the cache otherwise returns the default of <typeparamref name="TValue"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">If the type does not match the <typeparamref name="TValue"/>.</exception>
	public static TValue GetOrDefault<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		TValue defaultValue = default!)
		=> cache.TryGetValue(key, out TValue value, true) ? value : defaultValue;

	private const string CannotProcessNullFactory = "Cannot process a null factory.";

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
				case null when IsNullableType<TValue>():
					value = Lazy.Default<TValue>();
					return true;

				case Lazy<TValue> lazy:
					value = lazy;
					return true;

				case TValue item:
					value = Lazy.Create(() => item);
					return true;
			}

			if (throwIfUnexpectedType)
				throw UnexpectedTypeException<Lazy<TValue>>(o);
		}

		value = default!;
		return false;
	}

	private static TValue GetOrCreateLazyCore<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key, Func<TKey, Lazy<TValue>> valueFactory)
	{
		if (!cache.TryGetValue(key, out object o))
		{
			var lazy = valueFactory(key);
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

	/// <exception cref="InvalidCastException">
	/// If a value is found but the type
	/// does not match either <typeparamref name="TValue"/>
	/// or a its container type.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// If <paramref name="valueFactory"/> returns null.
	/// </exception>
	/// <inheritdoc cref="GetOrCreateLazy{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, TValue})"/>
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

	/// <inheritdoc cref="GetOrCreateLazy{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, TValue})"/>
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
		if (!cache.TryGetValue(key, out object o))
		{
			var lazy = valueFactory(key);

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

	/// <inheritdoc cref="GetOrCreateLazyAsync{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, Task{TValue}})"/>
	/// <inheritdoc cref="GetOrCreateLazy{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, Func{TValue}}))"/>
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

	/// <summary>
	/// Attempts to retrieve the task associated with the provide key.
	/// If it is not present it inserts a Lazy of a Task of <typeparamref name="TValue"/>
	/// from the <paramref name="valueFactory"/>.
	/// If the result of the Lazy or the Task causes an exception to be thrown, the item is evicted from the cache.
	/// </summary>
	/// <inheritdoc cref="GetOrCreateLazy{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, TValue})"/>
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
			var lazy = Lazy.Create(() => valueFactory(key));
			cache.Set(key, lazy);
			return lazy;
		});
	}

	/// <inheritdoc cref="GetOrCreateLazyAsync{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, Task{TValue}})"/>
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

	/// <summary>
	/// Creates a pre-keyed item that the value can be retrieved from or update to the cache.
	/// </summary>
	public static CacheItem<TKey, TValue> CreateItem<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		TValue defaultValue = default!)
		=> new(cache, key, defaultValue);

	/// <summary>
	/// Creates a pre-keyed item that the value can be retrieved from will be updated by the value factory.
	/// </summary>
	public static LazyCacheItem<TKey, TValue> CreateLazyItem<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		Func<TValue> factory)
		=> new(cache, key, factory);

	/// <inheritdoc cref="CreateLazyItem{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, TValue})">
	public static LazyCacheItem<TKey, TValue> CreateLazyItem<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		Func<TKey, TValue> factory)
		=> new(cache, key, factory);

	/// <inheritdoc cref="CreateLazyItem{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, TValue})">
	public static AsyncLazyCacheItem<TKey, TValue> CreateAsyncLazyItem<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		Func<Task<TValue>> factory)
		=> new(cache, key, factory);

	/// <inheritdoc cref="CreateLazyItem{TKey, TValue}(ICacheAdapter{TKey}, TKey, Func{TKey, TValue})">
	public static AsyncLazyCacheItem<TKey, TValue> CreateAsyncLazyItem<TKey, TValue>(
		this ICacheAdapter<TKey> cache,
		TKey key,
		Func<TKey, Task<TValue>> factory)
		=> new(cache, key, factory);
}