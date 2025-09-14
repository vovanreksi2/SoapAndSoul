using System.Collections.Generic;

namespace SoupAndSoupApp.Helpers.Autosave;

public interface IActiveViewModelRegistry
{
    void Register(IAutoSaveCandidate viewModel);
    void UnRegister(IAutoSaveCandidate viewModel);
    IReadOnlyCollection<IAutoSaveCandidate> GetActiveViewModels();
}