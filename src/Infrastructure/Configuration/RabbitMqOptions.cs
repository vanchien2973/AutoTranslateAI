namespace Infrastructure.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";

    public ushort Port { get; init; } = 5672;

    public string Username { get; init; } = "ata";

    public string Password { get; init; } = "ata_dev_password";

    public string VirtualHost { get; init; } = "/";

    public int RetryLimit { get; init; } = 3;

    public int RetryInitialIntervalSeconds { get; init; } = 5;

    public int RetryIntervalIncrementSeconds { get; init; } = 15;

    public int ConcurrencyLimit { get; init; } = 1;
}
