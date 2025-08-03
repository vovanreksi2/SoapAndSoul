using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers.Notifications;

public static class UiNotificationExtensions
{
    public static UiNotification ToUiNotification(this DomainNotificationType type)
    {
        return type switch
        {
            DomainNotificationType.RecipeCreated => new UiNotification("Рецепт створено", NotificationLevel.Success),
            DomainNotificationType.RecipeUpdated => new UiNotification("Рецепт оновлено", NotificationLevel.Success),
            DomainNotificationType.RecipeDeleted => new UiNotification("Рецепт видалено", NotificationLevel.Warning),

            DomainNotificationType.ComponentCreated => new UiNotification("Компонент створено", NotificationLevel.Success),
            DomainNotificationType.ComponentUpdated => new UiNotification("Компонент оновлено", NotificationLevel.Success),
            DomainNotificationType.ComponentDeleted => new UiNotification("Компонент видалено", NotificationLevel.Warning),

            DomainNotificationType.ErrorWhileSaving => new UiNotification("Помилка при збереженні", NotificationLevel.Error),
            DomainNotificationType.ErrorDuringInit => new UiNotification("Помилка при ініціалізації", NotificationLevel.Error),
            _ => new UiNotification("Невідома дія", NotificationLevel.Warning),
        };
    }
}