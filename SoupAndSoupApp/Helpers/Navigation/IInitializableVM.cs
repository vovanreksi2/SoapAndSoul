using System.Threading.Tasks;

namespace SoupAndSoupApp.Helpers.Navigation;

public interface IInitializableVM
{
    public Task InitializeAsync();
}