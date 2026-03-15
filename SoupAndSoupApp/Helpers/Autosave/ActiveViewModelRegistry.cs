using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SoupAndSoupApp.Helpers.Autosave;

public class ActiveViewModelRegistry : IActiveViewModelRegistry
{
    private readonly ConcurrentDictionary<IAutoSaveCandidate, byte> _activeViewModels = [];

    public void Register(IAutoSaveCandidate viewModel)
    {
        _activeViewModels.TryAdd(viewModel, 0);
    }

    public void UnRegister(IAutoSaveCandidate viewModel)
    {
        _activeViewModels.TryRemove(viewModel, out _);
    }

    public IReadOnlyCollection<IAutoSaveCandidate> GetActiveViewModels()
    {
        return _activeViewModels.Keys.ToList().AsReadOnly();
    }
}