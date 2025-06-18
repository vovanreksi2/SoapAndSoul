using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data;

public abstract class RepositoryBase<T> : IRepository<T> where T : class
{
    protected readonly SoapAndSoulContext _context;
    protected readonly DbSet<T> _dbSet;

    public RepositoryBase(SoapAndSoulContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));



        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}