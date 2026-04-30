namespace BreakfastProvider.Api.Configuration;

public sealed class ProgramSettings
{
    private string _instanceId = "";

    [ConfigurationKeyName("WEBSITE_SITE_NAME")]
    public string ResourceId { get; init; } = "";

    [ConfigurationKeyName("WEBSITE_INSTANCE_ID")]
    public string InstanceId
    {
        get => _instanceId ??= "Local-" + Environment.UserName;
        init => _instanceId = value;
    }
}
