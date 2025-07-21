using System.Collections.Generic;

namespace SoupAndSoupApp.Helpers;

public interface IActiveViewModelRegistry
{
    void Register(IAutoSaveCandidate viewModel);
    void UnRegister(IAutoSaveCandidate viewModel);
    IReadOnlyCollection<IAutoSaveCandidate> GetActiveViewModels();
}