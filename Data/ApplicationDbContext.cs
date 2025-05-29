namespace url_shortener.Data;

using Microsoft.EntityFrameworkCore;
using url_shortener.Models;
using url_shortener.Settings;
using System;


public class ApplicationDbContext : DbContext
{
    public string DbPath { get; }
    public DbSet<ShortenedUrl> ShortenedUrls { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "urls.db");

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortenedUrl>(UriBuilder =>
        {
            UriBuilder.HasKey(s => s.Id);
            UriBuilder.Property(ShortenedUrl => ShortenedUrl.OriginalUrl)
                .IsRequired()
                .HasMaxLength(ShortLinkSettings.Length); // URL length limit

            UriBuilder.HasIndex(shortenedUrl => shortenedUrl.Code)
                .IsUnique();

        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }
    }

}