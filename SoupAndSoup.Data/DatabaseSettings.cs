namespace SoupAndSoup.Data;

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool IsAzureDb { get; set; } 
}