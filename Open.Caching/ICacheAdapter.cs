namespace Open.Caching;

/// <summary>
/// Interface for defining a cache adapter.
/// </summary>
/// <typeparam name="TKey">The non-null key type.</typeparam>
public interface ICacheAdapter<TKey>
	where TKey : notnull
{
	/// <summary>
	/// Gets the <paramref name="item"/> associated with the provided <paramref name="key"/> if present.
	/// </summary>
	/// <param name="key">A <typeparamref name="TKey"/> identifying the requested item.</param>
	/// <param name="item">The located item or default for <typeparamref name="TValue"/>.</param>
	/// <param name="throwIfUnexpectedType"></param>
	/// <returns><see langword="true"/> if the key was found; otherwise <see langword="false"/>.</returns>
	/// <exception cref="InvalidCastException">
	/// If <paramref name="throwIfUnexpectedType"/> is <see langword="true"/>
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