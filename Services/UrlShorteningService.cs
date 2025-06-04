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

    public async Task<ShortenedUrl> CreateShortenedUrlAsync(CreateShortenedUrlDto shortenedUrlDto, string userId)
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

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required for creating shortened URLs.");
        }


        var shortenedUrl = new ShortenedUrl(originalUrl: shortenedUrlDto.OriginalUrl,
                                            shortenedUrl: null, // This will be set after generating the code
                                            code: null, // This will be set after generating the code
                                            createdAt: DateTime.UtcNow,
                                            expirationDate: shortenedUrlDto.ExpirationDate ?? DateTime.UtcNow.AddDays(ShortLinkSettings.DefaultExpirationDays),
                                            userId: userId);

        if (!string.IsNullOrWhiteSpace(shortenedUrlDto.RequestedCode))
        {
            var requestedCode = shortenedUrlDto.RequestedCode.Trim();
            if (!requestedCode.All(c => ShortLinkSettings.Alphabet.Contains(c)))
            {
                throw new ArgumentException($"Requested code must contain only valid characters.", nameof(shortenedUrlDto.RequestedCode));
            }

            if (await _dbContext.ShortenedUrls.AnyAsync(s => s.Code == requestedCode))
            {
                throw new InvalidOperationException("The requested code is already in use.");
            }

            shortenedUrl.Code = requestedCode;
        }
        else
        {
            // Generate a unique code if no specific code was requested
            shortenedUrl.Code = await GenerateUniqueCodeAsync();
        }

        shortenedUrl.CreatedAt = DateTime.UtcNow;
        shortenedUrl.ClickCount = 0;
        shortenedUrl.IsActive = true;
        shortenedUrl.ShortUrl = System.IO.Path.Combine(ShortLinkSettings.BaseUrl, "l", shortenedUrl.Code);

        await _dbContext.ShortenedUrls.AddAsync(shortenedUrl);
        await _dbContext.SaveChangesAsync();

        return shortenedUrl;
    }

    public async Task<ShortenedUrl?> GetShortenedUrlByCodeAsync(string code)
    {
        var url = await _dbContext.ShortenedUrls
            .FirstOrDefaultAsync(s => s.Code == code && s.IsActive && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow));

        if (url == null)
        {
            Console.WriteLine($"No active URL found for code: {code}");
            throw new InvalidOperationException("No active URL found for the provided code.");
        }

        if (url != null)
        {
            url.ClickCount++;
        }

        // Check if the URL is inactive or expired
        // If the URL is inactive or has expired, we will deactivate it and throw an exception
        if (url != null && (!url.IsActive || (url.ExpirationDate.HasValue && url.ExpirationDate <= DateTime.UtcNow)))
        {
            //update the URL to inactive if it has expired
            url.IsActive = false;
            Console.WriteLine($"URL with code {code} is inactive or expired.");
            await _dbContext.SaveChangesAsync();
            // URL is inactive or expired
            throw new InvalidOperationException("This URL is inactive or has expired.");
        }

        // Check if the URL has exceeded the maximum click count
        if (url != null && (url.ClickCount > ShortLinkSettings.MaxClickCount))
        {
            url.IsActive = false; // Deactivate the URL if it exceeds the maximum click count
            Console.WriteLine($"URL with code {code} has exceeded the maximum click count.");
            await _dbContext.SaveChangesAsync();
            // URL is inactive due to exceeding click count
            throw new InvalidOperationException("This URL has exceeded the maximum click count and is no longer active.");
        }

        await _dbContext.SaveChangesAsync();

        return url;
    }

    public async Task<ShortenedUrl?> GetShortenedUrlAnalyticsByCodeAsync(string code)
    {
        var url = await _dbContext.ShortenedUrls
            .FirstOrDefaultAsync(s => s.Code == code && s.IsActive && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow));

        if (url != null && (!url.IsActive || (url.ExpirationDate.HasValue && url.ExpirationDate <= DateTime.UtcNow)))
        {
            //update the URL to inactive if it has expired
            url.IsActive = false;
            Console.WriteLine($"URL with code {code} is inactive or expired.");
            await _dbContext.SaveChangesAsync();
            // URL is inactive or expired
        }

        // Check if the URL has exceeded the maximum click count
        if (url != null && (url.ClickCount > ShortLinkSettings.MaxClickCount))
        {
            url.IsActive = false; // Deactivate the URL if it exceeds the maximum click count
            Console.WriteLine($"URL with code {code} has exceeded the maximum click count.");
            await _dbContext.SaveChangesAsync();
            // URL is inactive due to exceeding click count
        }



        return url;
    }

    public async Task<IEnumerable<ShortenedUrl>> GetAllShortenedUrlsAsync()
    {
        return await _dbContext.ShortenedUrls
            .Where(s => s.IsActive && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow))
            .ToListAsync();
    }
    
    public async Task<IEnumerable<ShortenedUrl>> GetShortenedUrlsByUserAsync(string userId)
    {
        return await _dbContext.ShortenedUrls
            .Where(s => s.UserId == userId && s.IsActive && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow))
            .ToListAsync();
    }

}