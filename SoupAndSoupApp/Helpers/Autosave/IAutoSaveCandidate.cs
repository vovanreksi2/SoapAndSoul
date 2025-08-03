using System.Threading;
using System.Threading.Tasks;

namespace SoupAndSoupApp.Helpers.Autosave;

public interface IAutoSaveCandidate
{
    Task<bool> SaveIfNeededAsync(CancellationToken cancellationToken = default);

    bool ShouldSave(CancellationToken cancellationToken = default);
}