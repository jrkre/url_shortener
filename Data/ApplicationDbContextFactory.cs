using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace url_shortener.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var connectionstring = System.IO.Path.Join(path, "urls.db");
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite($"Data Source={connectionstring}");

            //give optionsbuilder the connection string from appsettings.json if available
            if (config.GetConnectionString("DefaultConnection") != null)
            {
                optionsBuilder.UseSqlite(config.GetConnectionString("DefaultConnection"));
            }

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}