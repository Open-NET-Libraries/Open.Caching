using Microsoft.Extensions.Caching.Memory;

namespace Open.Caching.Extensions.Memory;
public static class MemoryCacheExtensions
{
	private const string CannotProcessNullFactory = "Cannot process a null factory.";
	private const string CannotProcessNullLazy = "Cannot process a null Lazy.";

	/// <remarks>
	/// If <paramref name="throwIfUnexpectedType"/> is true
	/// and the value found does not match either <typeparamref name="TItem"/>
	/// or a Lazy of <typeparamref name="TItem"/>
	/// then an <see cref="InvalidCastException"/> will be thrown.
	/// </remarks>
	/// <exception cref="InvalidCastException">
	/// If <paramref name="throwIfUnexpectedType"/> is true
	/// and the value does not resolve to the expected type.
	/// </exception>
	/// <inheritdoc cref="IMemoryCache.TryGetValue(object, out object)"/>
	public static bool TryGetLazy<TItem>(
		this IMemoryCache cache,
		object key,
		out Lazy<TItem> value,
		bool throwIfUnexpectedType = false)
	{
		if (cache.TryGetValue(key, out object o))
		{
			switch (o)
			{
				case null:
					value = Lazy.Default<TItem>();
					return true;

				case Lazy<TItem> lazy:
					value = lazy;
					return true;

				case TItem item:
					value = Lazy.Create(()=>item);
					_ = value.Value; // pre-create.
					return true;
			}
		}

		if (throwIfUnexpectedType)
			throw new InvalidCastException($"Expected a Lazy<{typeof(TItem)}> but actual type found was {o.GetType()}");

		value = default!;
		return false;
	}

	/// <summary>
	/// Attempts to retrieve the item associated with the provide key.
	/// If it is not present it inserts a Lazy of <typeparamref name="TItem"/>
	/// from the <paramref name="valueFactory"/>.
	/// If the result of the Lazy causes an exception to be thrown, the item is evicted from the cache.
	/// </summary>
	/// <exception cref="InvalidCastException">
	/// If a value is found but the type
	/// does not match either <typeparamref name="TItem"/>
	/// or a its container type.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// If <paramref name="valueFactory"/> returns null.
	/// </exception>
	public static TItem GetOrCreateLazy<TItem>(
		this IMemoryCache cache,
		object key,
		Func<ICacheEntry, Lazy<TItem>> valueFactory)
	{
		if (cache.TryGetValue(key, out object o))
		{
			return o switch
			{
				null => default!,
				Lazy<TItem> lz => lz.Value,
				TItem item => item,
				_ => throw new InvalidCastException($"Expected a Lazy<{typeof(TItem)}> but actual type found was {o.GetType()}")
			};
		}

		Lazy<TItem> lazy;
		using (var cacheEntry = cache.CreateEntry(key))
		{
			lazy = valueFactory(cacheEntry);
			if (lazy is null) throw new InvalidOperationException(CannotProcessNullLazy);
			cacheEntry.Value = lazy;
		}

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

	/// <inheritdoc cref="GetOrCreateLazy{TItem}(IMemoryCache, object, Func{ICacheEntry, Lazy{TItem}})" />
	public static TItem GetOrCreateLazy<TItem>(
		this IMemoryCache cache,
		object key,
		Func<ICacheEntry, Func<TItem>> valueFactory)

	{
		if (cache.TryGetValue(key, out object o))
		{
			return o switch
			{
				null => default!,
				Lazy<TItem> lz => lz.Value,
				TItem item => item,
				_ => throw new InvalidCastException($"Expected a Lazy<{typeof(TItem)}> but actual type found was {o.GetType()}")
			};
		}

		Lazy<TItem> lazy;
		using (var cacheEntry = cache.CreateEntry(key))
		{
			var factory = valueFactory(cacheEntry);
			if (factory is null) throw new InvalidOperationException(CannotProcessNullFactory);
			cacheEntry.Value = lazy = Lazy.Create(factory);
		}

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
	/// Attempts to retrieve the task associated with the provide key.
	/// If it is not present it inserts a Lazy of a Task of <typeparamref name="TItem"/>
	/// from the <paramref name="valueFactory"/>.
	/// If the result of the Lazy or the Task causes an exception to be thrown, the item is evicted from the cache.
	/// </summary>
	/// <inheritdoc cref="GetOrCreateLazy{TItem}(IMemoryCache, object, Func{ICacheEntry, Lazy{TItem}})" />
	public static Task<TItem> GetOrCreateLazyAsync<TItem>(
		this IMemoryCache cache,
		object key,
		Func<ICacheEntry, Func<Task<TItem>>> valueFactory)
	{
		if (cache.TryGetValue(key, out object o))
		{
			return o switch
			{
				null => Task.FromResult(default(TItem)!),
				Lazy<Task<TItem>> lz => lz.Value,
				Task<TItem> task => task,
				Lazy<TItem> lz => Task.FromResult(lz.Value),
				TItem item => Task.FromResult(item),
				_ => throw new InvalidCastException($"Expected a Lazy<{typeof(TItem)}> but actual type found was {o.GetType()}")
			};
		}

		Lazy<Task<TItem>> lazy;
		using (var cacheEntry = cache.CreateEntry(key))
		{
			var factory = valueFactory(cacheEntry);
			if (factory is null) throw new InvalidOperationException(CannotProcessNullFactory);
			cacheEntry.Value = lazy = Lazy.Create(factory);
		}

		try
		{
			var task = lazy.Value;
			return task.ContinueWith(t =>
			{
				if(t.IsFaulted || t.IsCanceled)
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
}
