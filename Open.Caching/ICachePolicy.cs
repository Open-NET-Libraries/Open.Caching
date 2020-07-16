using System;

namespace Open.Caching
{
	public interface ICachePolicy<out TPolicy>
		where TPolicy : ICachePolicy<TPolicy>
	{
		ExpirationMode Mode { get; }
		TimeSpan ExpiresAfter { get; }

		TPolicy Create(ExpirationMode mode, TimeSpan ExpiresAfter);
	}

	public interface ICachePolicy<TPriority, out TPolicy> : ICachePolicy<TPolicy>
		where TPolicy : ICachePolicy<TPriority, TPolicy>
	{
		TPriority Priority { get; }

		TPolicy Create(ExpirationMode mode, TimeSpan ExpiresAfter, TPriority priority);
	}

	public static class CachePolicyExtensions
	{
		public static TPolicy Slide<TPolicy>(this TPolicy policy) where TPolicy : ICachePolicy<TPolicy>
		=> policy.Mode == ExpirationMode.Sliding
			? policy
			: policy.Create(ExpirationMode.Sliding, policy.ExpiresAfter);

		public static TPolicy Expire<TPolicy>(this TPolicy policy) where TPolicy : ICachePolicy<TPolicy>
		=> policy.Mode == ExpirationMode.Absolute
			? policy
			: policy.Create(ExpirationMode.Absolute, policy.ExpiresAfter);

		public static TPolicy After<TPolicy>(this TPolicy policy, TimeSpan after) where TPolicy : ICachePolicy<TPolicy>
		=> policy.ExpiresAfter == after
			? policy
			: policy.Create(policy.Mode, after);

		public static TPolicy After<TPolicy>(this TPolicy policy, int seconds) where TPolicy : ICachePolicy<TPolicy>
		=> After(policy, TimeSpan.FromSeconds(seconds));

		public static TPolicy WithPriority<TPolicy, TPriority>(this TPolicy policy, TPriority priority) where TPolicy : ICachePolicy<TPriority, TPolicy>
		=> policy.Priority!.Equals(priority)
			? policy
			: policy.Create(policy.Mode, policy.ExpiresAfter, priority);
	}
}
