namespace url_shortener.Services;

using url_shortener.Settings;
using url_shortener.Data; // Ensure this matches the namespace where ApplicationDbContext is defined
using Microsoft.EntityFrameworkCore;
using url_shortener.Models;
using url_shortener.DTO;

public class UrlShorteningService
{
    private ApplicationDbContext _dbContext;
    private readonly Random _random = new Random();

    public UrlShorteningService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateUniqueCodeAsync()
    {
        var codeChars = new char[ShortLinkSettings.Length];
        const int maxValue = 62;

        while (true)
        {
            for (var i = 0; i < ShortLinkSettings.Length; i++)
            {
                var randomIndex = _random.Next(maxValue);
                codeChars[i] = ShortLinkSettings.Alphabet[randomIndex];
            }
            var code = new string(codeChars);

            if (!await _dbContext.ShortenedUrls.AnyAsync(s => s.Code == code))
            {
                return code;
            }
        }

    }
    

    public async Task<ShortenedUrl> CreateShortenedUrlAsync(CreateShortenedUrlDto shortenedUrlDto)
    {
        
        
        if (string.IsNullOrWhiteSpace(shortenedUrlDto.OriginalUrl))
        {
            throw new ArgumentException("Original URL cannot be null or empty.", nameof(shortenedUrlDto.OriginalUrl));
        }

        if (shortenedUrlDto.ExpirationDate.HasValue && shortenedUrlDto.ExpirationDate <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiration date must be in the future.", nameof(shortenedUrlDto.ExpirationDate));
        }

        if (shortenedUrlDto.ExpirationDate.HasValue && (shortenedUrlDto.ExpirationDate.Value - DateTime.UtcNow).TotalDays > ShortLinkSettings.MaxExpirationDays)
        {
            throw new ArgumentException($"Expiration date cannot be more than {ShortLinkSettings.MaxExpirationDays} days in the future.", nameof(shortenedUrlDto.ExpirationDate));
        }


        var shortenedUrl = new ShortenedUrl(originalUrl: shortenedUrlDto.OriginalUrl,
                                            shortenedUrl: null, // This will be set after generating the code
                                            code: null, // This will be set after generating the code
                                            createdAt: DateTime.UtcNow,
                                            expirationDate: shortenedUrlDto.ExpirationDate ?? DateTime.UtcNow.AddDays(ShortLinkSettings.DefaultExpirationDays));

        shortenedUrl.Code = await GenerateUniqueCodeAsync();
        shortenedUrl.CreatedAt = DateTime.UtcNow;
        shortenedUrl.ClickCount = 0;
        shortenedUrl.IsActive = true;
        shortenedUrl.ShortUrl = System.IO.Path.Combine(ShortLinkSettings.BaseUrl, shortenedUrl.Code);

        await _dbContext.ShortenedUrls.AddAsync(shortenedUrl);
        await _dbContext.SaveChangesAsync();

        return shortenedUrl;
    }

    public async Task<ShortenedUrl?> GetShortenedUrlByCodeAsync(string code)
    {
        var url = await _dbContext.ShortenedUrls
            .FirstOrDefaultAsync(s => s.Code == code && s.IsActive && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow));

        if (url != null)
        {
            url.ClickCount++;
            await _dbContext.SaveChangesAsync();
        }
        return url;
    }

}