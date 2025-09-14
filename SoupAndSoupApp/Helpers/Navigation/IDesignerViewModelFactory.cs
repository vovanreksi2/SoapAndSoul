using SoupAndSoupApp.Models;
using SoupAndSoupApp.ViewModels;

namespace SoupAndSoupApp.Helpers.Navigation;

public interface IDesignerViewModelFactory
{
    SoapDesignerViewModel CreateDesignerViewModel(CosmeticType type);
}