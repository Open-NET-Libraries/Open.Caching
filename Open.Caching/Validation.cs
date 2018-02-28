using System;

namespace Open.Caching
{
	public static class Validation
	{
		public static bool IsValidExpiresSliding(TimeSpan? slide)
		{
			return !slide.HasValue || IsValidExpiresSliding(slide.Value);
		}

		public static bool IsValidExpiresAbsolute(TimeSpan? abs)
		{
			return !abs.HasValue || abs.Value > TimeSpan.Zero;
		}

		public static bool IsValidExpiresSliding(TimeSpan slide)
		{
			return slide >= TimeSpan.Zero;
		}
	}
}
