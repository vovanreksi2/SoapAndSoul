using System.Collections.Generic;
using System.Threading.Tasks;
using SoupAndSoupApp.Models;
using SoupAndSoupApp.ViewModels;

namespace SoupAndSoupApp.Helpers;

public interface IDialogService
{
    public Task<NewComponentDto> ShowAddEditComponentDialogAsync(bool isEditMode, ComponentTypeModel componentType, ComponentModel? component = null);
}