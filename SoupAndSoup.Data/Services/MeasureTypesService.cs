using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Services;

public class MeasureTypesService : RepositoryBase<MeasureType>, IMeasureTypesService
{
    public MeasureTypesService(IDbContextFactory<SoapAndSoulContext> contextFactory) : base(contextFactory) { }
}