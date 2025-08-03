using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Services;

public interface IComponentService
{
    Task<Component?> GetByIdAsync(int id);
    Task<List<Component>> GetAllAsync(int cosmeticType);
    Task<List<Component>> GetAllAsync(bool noTracking = false);
    Task<bool> UpdateAsync(Component entity);
    Task<bool> SoftDeleteAsync(int id);
    Task<Component?> CreateAsync(Component entity);
    Task<bool> DeleteAsync(int id);
}