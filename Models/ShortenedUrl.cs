using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace url_shortener.Models;

public class ShortenedUrl
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public bool IsActive { get; set; } = true; // Optional: to mark if the shortened URL is active or not
    public string OriginalUrl { get; set; }
    public string ShortUrl { get; set; }
    public string Code { get; set; } // Optional: a unique code for the shortened URL
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int ClickCount { get; set; } // Optional: to track how many times the shortened URL has been clicked

    public ShortenedUrl()
    {
        OriginalUrl = string.Empty;
        ShortUrl = string.Empty;
        Code = string.Empty;
        CreatedAt = DateTime.UtcNow; // Set the creation date to the current UTC time
        ClickCount = 0; // Initialize click count to zero
    }

    public ShortenedUrl(string originalUrl, string? shortenedUrl, string? code, DateTime? createdAt, DateTime? expirationDate = null, bool isActive = true, int? clickCount = 0)
    {
        OriginalUrl = originalUrl;
        ShortUrl = shortenedUrl ?? string.Empty; // Ensure ShortUrl is not null
        Code = code ?? string.Empty; // Ensure code is not null
        CreatedAt = DateTime.Now;
        ExpirationDate = expirationDate;
        IsActive = isActive;
        ClickCount = 0; // Initialize click count to zero
    }
}