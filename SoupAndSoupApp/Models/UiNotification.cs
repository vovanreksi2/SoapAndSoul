namespace SoupAndSoupApp.Models;

public class UiNotification
{
    public string Message { get; }
    public NotificationLevel Level { get; }

    public UiNotification(string message, NotificationLevel level)
    {
        Message = message;
        Level = level;
    }
}