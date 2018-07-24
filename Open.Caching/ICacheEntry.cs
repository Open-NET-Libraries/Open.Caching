using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Open.Caching
{
	[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
	public interface ICacheEntry<T>
	{
		bool HasValue { get; }
		T Value { get; }
		T DefaultValue { get; }

		Task<T> GetOrAddAsync(Func<T> factory, bool runSynchronously);
		Task<T> GetOrAddAsync(Func<Task<T>> factory);
		T GetOrAdd(Func<T> factory);
		T GetOrDefault(T defaultValue);
	}

}
