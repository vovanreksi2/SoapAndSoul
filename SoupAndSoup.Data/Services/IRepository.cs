public interface IRepository<T> where T : class
{
    Task<T?> CreateAsync(T entity);
    Task<T> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync(bool noTracking = false);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
}