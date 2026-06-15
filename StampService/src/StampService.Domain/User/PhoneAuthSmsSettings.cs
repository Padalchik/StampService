namespace StampService.Domain.User;

public class PhoneAuthSmsSettings
{
    public const int SingletonId = 1;

    public int Id { get; private set; }
    public bool IsEnabled { get; private set; }

    private PhoneAuthSmsSettings(bool isEnabled)
    {
        Id = SingletonId;
        IsEnabled = isEnabled;
    }

    protected PhoneAuthSmsSettings()
    {
    }

    public static PhoneAuthSmsSettings Create(bool isEnabled)
    {
        return new PhoneAuthSmsSettings(isEnabled);
    }

    public void Update(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}
