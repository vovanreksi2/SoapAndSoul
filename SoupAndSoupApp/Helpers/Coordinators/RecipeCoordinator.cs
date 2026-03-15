using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SoupAndSoup.Data.Models;
using SoupAndSoup.Data.Services;
using SoupAndSoupApp.ExternalServices;
using SoupAndSoupApp.Helpers.Images;
using SoupAndSoupApp.Helpers.Mappers;
using SoupAndSoupApp.Helpers.Notifications;
using SoupAndSoupApp.Models;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.Helpers.Coordinators;

public class RecipeCoordinator(
    IRecipeService recipeService,
    IRecipeMapper recipeMapper,
    IImageService imageService,
    IAzureBlobStorageService blobStorageService,
    INotificationService notificationService,
    ILogger<RecipeCoordinator> logger)
{
    public async Task<bool> SavePreviouslySelectedRecipeAsync(
        RecipeModel? oldRecipe, bool isDirty, string newImagePath, CosmeticType cosmeticType, string noImageUrl)
    {
        if (oldRecipe is null)
        {
            logger.LogInformation("Old recipe is null, nothing to save.");
            return true;
        }

        if (!ShouldSaveRecipe(oldRecipe, isDirty)) return true;

        var isSuccessTransaction = await DesignerActivityHelper.ExecuteCompensatingTransaction(async () =>
            {
                await imageService.UpdateRecipeImageAsync(oldRecipe, newImagePath);

                var isSaveSuccess = await AddOrUpdateRecipeAsync(oldRecipe, cosmeticType, noImageUrl);
                if (!isSaveSuccess) return false;

                oldRecipe.IsDirty = false;
                return true;
            },
            async () => await blobStorageService.DeleteBlobAsync(oldRecipe.ImagePathString),
            logger);

        return isSuccessTransaction;
    }

    public bool ShouldSaveRecipe(RecipeModel? recipe, bool isDirty)
    {
        return recipe is not null && (isDirty || recipe.IsDirty);
    }

    public async Task<bool> DeleteRecipeAsync(RecipeModel recipeModel)
    {
        if (!recipeModel.IsNew)
        {
            var isDeleteSuccess = await recipeService.SoftDelete(recipeModel.Id);
            DesignerActivityHelper.NotifyResult(isDeleteSuccess, DomainNotificationType.RecipeDeleted, notificationService);

            if (!isDeleteSuccess)
            {
                logger.LogError("Failed to delete recipe with ID {recipeId}, Name: {recipeName}", recipeModel.Id, recipeModel.Name);
                return false;
            }

            await imageService.DeleteImageAsync(recipeModel);
            logger.LogInformation("Success for delete recipe with ID {recipeId}, Name: {recipeName}", recipeModel.Id, recipeModel.Name);
        }

        return true;
    }

    public Task<RecipeModel> MapFromEntityAsync(
        Recipe recipe, System.Windows.Input.ICommand deleteRecipeCommand,
        DynamicData.SourceCache<ComponentModel, int> cachedComponents, string noImageUrl)
    {
        return recipeMapper.MapToModelAsync(recipe, deleteRecipeCommand, cachedComponents, noImageUrl);
    }

    public Recipe MapToEntity(RecipeModel recipe, CosmeticType cosmeticType, string noImageUrl)
    {
        return recipeMapper.MapToEntity(recipe, cosmeticType, noImageUrl);
    }

    private async Task<bool> AddOrUpdateRecipeAsync(RecipeModel inputRecipe, CosmeticType cosmeticType, string noImageUrl)
    {
        var recipe = recipeMapper.MapToEntity(inputRecipe, cosmeticType, noImageUrl);

        if (inputRecipe.IsNew)
        {
            var saveResult = await PersistRecipeAsync(
                async () => { var r = await recipeService.CreateAsync(recipe); return r is not null && r.Id > 0; },
                "Failed to create new recipe with NAME {recipeName}", recipe.Name);
            DesignerActivityHelper.NotifyResult(saveResult, DomainNotificationType.RecipeCreated, notificationService);
            return saveResult;
        }

        var updateResult = await PersistRecipeAsync(
            () => recipeService.UpdateAsync(recipe),
            "Failed to update recipe with NAME {recipeName} and ID {recipeId}", recipe.Name, recipe.Id);
        DesignerActivityHelper.NotifyResult(updateResult, DomainNotificationType.RecipeUpdated, notificationService);
        logger.LogInformation("Success for save recipe with ID {recipeId}", inputRecipe.Id);

        return updateResult;
    }

    private async Task<bool> PersistRecipeAsync(Func<Task<bool>> operation, string errorTemplate, params object[] args)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, errorTemplate, args);
            return false;
        }
    }
}
