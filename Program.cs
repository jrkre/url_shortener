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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();


// Added: Configure Identity and Authentication
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");

// Configure JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // Reduce clock skew to avoid timing issues
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                Console.WriteLine($"Token extracted: {token.Substring(0, Math.Min(20, token.Length))}...");
                context.Token = token;
            }
            else
            {
                Console.WriteLine($"No valid Bearer token found in Authorization header: {authHeader}");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            IdentityModelEventSource.ShowPII = true;
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            Console.WriteLine($"Token: {context.Request.Headers["Authorization"].FirstOrDefault()}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"Authentication challenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

string? connectionString;

//if production, use productionconnection string with mysql, otherwise use development connection string and sqlite
if (builder.Environment.IsProduction())
{
    connectionString = builder.Configuration.GetConnectionString("ProductionConnection");
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DevelopmentConnection");
}

Console.WriteLine($"Using connection string: {connectionString}");

// Configure Entity Framework Core with MySQL if production, otherwise use SQLite
if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString,
            new MySqlServerVersion(new Version(11, 7, 2)),
            mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            )
        ));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}


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
    var authHeader = context.Request.Headers["Authorization"].ToString();
    Console.WriteLine($"Authorization Header: {authHeader}");
    await next.Invoke();
});

// app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

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
