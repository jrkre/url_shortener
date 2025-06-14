namespace url_shortener.Data;

using Microsoft.EntityFrameworkCore;
using url_shortener.Models;
using url_shortener.Settings;
using System;


public class ApplicationDbContext : DbContext
{
    // public string DbPath { get; }
    public DbSet<ShortenedUrl> ShortenedUrls { get; set; }

    public DbSet<ClickEvent> ClickEvents { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {


    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortenedUrl>(UriBuilder =>
        {
            UriBuilder.HasKey(s => s.Id);
            UriBuilder.Property(ShortenedUrl => ShortenedUrl.OriginalUrl)
                .IsRequired()
                .HasMaxLength(ShortLinkSettings.OriginalUrlLength); // URL length limit

            UriBuilder.HasIndex(shortenedUrl => shortenedUrl.Code)
                .IsUnique();

        });

        modelBuilder.Entity<ShortenedUrl>()
            .HasMany(s => s.ClickEvents)
            .WithOne(c => c.ShortenedUrl)
            .HasForeignKey(c => c.ShortenedUrlId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     if (!optionsBuilder.IsConfigured)
    //     {
    //         optionsBuilder.UseMySql($"Data Source={DbPath}");
    //     }
    // }

}