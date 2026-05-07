namespace StampService.ApplicationTests.Fakes;

public class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    public void SetUtcNow(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }
}
