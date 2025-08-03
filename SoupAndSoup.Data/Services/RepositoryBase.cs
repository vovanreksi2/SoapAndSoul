using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data;

public abstract class RepositoryBase<T> : IRepository<T> where T : class
{
    private readonly IDbContextFactory<SoapAndSoulContext> _contextFactory;

    protected RepositoryBase(IDbContextFactory<SoapAndSoulContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }


    public virtual Task<T?> CreateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        return UseContextAsync(async (context, dbSet) =>
        {
            await dbSet.AddAsync(entity);
            var result = await context.SaveChangesAsync();

            return result > 0 ? entity : null;
        });
    }

    public virtual Task<T?> GetByIdAsync(int id)
    {
        return UseContextAsync(async (context, dbSet) => await dbSet.FindAsync(id));
    }

    public virtual Task<List<T>> GetAllAsync(bool noTracking = false)
    {
        return UseContextAsync((context, dbSet) => noTracking
            ? dbSet.AsNoTracking().ToListAsync()
            : dbSet.ToListAsync());
    }

    public virtual Task<bool> UpdateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        return UseContextAsync(async (context, dbSet) =>
        {
            dbSet.Update(entity);

            var result = await context.SaveChangesAsync();
            return result > 0;
        });
    }

    public virtual Task<bool> DeleteAsync(int id)
    {
        return UseContextAsync(async (context, dbSet) =>
        {
            var entity = await dbSet.FindAsync(id);
            if (entity == null) return false;

            dbSet.Remove(entity);
            var result = await context.SaveChangesAsync();
            return result > 0;
        });
    }

    protected async Task<TResult> UseContextAsync<TResult>(Func<SoapAndSoulContext, DbSet<T>, Task<TResult>> func)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));

        await using var context = await _contextFactory.CreateDbContextAsync();
        if (context == null) throw new InvalidOperationException("Failed to create a database context.");

        var dbSet = context.Set<T>();
        return await func(context, dbSet);
    }
}