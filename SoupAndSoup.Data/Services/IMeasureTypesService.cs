using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Services;

public interface IMeasureTypesService
{
    Task<MeasureType?> CreateAsync(MeasureType entity);
    Task<MeasureType?> GetByIdAsync(int id);
    Task<List<MeasureType>> GetAllAsync(bool noTracking = false);
    Task<bool> UpdateAsync(MeasureType entity);
    Task<bool> DeleteAsync(int id);
}