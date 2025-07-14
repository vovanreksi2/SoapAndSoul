using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SoupAndSoup.Data
{
    public class SoapAndSoulContextFactory : IDesignTimeDbContextFactory<SoapAndSoulContext>
    {
        public SoapAndSoulContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SoapAndSoulContext>();

            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=SoapAndSoulDb_New2;Trusted_Connection=True;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(connectionString);

            return new SoapAndSoulContext(optionsBuilder.Options);
        }
    }
}
