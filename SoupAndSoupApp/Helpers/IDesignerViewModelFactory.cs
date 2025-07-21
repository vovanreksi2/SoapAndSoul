using SoupAndSoupApp.Models;
using SoupAndSoupApp.ViewModels;

namespace SoupAndSoupApp.Helpers;

public interface IDesignerViewModelFactory
{
    SoapDesignerViewModel CreateDesignerViewModel(CosmeticType type);
}