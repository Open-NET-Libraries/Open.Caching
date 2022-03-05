using Microsoft.Extensions.Caching.Memory;
using System;

namespace Open.Caching.Memory
{
	public class MemoryCacheAdapter : ICacheAdapter, ICacheAdapter<string>
	{
		private readonly IMemoryCache _cache;

		public MemoryCacheAdapter(IMemoryCache cache)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		public void Remove(object key)
			=> _cache.Remove(key);

		public void Set<TValue>(object key, TValue item)
			=> _cache.Set(key, item);

		public bool TryGetValue<TValue>(object key, out TValue item, bool throwIfUnexpectedType = false)
		{
			if (_cache.TryGetValue(key, out object o))
			{
				switch (o)
				{
					case null:
						item = default!;
						return true;

					case TValue v:
						item = v;
						return true;
				}
			}

			if (throwIfUnexpectedType)
				throw new InvalidCastException($"Expected {typeof(TValue)} but actual type found was {o.GetType()}");

			item = default!;
			return false;
		}

		void ICacheAdapter<string>.Remove(string key)
			=> Remove(key);

		void ICacheAdapter<string>.Set<TValue>(string key, TValue item)
			=> Set(key, item);

		bool ICacheAdapter<string>.TryGetValue<TValue>(string key, out TValue item, bool throwIfUnexpectedType)
			=> TryGetValue(key, out item, throwIfUnexpectedType);
	}
}
