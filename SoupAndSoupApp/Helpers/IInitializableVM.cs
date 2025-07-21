using System.Threading.Tasks;

namespace SoupAndSoupApp.Helpers;

public interface IInitializableVM
{
    public Task InitializeAsync();
}