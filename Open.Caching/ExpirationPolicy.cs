namespace Open.Caching;

public readonly record struct ExpirationPolicy
{
	private const string MustBePositive = "Must be at a positive value";

	public TimeSpan Absolute { get; }
	public TimeSpan Sliding { get; }

	public bool HasAbsolute => Absolute != TimeSpan.Zero;
	public bool HasSliding => Sliding != TimeSpan.Zero;

	public DateTimeOffset AbsoluteRelativeToNow
		=> Absolute==TimeSpan.Zero
		? DateTimeOffset.MaxValue
		: DateTimeOffset.Now + Absolute;

	public ExpirationPolicy(TimeSpan absolute, TimeSpan sliding)
	{
		if (absolute < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(absolute), absolute, MustBePositive);
		if (sliding < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(sliding), sliding, MustBePositive);

		Absolute = absolute;
		Sliding = sliding;
	}

	public ExpirationPolicy After(TimeSpan value) => new(value, Sliding);

	public ExpirationPolicy Slide(TimeSpan value) => new(Absolute, value);
}

public static class Expire
{
	public static ExpirationPolicy Policy(TimeSpan absolute, TimeSpan sliding)
		=> new(absolute, sliding);

	public static ExpirationPolicy Absolute(TimeSpan value)
		=> new(value, TimeSpan.Zero);

	public static ExpirationPolicy Sliding(TimeSpan value)
		=> new(TimeSpan.Zero, value);

	public static bool IsValidSliding(TimeSpan? slide)
	=> !slide.HasValue || IsValidSliding(slide.Value);

	public static bool IsValidAbsolute(TimeSpan? abs)
		=> !abs.HasValue || abs.Value > TimeSpan.Zero;

	public static bool IsValidSliding(TimeSpan slide)
		=> slide >= TimeSpan.Zero;
}
