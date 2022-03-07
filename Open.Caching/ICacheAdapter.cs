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