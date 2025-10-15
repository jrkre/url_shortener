using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using url_shortener.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Internal;
using Microsoft.AspNetCore.Identity;
using url_shortener.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using System.Text;
using Microsoft.IdentityModel.Logging;
using Pomelo.EntityFrameworkCore.MySql;
using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var environment = builder.Environment.EnvironmentName;
var connectionStringName = environment == "Development" ? "DefaultConnection" : "ProductionConnection";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);

Console.WriteLine($"Using connection string: {connectionString}");

// Configure DbContext with appropriate database provider
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    if (environment == "Development")
    {
        options.UseMySql(connectionString,
            new MySqlServerVersion(new Version(11, 7, 2)),
            mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null

            ));
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

// Add DbContextFactory for concurrent operations
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    if (environment == "Development")
    {
        options.UseMySql(connectionString,
            new MySqlServerVersion(new Version(11, 7, 2)),
            mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            ));
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

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// // Configure Cookie Authentication
// builder.Services.ConfigureApplicationCookie(options =>
// {
//     options.LoginPath = "/api/account/login";
//     options.LogoutPath = "/api/account/logout";
//     options.AccessDeniedPath = "/api/account/access-denied";
//     options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
//     options.SlidingExpiration = true;
//     options.Cookie.HttpOnly = true;
//     options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
//     options.Cookie.SameSite = SameSiteMode.Lax;
//     options.Cookie.Name = "UrlShortenerAuth";

//     // Configure for API responses
//     options.Events.OnRedirectToLogin = context =>
//     {
//         context.Response.StatusCode = 401;
//         return Task.CompletedTask;
//     };
//     options.Events.OnRedirectToAccessDenied = context =>
//     {
//         context.Response.StatusCode = 403;
//         return Task.CompletedTask;
//     };
// });

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/account/login";   // optional: redirect path
        options.LogoutPath = "/api/account/logout";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // important for HTTPS
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = "url_shortener_auth";
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});




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



// app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    Console.WriteLine($"====== Request: {context.Request.Method} {context.Request.Path} ======");

    if (context.User?.Identity != null && context.User.Identity.IsAuthenticated)
    {
        var username = context.User.Identity.Name ?? "(no name claim)";

        Console.WriteLine($"Authenticated user: {username}");
        foreach (var claim in context.User.Claims)
        {
            Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
        }
    }
    else
    {
        Console.WriteLine("Unauthenticated request");
    }

    await next.Invoke();
});

app.MapControllers();

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
