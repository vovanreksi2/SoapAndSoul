namespace SoupAndSoupApp.Models;

public enum DomainNotificationType
{
    RecipeCreated,
    RecipeUpdated,
    RecipeDeleted,

    ComponentCreated,
    ComponentUpdated,
    ComponentDeleted,

    ErrorWhileSaving
}