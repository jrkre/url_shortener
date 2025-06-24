using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using url_shortener.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var environment = builder.Environment.EnvironmentName;
var connectionStringName = environment == "Development" ? "DevelopmentConnection" : "ProductionConnection";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);

// Configure DbContext with appropriate database provider
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (environment == "Development")
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseMySql(connectionString,
            new MySqlServerVersion(new Version(11, 7, 2)),
            mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            ));
    }
});

//grab login information from environment variables if available

// var dbUser = Environment.GetEnvironmentVariable("DB_USER");
// var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

// if (!string.IsNullOrEmpty(dbUser) && !string.IsNullOrEmpty(dbPassword))
// {
//     connectionString += $"user={dbUser};password={dbPassword};";
// }

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(11, 7, 2)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        ) // Adjust the version as needed
    ));


builder.Services.AddScoped<url_shortener.Services.UrlShorteningService>();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/var/dpkeys"))
    .SetApplicationName("url_shortener");


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//     db.Database.Migrate(); // runs migrations at app startup
// }


app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
});

// app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();
// app.UseDefaultFiles();

var reactBuildPath = Path.Combine(Directory.GetCurrentDirectory(), "ClientApp", "build");

if (Directory.Exists(reactBuildPath))
{

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(reactBuildPath),
        RequestPath = ""
    });

    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(reactBuildPath),
    });
}
else
{
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
    Console.WriteLine("React build path does not exist. Serving static files from default location.");
}


// app.MapControllerRoute(
//     name: "default",
//     pattern: "api/{controller}/{action}");


app.Run();
