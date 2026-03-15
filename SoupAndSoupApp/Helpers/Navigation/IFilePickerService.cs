using System.Threading.Tasks;

namespace SoupAndSoupApp.Helpers.Navigation;

public interface IFilePickerService
{
    Task<string?> PickImageAsync();
}
