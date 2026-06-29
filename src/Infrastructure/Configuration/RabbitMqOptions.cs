namespace Infrastructure.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";

    public ushort Port { get; init; } = 5672;

    public string Username { get; init; } = "ata";

    public string Password { get; init; } = "ata_dev_password";

    public string VirtualHost { get; init; } = "/";
}
