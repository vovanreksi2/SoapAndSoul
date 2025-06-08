using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace SoupAndSoup.Data
{
    public class SoapAndSoulContextFactory : IDesignTimeDbContextFactory<SoapAndSoulContext>
    {
        public SoapAndSoulContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SoapAndSoulContext>();

            // ❗ Обов’язково вкажи свій правильний рядок з'єднання
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=SoapAndSoulDb;Trusted_Connection=True;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(connectionString);

            return new SoapAndSoulContext(optionsBuilder.Options);
        }
    }
}
