using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Open.Caching
{
	public abstract class CacheBase<TPriority> :
		CachePolicyControllerBase<TPriority>, ICache<TPriority>
	{
		#region ICache<TPriority> Members

		public abstract T GetOrAdd<T>(string key, ExpirationMode mode, TimeSpan elapsed, TPriority priority, Func<T> valueFactory);

		public virtual T GetOrAdd<T>(string key, ExpirationMode mode, TimeSpan elapsed, TPriority priority, Func<string, T> valueFactory)
		{
			return GetOrAdd(key, mode, elapsed, priority, () => valueFactory(key));
		}

		#endregion

		#region ICache<TPriority> Members

		public T Slide<T>(string key, TimeSpan elapsed, Func<string, T> valueFactory)
		{
			if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException("elapsed");
			return GetOrAdd<T>(key, ExpirationMode.Sliding, elapsed, Priority, valueFactory);
		}

		public T Expire<T>(string key, TimeSpan elapsed, Func<string, T> valueFactory)
		{
			if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException("elapsed");
			return GetOrAdd<T>(key, ExpirationMode.Absolute, elapsed, Priority, valueFactory);
		}

		public T Slide<T>(string key, TimeSpan elapsed, Func<T> valueFactory)
		{
			if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException("elapsed");
			return GetOrAdd<T>(key, ExpirationMode.Sliding, elapsed, Priority, valueFactory);
		}

		public T Expire<T>(string key, TimeSpan elapsed, Func<T> valueFactory)
		{
			if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException("elapsed");
			return GetOrAdd<T>(key, ExpirationMode.Absolute, elapsed, Priority, valueFactory);
		}

		#endregion

		#region IGetOrAdd Members

		public T GetOrAdd<T>(string key, Func<string, T> valueFactory)
		{
			return GetOrAdd<T>(key, Mode, ExpiresAfter, Priority, valueFactory);
		}

		public T GetOrAdd<T>(string key, Func<T> valueFactory)
		{
			return GetOrAdd<T>(key, Mode, ExpiresAfter, Priority, valueFactory);
		}

		#endregion



		#region IPolicyController<ICachingModifier<TPriority>,TPriority> Members

		CachingModifier<TPriority> NewModifier()
		{
			return new CachingModifier<TPriority>(this);
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Expire(TimeSpan after)
		{
			var modifier = NewModifier();
			modifier.SetAbsolute(after);
			return modifier;
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Expire(int seconds)
		{
			return Expire(TimeSpan.FromSeconds(seconds));
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Expire(uint seconds)
		{
			return Expire(TimeSpan.FromSeconds(seconds));
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Slide(TimeSpan after)
		{
			var modifier = NewModifier();
			modifier.SetSliding(after);
			return modifier;
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Slide(int seconds)
		{
			return Slide(TimeSpan.FromSeconds(seconds));
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Slide(uint seconds)
		{
			return Slide(TimeSpan.FromSeconds(seconds));
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> Prioritize(TPriority level)
		{
			var modifier = NewModifier();
			modifier.Priority = level;
			return modifier;
		}

		public IPolicyModifier<ICachingModifier<TPriority>, TPriority> SyncTimeoutAfter(TimeSpan timeout)
		{
			var modifier = NewModifier();
			modifier.SyncTimeout = timeout;
			return modifier;
		}

		#endregion
	}
}
