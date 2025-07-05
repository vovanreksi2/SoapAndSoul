using System.Threading;
using System.Threading.Tasks;

namespace SoupAndSoupApp.Helpers;

public interface IAutoSaveCandidate
{
    Task<bool> SaveIfNeededAsync(CancellationToken cancellationToken = default);

    bool ShouldSave(CancellationToken cancellationToken = default);
}