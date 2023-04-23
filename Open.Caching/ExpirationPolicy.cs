namespace Open.Caching;

/// <summary>
/// A universal expiration policy that can be used for both absolute and sliding expiration.
/// </summary>
public readonly record struct ExpirationPolicy
{
	private const string CannotBeNegative = "Cannot be a negative value";

	/// <summary>
	/// The absolute expiration time.
	/// </summary>
	public TimeSpan Absolute { get; }

	/// <summary>
	/// The sliding expiration time.
	/// </summary>
	public TimeSpan Sliding { get; }

	/// <summary>
	/// Indicates whether the expiration policy has an absolute expiration time.
	/// </summary>
	public bool HasAbsolute => Absolute != TimeSpan.Zero;

	/// <summary>
	/// Indicates whether the expiration policy has a sliding expiration time.
	/// </summary>
	public bool HasSliding => Sliding != TimeSpan.Zero;

	/// <summary>
	/// The absolute expiration time relative to now.
	/// </summary>
	public DateTimeOffset AbsoluteRelativeToNow
		=> Absolute == TimeSpan.Zero
		? DateTimeOffset.MaxValue
		: DateTimeOffset.Now + Absolute;

	/// <summary>
	/// Constructs a new expiration policy.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If either of the <paramref name="absolute"/> or <paramref name="sliding"/> parameters are negative.</exception>
	public ExpirationPolicy(TimeSpan absolute, TimeSpan sliding)
	{
		if (absolute < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(absolute), absolute, CannotBeNegative);
		if (sliding < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(sliding), sliding, CannotBeNegative);

		Absolute = absolute;
		Sliding = sliding;
	}

	/// <summary>
	/// Returns an <see cref="ExpirationPolicy"/> with the same sliding expiration time and the specified absolute expiration time.
	/// </summary>
	public ExpirationPolicy After(TimeSpan value) => new(value, Sliding);

	/// <summary>
	/// Returns an <see cref="ExpirationPolicy"/> with the same absolute expiration time and the specified sliding expiration time.
	/// </summary>
	public ExpirationPolicy Slide(TimeSpan value) => new(Absolute, value);
}

/// <summary>
/// Shortcuts for creating the desired <see cref="ExpirationPolicy"/>.
/// </summary>
public static class Expire
{
	/// <summary>
	/// An expiration policy that never expires.
	/// </summary>
	public static readonly ExpirationPolicy Never = new(TimeSpan.Zero, TimeSpan.Zero);

	/// <summary>
	/// Creates a new <see cref="ExpirationPolicy"/> with the specified absolute and sliding expiration times.
	/// </summary>
	public static ExpirationPolicy Policy(TimeSpan? absolute, TimeSpan? sliding)
		=> new(absolute ?? TimeSpan.Zero, sliding ?? TimeSpan.Zero);

	/// <summary>
	/// Creates a new <see cref="ExpirationPolicy"/> with the specified absolute expiration time.
	/// </summary>
	public static ExpirationPolicy Absolute(TimeSpan value)
		=> new(value, TimeSpan.Zero);

	/// <summary>
	/// Creates a new <see cref="ExpirationPolicy"/> with the specified sliding expiration time.
	/// </summary>
	public static ExpirationPolicy Sliding(TimeSpan value)
		=> new(TimeSpan.Zero, value);

	/// <summary>
	/// Returns <see langword="true"/> if the specified <paramref name="value"/> is valid for use in an <see cref="ExpirationPolicy"/>.
	/// </summary>
	public static bool IsValid(TimeSpan? value)
		=> !value.HasValue || value.Value >= TimeSpan.Zero;
}
