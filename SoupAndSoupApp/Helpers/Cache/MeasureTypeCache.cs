using System;
using System.Threading.Tasks;
using SoupAndSoup.Data.Services;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers.Cache;

public class MeasureTypeCache(IMeasureTypesService measureTypesService) : MemoryCache<MeasureTypeModel>
{
    public Task<MeasureTypeModel> GetOrAddAsync(int id)
    {
        return GetOrAddAsync(id.ToString(), async () =>
        {
            var result = await measureTypesService.GetByIdAsync(id);
            if (result is null)
                throw new ArgumentException($"UseMeasureType with id {id} not found.");

            return new MeasureTypeModel(result);
        });
    }
}
