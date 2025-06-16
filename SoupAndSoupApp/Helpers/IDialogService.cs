using System.Collections.Generic;
using System.Threading.Tasks;
using SoupAndSoupApp.Models;
using SoupAndSoupApp.ViewModels;

namespace SoupAndSoupApp.Helpers;

public interface IDialogService
{
    Task<NewIngredientDto?> ShowAddIngredientDialogAsync(SoapTypeComponent type, IEnumerable<MeasureTypeModel> measureTypes);
    Task<NewIngredientDto?> ShowEditIngredientDialogAsync(IngredientModel ingredientModel);
}