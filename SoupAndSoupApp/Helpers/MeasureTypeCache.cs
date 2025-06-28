using System;
using System.Threading.Tasks;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers;

public class MeasureTypeCache: MemoryCache<MeasureTypeModel>
{
    private readonly MeasureTypesService _measureTypesService;

    public MeasureTypeCache(MeasureTypesService measureTypesService)
    {
        _measureTypesService = measureTypesService;
    }
 
    public Task<MeasureTypeModel> GetOrAddAsync(int id)
    {
        return GetOrAddAsync(id.ToString(), async () =>
        {
            var result = await _measureTypesService.GetByIdAsync(id);
            if (result == null)
                throw new ArgumentException($"UseMeasureType with id {id} not found.");

            return new MeasureTypeModel(result);
        });
    }
}