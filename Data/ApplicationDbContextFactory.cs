using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
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
                .AddEnvironmentVariables()
                .Build();

            
            string? connectionString;

            //if production, use productionconnection string with mysql, otherwise use development connection string and sqlite
            if (config.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Production")
            {
                connectionString = config.GetConnectionString("DefaultConnection") ??
                                   config.GetConnectionString("ProductionConnection");
            }
            else
            {
                connectionString = config.GetConnectionString("DevelopmentConnection");
            }
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");
            }


            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(11, 7, 2)));

            //give optionsbuilder the connection string from appsettings.json if available
            // if (config.GetConnectionString("DefaultConnection") != null)
            // {
            //     optionsBuilder.UseMySql(config.GetConnectionString("DefaultConnection"));
            // }

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}