using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace Open.Caching
{
	public abstract class CachePolicyBase<TOptions, TPolicy> : ICachePolicy<TPolicy>, ICacheHelper<TOptions>
		where TPolicy : CachePolicyBase<TOptions, TPolicy>
		where TOptions : class
	{
		protected CachePolicyBase(ExpirationMode mode, TimeSpan after)
		{
			if (after < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(after), after, "Cannot be a negative value.");
			Contract.EndContractBlock();

			Mode = mode;
			ExpiresAfter = after;
		}

		public object this[string key]
		{
			get => Get(key);
			set => Insert(key, value);
		}

		public ExpirationMode Mode { get; private set; }
		public TimeSpan ExpiresAfter { get; private set; }

		public abstract TPolicy Create(ExpirationMode mode, TimeSpan expiresAfter);

		protected static Task<T> ReturnAsTask<T>(object value)
		{
			if (value == null) return null;
			if (value is Task<T> task) return task;
			if (value is T v) return Task.FromResult(v);
			Debug.Fail("Actual type does not match expected for cache entry.");
			return null;
		}

		public abstract object Get(string key);

		public abstract object Add<T>(string key, T value, TOptions options = null);
		public abstract void Insert<T>(string key, T value, TOptions options = null);

		public Task<T> GetOrAddAsync<T>(string key, Func<T> factory, bool runSynchronously, Func<TOptions> options = null)
		{
			// Check if task/value exists first.
			var current = ReturnAsTask<T>(Get(key));
			if (current != null) return current;

			// Setup the task.
			var task = new Task<T>(factory);

			// Attempt to add the task.  If current is not null, then return it (not ours).
			current = ReturnAsTask<T>(Add(key, task, options?.Invoke()));

			// The .Add implementations can take 2 forms: Returns the previous value, or the current value.
			// Either way, if it's null or equals our task, then we need to start the task.
			if(current==null || current==task)
			{
				// We own the task.  Go ahead and run it.
				if (runSynchronously) task.RunSynchronously();
				else task.Start();

				current = task;
			}

			return current;
		}

		public Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, Func<TOptions> options = null)
		{
			// Check if task/value exists first.
			var current = ReturnAsTask<T>(Get(key));
			if (current != null) return current;

			// Setup the task.
			var task = new Task<Task<T>>(factory);
			var unwrapped = task.Unwrap();

			// Attempt to add the task.  If current is not null, then return it (not ours).
			current = ReturnAsTask<T>(Add(key, unwrapped, options?.Invoke()));

			// The .Add implementations can take 2 forms: Returns the previous value, or the current value.
			// Either way, if it's null or equals our task, then we need to start the task.
			if (current == null || current == unwrapped)
			{
				// We own the task.  Go ahead and run it.
				task.Start();

				current = unwrapped;
			}

			return current;
		}

		public T GetOrAdd<T>(string key, Func<T> factory, Func<TOptions> options = null)
			=> GetOrAddAsync(key, factory, true, options).Result;

		public ICacheEntry<T> Entry<T>(string key, T defaultValue = default(T))
			=> new CacheEntry<T>(this, key, defaultValue);

		void ICacheHelper.Insert<T>(string key, T value)
			=> Insert(key, value);

		void ICacheHelper<TOptions>.Insert<T>(string key, T value, TOptions options)
			=> Insert(key, value, options);


		object ICacheHelper.Add<T>(string key, T value)
			=> Add(key, value);

		object ICacheHelper<TOptions>.Add<T>(string key, T value, TOptions options)
			=> Add(key, value, options);


		Task<T> ICacheHelper.GetOrAddAsync<T>(string key, Func<T> factory, bool runSynchronously)
			=> GetOrAddAsync(key, factory, runSynchronously);

		Task<T> ICacheHelper.GetOrAddAsync<T>(string key, Func<Task<T>> factory)
			=> GetOrAddAsync(key, factory);

		T ICacheHelper.GetOrAdd<T>(string key, Func<T> factory)
			=> GetOrAdd(key, factory);


		Task<T> ICacheHelper<TOptions>.GetOrAddAsync<T>(string key, Func<T> factory, bool runSynchronously, Func<TOptions> options)
			=> GetOrAddAsync(key, factory, runSynchronously, options);

		Task<T> ICacheHelper<TOptions>.GetOrAddAsync<T>(string key, Func<Task<T>> factory, Func<TOptions> options)
			=> GetOrAddAsync(key, factory, options);

		T ICacheHelper<TOptions>.GetOrAdd<T>(string key, Func<T> factory, Func<TOptions> options)
			=> GetOrAdd(key, factory, options);

	}

	public abstract class CachePolicyBase<TPriority, TOptions, TPolicy> : CachePolicyBase<TOptions, TPolicy>, ICachePolicy<TPriority, TPolicy>
		where TPolicy : CachePolicyBase<TPriority, TOptions, TPolicy>
		where TOptions : class
	{
		protected CachePolicyBase(ExpirationMode mode, TimeSpan after, TPriority priority) : base(mode, after)
		{
			Priority = priority;
		}

		public TPriority Priority { get; private set; }
		public abstract TPolicy Create(ExpirationMode mode, TimeSpan expiresAfter, TPriority priority);
	}
}
