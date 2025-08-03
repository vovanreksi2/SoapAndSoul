public interface IRepository<T> where T : class
{
    public Task<T?> CreateAsync(T entity);
    public Task<T> GetByIdAsync(int id);
    public Task<List<T>> GetAllAsync(bool noTracking = false);
    public Task<bool> UpdateAsync(T entity);
    public Task<bool> DeleteAsync(int id);
}