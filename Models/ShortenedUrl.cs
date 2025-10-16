using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace url_shortener.Models;

public class ShortenedUrl
{
    [Key]
    public Guid Id { get; set; }
    public bool IsActive { get; set; } = true; // Optional: to mark if the shortened URL is active or not


    [Required]
    [MaxLength(2048)]
    public string OriginalUrl { get; set; }

    [Required]
    [MaxLength(255)]
    public string ShortUrl { get; set; }

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } // Optional: a unique code for the shortened URL
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int ClickCount { get; set; } // Optional: to track how many times the shortened URL has been clicked

    public string? UserId { get; set; } // Optional: to associate the shortened URL with a user
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; } // Navigation property to the user who created the shortened URL
    // New analytics properties
    public List<ClickEvent> ClickEvents { get; set; } = new List<ClickEvent>();


    public ShortenedUrl()
    {
        OriginalUrl = string.Empty;
        ShortUrl = string.Empty;
        Code = string.Empty;
        CreatedAt = DateTime.UtcNow; // Set the creation date to the current UTC time
        ClickCount = 0; // Initialize click count to zero
        IsActive = true; // Default to active
        ExpirationDate = null; // Default to no expiration date
        UserId = string.Empty; // Default to no user association
        ClickEvents = new List<ClickEvent>();
    }

    public ShortenedUrl(string originalUrl, string? shortenedUrl, string? code, DateTime? createdAt, DateTime? expirationDate = null, bool isActive = true, int? clickCount = 0, string? userId = null)
    {
        OriginalUrl = originalUrl;
        ShortUrl = shortenedUrl ?? string.Empty; // Ensure ShortUrl is not null
        Code = code ?? string.Empty; // Ensure code is not null
        CreatedAt = DateTime.Now;
        ExpirationDate = expirationDate;
        IsActive = isActive;
        ClickCount = 0; // Initialize click count to zero
        UserId = userId ?? string.Empty;; // Default to no user association
    }
}


public class ClickEvent
{
    [Key]
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? Referrer { get; set; }

    public Guid ShortenedUrlId { get; set; }
    [ForeignKey("ShortenedUrlId")]
    public ShortenedUrl ShortenedUrl { get; set; } = null!;
}