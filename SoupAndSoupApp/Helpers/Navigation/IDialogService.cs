using System.Threading.Tasks;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers.Navigation;

public interface IDialogService
{
    public Task<NewComponentDto?> ShowAddEditComponentDialogAsync(bool isEditMode, ComponentTypeModel componentType, ComponentModel? component = null);
}