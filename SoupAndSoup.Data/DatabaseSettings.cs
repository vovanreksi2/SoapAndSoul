namespace SoupAndSoup.Data;

public class DatabaseSettings
{
    public string ConnectionString { get; init; } = string.Empty;
    public bool EnableSensitiveDataLogging { get; init; }
    public bool IsAzureDb { get; init; }
}