/* The MIT License (MIT)

Copyright (c) 2015 Oren J. Ferrari

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

using System;
using System.Diagnostics.Contracts;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using Open.Threading;


namespace Open.Caching
{
	#region Delegates
	public delegate bool CacheItemDelegate<T>(out T result);
	public delegate bool CacheItemWithPolicyDelegate<T>(out T result, out CacheItemPolicy policy);
	#endregion
	
	public static class ObjectCacheExtensions
	{

		public static readonly object NullPlaceHolder = new Object();

		const int SYNC_TIMEOUT_DEFAULT_MILLISECONDS = 100000;

		private static readonly ConditionalWeakTable<ObjectCache, ReadWriteHelper<string>> _sychronizeReadWriteRegistry
			= new ConditionalWeakTable<ObjectCache, ReadWriteHelper<string>>();

		internal static ReadWriteHelper<string> GetReadWriteHelper(this ObjectCache cache)
		{
			Contract.Requires(cache!=null);
			Contract.Ensures(Contract.Result<ReadWriteHelper<string>>() != null);

			var result = _sychronizeReadWriteRegistry.GetOrCreateValue(cache);
			Contract.Assume(result != null);
			return result;
		}



		/// <summary>
		/// Manages optimal synchronization of retrieval and insertion of cache values.
		/// This method signature is specifically for use with RenderItemDelegate<typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of response.</typeparam>
		/// <param name="cache">The Cache object context. </param>
		/// <param name="cacheKey">The string cacheKey used to access the cache.</param>
		/// <param name="result">The output variable to recieve the response.</param>
		/// <param name="syncTimeout">Optional milliseconds timeout for thread synchronization to wait for cache access.</param>
		/// <param name="renderer">CacheItemWithPolicyDelegate<typeparamref name="T"/>; that will output the results and return the renderer's success value.</param>
		/// <returns>The true/false response of the provided CacheItemWithPolicyDelegate<typeparamref name="T"/>.</returns>
		public static bool EnsureItem<T>(
			this ObjectCache cache,
			string cacheKey,
			out T result,
			int syncTimeout,
			CacheItemWithPolicyDelegate<T> renderer) where T : class
		{
			Contract.Requires<NullReferenceException>(renderer != null);
			Contract.Requires<ArgumentNullException>(cacheKey != null);
			Contract.Requires<ArgumentOutOfRangeException>(syncTimeout >= 0);

			CacheItemPolicy policy;
			if (cache == null)
				return renderer(out result, out policy);

			bool success = true;
			T r = null;

			cache.GetReadWriteHelper()
				.ReadWriteConditionalOptimized(cacheKey, // Syncronizes access by cacheKey...
				() =>
				{
					if (!cache.Contains(cacheKey))
						return true;

					object o = cache[cacheKey];
					if (o == NullPlaceHolder)
						return false;

					r = o as T;
					return r == null;
				},
				() =>
				{
					if (renderer(out r, out policy))
						cache.Set(cacheKey, r ?? NullPlaceHolder, policy);
					else
						success = false;
				},
				syncTimeout, true);
			result = r;

			return success;
		}

		/// <summary>
		/// Manages optimal synchronization of retrieval and insertion of cache values.
		/// This method signature is specifically for use with RenderItemDelegate<typeparamref name="TLock"/>.
		/// </summary>
		/// <typeparam name="TLock">The type of response.</typeparam>
		/// <param name="cache">The Cache object context. </param>
		/// <param name="cacheKey">The string cacheKey used to access the cache.</param>
		/// <param name="response">The output variable to recieve the response.</param>
		/// <param name="renderer">RenderItemDelegate<typeparamref name="TLock"/>; that will output the results and return a lockHeld source.</param>
		/// <param name="syncTimeout">Optional milliseconds timeout for thread synchronization to wait for cache access.</param>
		/// <returns>The true/false response of the provided CacheItemDelegate<typeparamref name="TLock"/>.</returns>
		public static bool EnsureItem<T>(
			this ObjectCache cache,
			string cacheKey,
			out T result,
			CacheItemDelegate<T> renderer,
			Func<CacheItemPolicy> policyFactory,
			int syncTimeout = SYNC_TIMEOUT_DEFAULT_MILLISECONDS) where T : class
		{
			Contract.Requires<NullReferenceException>(renderer != null);
			Contract.Requires<ArgumentNullException>(cacheKey != null);
			Contract.Requires<ArgumentOutOfRangeException>(syncTimeout >= 0);


			return EnsureItem<T>(cache, cacheKey, out result, syncTimeout,
				delegate(out T r, out CacheItemPolicy policy)
				{
					var success = renderer(out r);
					policy = success ? policyFactory() : null;
					return success;
				});

		}

		public static bool EnsureAbsoluteItem<T>(
			this ObjectCache cache,
			string cacheKey,
			out T result,
			int syncTimeout,
			TimeSpan absAfter,
			CacheItemDelegate<T> renderer,
			CacheItemPriority priority = CacheItemPriority.Default) where T : class
		{
			Contract.Requires<NullReferenceException>(renderer != null);
			Contract.Requires<ArgumentNullException>(cacheKey != null);
			Contract.Requires<ArgumentOutOfRangeException>(syncTimeout >= 0);


			return EnsureItem<T>(cache, cacheKey, out result, syncTimeout,
				delegate(out T r, out CacheItemPolicy policy)
				{
					var success = renderer(out r);
					policy = success ? new CacheItemPolicy
					{
						Priority = priority,
						AbsoluteExpiration = DateTime.Now.Add(absAfter),
						SlidingExpiration = ObjectCache.NoSlidingExpiration
					} : null;
					return success;
				});
		}

		public static bool EnsureAbsoluteItem<T>(
			this ObjectCache cache,
			string cacheKey,
			out T result,
			TimeSpan absAfter,
			CacheItemDelegate<T> renderer,
			CacheItemPriority priority = CacheItemPriority.Default) where T : class
		{
			return EnsureAbsoluteItem(cache, cacheKey, out result, SYNC_TIMEOUT_DEFAULT_MILLISECONDS, absAfter, renderer, priority);
		}

		public static bool EnsureAbsoluteItem<T>(
			this ObjectCache cache,
			string cacheKey,
			out T result,
			int syncTimeout,
			Func<DateTime> absTime,
			CacheItemDelegate<T> renderer,
			CacheItemPriority priority = CacheItemPriority.Default) where T : class
		{
			Contract.Requires<NullReferenceException>(renderer != null);
			Contract.Requires<ArgumentNullException>(cacheKey != null);
			Contract.Requires<ArgumentOutOfRangeException>(syncTimeout >= 0);


			return EnsureItem<T>(cache, cacheKey, out result, syncTimeout,
				delegate(out T r, out CacheItemPolicy policy)
				{
					var success = renderer(out r);
					policy = success ? new CacheItemPolicy
					{
						Priority = priority,
						AbsoluteExpiration = absTime(),
						SlidingExpiration = ObjectCache.NoSlidingExpiration
					} : null;
					return success;
				});
		}

		public static bool EnsureAbsoluteItem<T>(
			this ObjectCache cache,
			string cacheKey,
			out T result,
			Func<DateTime> absTime,
			CacheItemDelegate<T> renderer,
			CacheItemPriority priority = CacheItemPriority.Default) where T : class
		{
			return EnsureAbsoluteItem(cache, cacheKey, out result, SYNC_TIMEOUT_DEFAULT_MILLISECONDS, absTime, renderer, priority);
		}

		public static bool EnsureSlidingItem<T>(
			this ObjectCache cache,
			string cacheKey,
			out T result,
			TimeSpan slide,
			int syncTimeout,
			CacheItemDelegate<T> renderer,
			CacheItemPriority priority = CacheItemPriority.Default) where T : class
		{
			Contract.Requires<NullReferenceException>(renderer != null);
			Contract.Requires<ArgumentNullException>(cacheKey != null);
			Contract.Requires<ArgumentOutOfRangeException>(syncTimeout >= 0);

			if (!Validation.IsValidExpiresSliding(slide))
				throw new ArgumentOutOfRangeException("slide");

			return EnsureItem<T>(cache, cacheKey, out result, syncTimeout,
				delegate(out T r, out CacheItemPolicy policy)
				{
					var success = renderer(out r);
					policy = success ? new CacheItemPolicy
					{
						Priority = priority,
						AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration,
						SlidingExpiration = slide
					} : null;
					return success;
				});
		}


		public static bool EnsureSlidingItem<T>(
			this ObjectCache cache,
			string cacheKey,
			out T result,
			TimeSpan slide,
			CacheItemDelegate<T> renderer,
			CacheItemPriority priority = CacheItemPriority.Default) where T : class
		{
			return EnsureSlidingItem<T>(cache, cacheKey, out result, slide, SYNC_TIMEOUT_DEFAULT_MILLISECONDS, renderer, priority);
		}

		/// <summary>
		/// Manages optimal synchronization of retrieval and insertion of cache values.
		/// This method signature is specifically for use with a Func&lt;<typeparamref name="T"/>&gt;.
		/// </summary>
		/// <typeparam name="T">The type of response.</typeparam>
		/// <param name="cache">The Cache object context.</param>
		/// <param name="cacheKey">The string cacheKey used to access the cache.</param>
		/// <param name="syncTimeout">Optional milliseconds timeout for thread synchronization to wait for cache access.</param>
		/// <param name="valueFactory">Func&lt;<typeparamref name="T"/>&gt; that will output the results and return a lockHeld source.</param>
		/// <param name="policyFactory">Generates the desired policy upon setting the cache value.</param>
		/// <returns></returns>
		public static T EnsureItem<T>(
			this ObjectCache cache,
			string cacheKey,
			int syncTimeout,
			Func<T> valueFactory,
			Func<CacheItemPolicy> policyFactory)
		{
			Contract.Requires<NullReferenceException>(valueFactory != null);
			Contract.Requires<ArgumentNullException>(cacheKey != null);
			Contract.Requires<ArgumentOutOfRangeException>(syncTimeout >= 0);

			if (cache == null)
				return valueFactory();

			T result = default(T);
			Func<bool> condition = () =>
			{
				if (!cache.Contains(cacheKey))
					return true;

				object r = cache[cacheKey];
				if (r == NullPlaceHolder)
					return false;

				if (r != null)
				{
					try
					{
						result = (T)r;
					}
					catch (InvalidCastException)
					{
						r = null;
					}
				}
				return r == null;
			};
			Action render = () =>
			{
				object value = result = valueFactory();
				cache.Set(cacheKey, value ?? NullPlaceHolder, policyFactory());
			};

			// Use syncWriteLockAcquisition=false since the cache insertion is synchronized behind the scenes
			// and it is safer to avoid locking the cache.
			if (!cache.GetReadWriteHelper()
				.ReadWriteConditionalOptimized(cacheKey, condition, render, syncTimeout, false))
				render(); // Timeout failed? Lock insert anyway and move on...

			return result;
		}

		/// <summary>
		/// Manages optimal synchronization of retrieval and insertion of cache values.
		/// This method signature is specifically for use with a Func&lt;<typeparamref name="T"/>&gt;.
		/// </summary>
		/// <typeparam name="T">The type of response.</typeparam>
		/// <param name="cache">The Cache object context.</param>
		/// <param name="cacheKey">The string cacheKey used to access the cache.</param>
		/// <param name="valueFactory">Func&lt;<typeparamref name="T"/>&gt; that will output the results and return a lockHeld source.</param>
		/// <param name="policyFactory">Generates the desired policy upon setting the cache value.</param>
		/// <returns></returns>
		public static T EnsureItem<T>(
			this ObjectCache cache,
			string cacheKey,
			Func<T> valueFactory,
			Func<CacheItemPolicy> policyFactory)
		{
			Contract.Requires<ArgumentNullException>(cacheKey != null);
			Contract.Requires<ArgumentNullException>(valueFactory != null);
			Contract.Requires<ArgumentNullException>(policyFactory != null);

			return EnsureItem<T>(cache, cacheKey, SYNC_TIMEOUT_DEFAULT_MILLISECONDS, valueFactory, policyFactory);
		}

		public static T GetOrAdd<T>(
			this ObjectCache cache,
			string key,
			ExpirationMode mode,
			TimeSpan elapsed,
			CacheItemPriority priority,
			Func<T> valueFactory)
		{
			switch (mode)
			{
				case ExpirationMode.Sliding:
					return cache.EnsureItem<T>(key, valueFactory, () => new CacheItemPolicy
					{
						Priority = priority,
						AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration,
						SlidingExpiration = elapsed
					});
				default: // Compiler doesn't reconginze that there's only one other option so use default.
					return cache.EnsureItem<T>(key, valueFactory, () => new CacheItemPolicy
					{
						Priority = priority,
						AbsoluteExpiration = DateTime.Now.Add(elapsed),
						SlidingExpiration = ObjectCache.NoSlidingExpiration
					});

			}
		}

		public static ObjectCacheModifier Slide(
			this ObjectCache cache,
			TimeSpan slide)
		{
			if (!Validation.IsValidExpiresSliding(slide))
				throw new ArgumentOutOfRangeException("slide");

			var modifier = new ObjectCacheModifier(cache);
			modifier.SetSliding(slide);
			return modifier;
		}

		public static ObjectCacheModifier Expire(
			this ObjectCache cache,
			TimeSpan elapsed)
		{
			if (!Validation.IsValidExpiresAbsolute(elapsed))
				throw new ArgumentOutOfRangeException("elapsed");

			var modifier = new ObjectCacheModifier(cache);
			modifier.SetAbsolute(elapsed);
			return modifier;
		}

		public static ObjectCacheModifier Prioritize(
			this ObjectCache cache,
			CacheItemPriority priority)
		{
			return new ObjectCacheModifier(cache, priority);
		}

		public static ObjectCacheModifier SyncTimeoutAfter(
			this ObjectCache cache,
			TimeSpan timeout)
		{
			if (!Validation.IsValidExpiresAbsolute(timeout))
				throw new ArgumentOutOfRangeException("elapsed");

			var modifier = new ObjectCacheModifier(cache);
			modifier.SyncTimeoutAfter(timeout);
			return modifier;
		}

	}
}
