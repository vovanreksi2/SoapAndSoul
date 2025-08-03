using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Services;

public interface IComponentTypeService
{
    Task<List<ComponentType>> GetAllAsync(int cosmeticType);
    Task<List<ComponentType>> GetAllAsync(bool noTracking = false);
    Task<ComponentType?> CreateAsync(ComponentType entity);
    Task<ComponentType?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(ComponentType entity);
    Task<bool> DeleteAsync(int id);
}