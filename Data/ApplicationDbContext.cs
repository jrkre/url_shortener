namespace url_shortener.Data;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;  // Added for Identity
using Microsoft.AspNetCore.Identity;  // Added for Identity
using Microsoft.EntityFrameworkCore;
using url_shortener.Models;
using url_shortener.Settings;
using System;


public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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
    base.OnModelCreating(modelBuilder);  // Added to configure Identity tables

        modelBuilder.Entity<ShortenedUrl>(UriBuilder =>
        {
            UriBuilder.HasKey(s => s.Id);
            UriBuilder.Property(ShortenedUrl => ShortenedUrl.OriginalUrl)
                .IsRequired()
                .HasMaxLength(ShortLinkSettings.OriginalUrlLength);

            UriBuilder.HasIndex(shortenedUrl => shortenedUrl.Code)
                .IsUnique();

            // Added: Configure relationship with ApplicationUser
            UriBuilder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);  // Optional: Cascade delete if user is removed
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