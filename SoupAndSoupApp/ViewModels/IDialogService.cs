using System.Collections.Generic;
using System.Threading.Tasks;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.ViewModels;

public interface IDialogService
{
    Task<NewIngredientDto?> ShowAddIngredientDialogAsync(SoapTypeComponent type, IEnumerable<MeasureTypeModel> measureTypes);
    Task<NewIngredientDto?> ShowEditIngredientDialogAsync(IngredientModel ingredientModel);
}