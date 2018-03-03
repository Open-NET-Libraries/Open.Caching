using System;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace Open.Caching.Runtime
{
	public class ObjectCachePolicy
	{
		public readonly ObjectCache Cache;

		public readonly ExpirationMode Mode;
		public readonly TimeSpan ExpiresAfter;
		public readonly CacheItemPriority Priority;
		public readonly TimeSpan ExpiresSliding;
		public DateTimeOffset ExpiresAbsolute => Mode == ExpirationMode.Absolute ? DateTimeOffset.Now.Add(ExpiresAfter) : ObjectCache.InfiniteAbsoluteExpiration;

		internal ObjectCachePolicy(ObjectCache target, ExpirationMode mode, TimeSpan expiresAfter, CacheItemPriority priority)
		{
			Cache = target ?? throw new ArgumentNullException(nameof(target));
			Mode = mode;
			ExpiresAfter = expiresAfter;
			Priority = priority;

			ExpiresSliding = mode == ExpirationMode.Sliding ? expiresAfter : ObjectCache.NoSlidingExpiration;
		}

		public CacheItemPolicy GetItemPolicy(
				CacheEntryUpdateCallback updateCallback = null,
				CacheEntryRemovedCallback removedCallback = null)
			=> new CacheItemPolicy
			{
				AbsoluteExpiration = ExpiresAbsolute,
				SlidingExpiration = ExpiresSliding,
				Priority = Priority,
				UpdateCallback = updateCallback,
				RemovedCallback = removedCallback
			};

		public CacheItemPolicy GetItemPolicy(
				CacheEntryRemovedCallback removedCallback,
				CacheEntryUpdateCallback updateCallback = null)
			=> GetItemPolicy(updateCallback, removedCallback);

		static Task<T> ReturnAsTask<T>(object value)
		{
			if (value == null) return null;
			if (value is Task<T> task) return task;
			if (value is T v) return Task.FromResult(v);
			Debug.Fail("Actual type does not match expected for cache entry.");
			return null;
		}
		
		public Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
		{
			// Check if task/value exists first.
			var current = ReturnAsTask<T>(HttpRuntime.Cache?[key]);
			if (current != null) return current;

			// Setup the task.
			var task = new Task<Task<T>>(factory);
			var unwrapped = task.Unwrap();

			// Attempt to add the task.  If current is not null, then return it (not ours).
			current = ReturnAsTask<T>(Add(key, unwrapped));

			if (current != null) return current;
			/* If current is null here, then either:
				1) Ours was used. https://msdn.microsoft.com/en-us/library/system.web.caching.cache.add(v=vs.110).aspx
				2) There is no HttpRuntime.Cache available.
				3) There is a value/type collision that will trigger Debug.Fail but not be fatal otherwise. */

			// We own the task.  Go ahead and run it.
			task.Start();

			return unwrapped;
		}


		public Task<T> GetOrAddAsync<T>(string key, Func<T> factory, bool runSynchronously = false)
		{
			// Check if task/value exists first.
			var current = ReturnAsTask<T>(Cache[key]);
			if (current != null) return current;

			// Setup the task.
			var task = new Task<T>(factory);

			// Attempt to add the task.  If current is not null, then return it (not ours).
			current = ReturnAsTask<T>(Cache.AddOrGetExisting(key, task, GetItemPolicy()));

			if (current != null) return current;
			/* If current is null here, then either:
				1) Ours was used. https://msdn.microsoft.com/en-us/library/ee395901(v=vs.110).aspx
				2) There is a value/type collision that will trigger Debug.Fail but not be fatal otherwise. */

			// We own the task.  Go ahead and run it.
			if (runSynchronously) task.RunSynchronously();
			else task.Start();

			return task;
		}

		public T GetOrAdd<T>(string key, Func<T> factory)
			=> GetOrAddAsync(key, factory, true).Result;
	}

}
