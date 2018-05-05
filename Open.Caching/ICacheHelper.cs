using System;
using System.Threading.Tasks;

namespace Open.Caching
{
	public interface ICacheHelper
	{
		object this[string key] { get; set; }

		void Insert<T>(string key, T value);
		object Add<T>(string key, T value);

		Task<T> GetOrAddAsync<T>(string key, Func<T> factory, bool runSynchronously);
		Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory);

		T GetOrAdd<T>(string key, Func<T> factory);
	}

	public interface ICacheHelper<TOptions> : ICacheHelper
		where TOptions : class
	{
		void Insert<T>(string key, T value, TOptions options);
		object Add<T>(string key, T value, TOptions options);

		Task<T> GetOrAddAsync<T>(string key, Func<T> factory, bool runSynchronously, Func<TOptions> options);
		Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, Func<TOptions> options);

		T GetOrAdd<T>(string key, Func<T> factory, Func<TOptions> options);
	}
}
