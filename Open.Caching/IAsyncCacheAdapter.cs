namespace Open.Caching;

public interface IAsyncCacheAdapter<TKey>
{
	/// <summary>
	/// Gets the item associated with this key if present.
	/// </summary>
	/// <param name="key">A <typeparamref name="TKey"/> identifying the requested item.</param>
	/// <param name="throwIfUnexpectedType"></param>
	/// <returns>True if the key was found; otherwise false.</returns>
	/// <exception cref="InvalidCastException">
	/// If <paramref name="throwIfUnexpectedType"/> is true
	/// and the value does not resolve to the expected type.
	/// </exception>
	/// <returns>
	/// The result of this async method is a Task of <typeparamref name="TValue"/>.
	/// If the item is found, a Task is returned containing the value.
	/// If the item is not availalbe, null is returned.
	/// </returns>
	ValueTask<Task<TValue>?> GetValueAsync<TValue>(
		TKey key,
		bool throwIfUnexpectedType = false);

	/// <summary>
	/// Adds or overwrites an item in the cache.
	/// </summary>
	/// <param name="key">A <typeparamref name="TKey"/> identifying the item.</param>
	/// <param name="item">The item to insert.</param>
	ValueTask SetAsync<TValue>(TKey key, TValue item);

	/// <summary>
	/// Removes an item from the cache.
	/// </summary>
	/// <param name="key"></param>
	ValueTask RemoveAsync(TKey key);
}
