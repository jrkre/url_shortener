namespace url_shortener.Services;

using url_shortener.Settings;
using url_shortener.Data; // Ensure this matches the namespace where ApplicationDbContext is defined
using Microsoft.EntityFrameworkCore;
using url_shortener.Models;
using url_shortener.DTO;
using System.Collections.Concurrent;

public class UrlShorteningService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ConcurrentQueue<string> _codePool = new();
    private readonly SemaphoreSlim _poolSemaphore = new(1, 1);
    private const int MinPoolSize = 100;
    private const int MaxPoolSize = 500;

    public UrlShorteningService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        // Initialize pool asynchronously in background
        _ = Task.Run(InitializeCodePoolAsync);
    }

    private async Task InitializeCodePoolAsync()
    {
        await _poolSemaphore.WaitAsync();
        try
        {
            await ReplenishCodePoolAsync();
        }
        finally
        {
            _poolSemaphore.Release();
        }
    }

    private async Task ReplenishCodePoolAsync()
    {
        var currentCount = _codePool.Count;
        if (currentCount >= MinPoolSize) return;

        var codesToGenerate = MaxPoolSize - currentCount;
        var generatedCodes = new HashSet<string>();

        // Generate codes in batches
        while (generatedCodes.Count < codesToGenerate)
        {
            var batchSize = Math.Min(50, codesToGenerate - generatedCodes.Count);
            var batch = GenerateCodeBatch(batchSize);

            // Check batch against database in single query using separate context
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var existingCodes = new HashSet<string>(await dbContext.ShortenedUrls
                    .Where(s => batch.Contains(s.Code))
                    .Select(s => s.Code)
                    .ToListAsync());

            // Add only non-existing codes
            foreach (var code in batch.Where(c => !existingCodes.Contains(c)))
            {
                generatedCodes.Add(code);
            }
        }

        // Add to pool
        foreach (var code in generatedCodes)
        {
            _codePool.Enqueue(code);
        }
    }

    private HashSet<string> GenerateCodeBatch(int count)
    {
        var codes = new HashSet<string>();
        var codeChars = new char[ShortLinkSettings.Length];

        while (codes.Count < count)
        {
            for (var i = 0; i < ShortLinkSettings.Length; i++)
            {
                var randomIndex = Random.Shared.Next(62);
                codeChars[i] = ShortLinkSettings.Alphabet[randomIndex];
            }
            codes.Add(new string(codeChars));
        }

        return codes;
    }

    public async Task<string> GenerateUniqueCodeAsync()
    {
        // Try to get from pool first
        if (_codePool.TryDequeue(out var pooledCode))
        {
            // Replenish pool if running low (fire and forget)
            if (_codePool.Count < MinPoolSize)
            {
                _ = Task.Run(async () =>
                {
                    await _poolSemaphore.WaitAsync();
                    try
                    {
                        await ReplenishCodePoolAsync();
                    }
                    finally
                    {
                        _poolSemaphore.Release();
                    }
                });
            }
            return pooledCode;
        }

        // Fallback to direct generation if pool is empty
        return await GenerateCodeDirectAsync();

    }

    private async Task<string> GenerateCodeDirectAsync()
    {
        var codeChars = new char[ShortLinkSettings.Length];
        var attempts = 0;
        const int maxAttempts = 10;

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        while (attempts < maxAttempts)
        {
            for (var i = 0; i < ShortLinkSettings.Length; i++)
            {
                var randomIndex = Random.Shared.Next(62);
                codeChars[i] = ShortLinkSettings.Alphabet[randomIndex];
            }
            var code = new string(codeChars);

            if (!await dbContext.ShortenedUrls.AnyAsync(s => s.Code == code))
            {
                return code;
            }
            attempts++;
        }

        throw new InvalidOperationException("Unable to generate unique code after maximum attempts.");
    }

    public async Task<ShortenedUrl> CreateShortenedUrlAsync(CreateShortenedUrlDto shortenedUrlDto, string userId)
    {
        // Input validation (unchanged for correctness)
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

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var shortenedUrl = new ShortenedUrl(
            originalUrl: shortenedUrlDto.OriginalUrl,
            shortenedUrl: null,
            code: null,
            createdAt: now,
            expirationDate: shortenedUrlDto.ExpirationDate ?? now.AddDays(ShortLinkSettings.DefaultExpirationDays), 
            userId: userId)
        {
            CreatedAt = now,
            ClickCount = 0,
            IsActive = true
        };

        // Handle custom code request
        if (!string.IsNullOrWhiteSpace(shortenedUrlDto.RequestedCode))
        {
            var requestedCode = shortenedUrlDto.RequestedCode.Trim();
            if (!requestedCode.All(c => ShortLinkSettings.Alphabet.Contains(c)))
            {
                throw new ArgumentException("Requested code must contain only valid characters.", nameof(shortenedUrlDto.RequestedCode));
            }

            // Single query to check if code exists and is active
            var existingActiveUrl = await dbContext.ShortenedUrls
                .FirstOrDefaultAsync(s => s.Code == requestedCode &&
                                         s.IsActive &&
                                         (s.ExpirationDate == null || s.ExpirationDate > now));

            if (existingActiveUrl != null)
            {
                throw new InvalidOperationException("The requested code is already in use.");
            }

            shortenedUrl.Code = requestedCode;
        }
        else
        {
            shortenedUrl.Code = await GenerateUniqueCodeAsync();
        }

        shortenedUrl.ShortUrl = System.IO.Path.Combine(ShortLinkSettings.BaseUrl, "l", shortenedUrl.Code);

        await dbContext.ShortenedUrls.AddAsync(shortenedUrl);
        await dbContext.SaveChangesAsync();

        return shortenedUrl;
    }

    private void ValidateCreateShortenedUrlDto(CreateShortenedUrlDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.OriginalUrl))
        {
            throw new ArgumentException("Original URL cannot be null or empty.", nameof(dto.OriginalUrl));
        }

        if (dto.ExpirationDate.HasValue)
        {
            if (dto.ExpirationDate <= DateTime.UtcNow)
            {
                throw new ArgumentException("Expiration date must be in the future.", nameof(dto.ExpirationDate));
            }

            if ((dto.ExpirationDate.Value - DateTime.UtcNow).TotalDays > ShortLinkSettings.MaxExpirationDays)
            {
                throw new ArgumentException($"Expiration date cannot be more than {ShortLinkSettings.MaxExpirationDays} days in the future.", nameof(dto.ExpirationDate));
            }
        }
    }

    private void ValidateRequestedCode(string code)
    {
        if (!code.All(c => ShortLinkSettings.Alphabet.Contains(c)))
        {
            throw new ArgumentException("Requested code must contain only valid characters.", nameof(code));
        }
    }

    private async Task ValidateUrlStatusAsync(ShortenedUrl url, string code, ApplicationDbContext dbContext)
    {
        bool needsUpdate = false;

        if (!url.IsActive || (url.ExpirationDate.HasValue && url.ExpirationDate <= DateTime.UtcNow))
        {
            url.IsActive = false;
            needsUpdate = true;
            throw new InvalidOperationException("This URL is inactive or has expired.");
        }

        if (url.ClickCount > ShortLinkSettings.MaxClickCount)
        {
            url.IsActive = false;
            needsUpdate = true;
            throw new InvalidOperationException("This URL has exceeded the maximum click count and is no longer active.");
        }

        if (needsUpdate)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<ShortenedUrl?> GetShortenedUrlByCodeAsync(string code)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var url = await dbContext.ShortenedUrls
            .AsNoTracking() // Track changes for updates
            .FirstOrDefaultAsync(s => s.Code == code && s.IsActive && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow));

        if (url == null)
        {
            throw new InvalidOperationException("No active URL found for the provided code.");
        }

        // url.ClickCount++;
        await ValidateUrlStatusAsync(url, code, dbContext);
        await dbContext.SaveChangesAsync();

        return url;
    }

    public async Task<ShortenedUrl?> GetShortenedUrlAnalyticsByCodeAsync(string code)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var url = await dbContext.ShortenedUrls
            .AsNoTracking() // Optimize for read-only query
            .Include(s => s.ClickEvents)
            .FirstOrDefaultAsync(s => s.Code == code);

        if (url != null)
        {
            await ValidateUrlStatusAsync(url, code, dbContext);
        }

        return url;
    }

    public async Task<ShortenedUrl?> TrackClickAndGetUrlAsync(string code, string? userAgent, string? ipAddress, string? referrer)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var url = await dbContext.ShortenedUrls
            .Include(s => s.ClickEvents)
            .FirstOrDefaultAsync(s => s.Code == code && s.IsActive && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow));

        if (url == null)
        {
            throw new InvalidOperationException("No active URL found for the provided code.");
        }

        url.ClickCount++;
        url.ClickEvents.Add(new ClickEvent
        {
            Timestamp = DateTime.UtcNow,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            Referrer = referrer,
            ShortenedUrlId = url.Id
        });

        await ValidateUrlStatusAsync(url, code, dbContext);
        await dbContext.SaveChangesAsync();

        return url;
    }

    public async Task<IEnumerable<ShortenedUrl>> GetAllShortenedUrlsAsync(string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return await _dbContext.ShortenedUrls
                .Where(s => s.UserId == userId && s.IsActive && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow))
                .ToListAsync();
        }
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

    private async Task<string> GenerateKeywordSuffix(string keyword)
    {
        // Validate keyword length
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Length > ShortLinkSettings.Length)
        {
            throw new ArgumentException($"Keyword must be between 1 and {ShortLinkSettings.Length} characters long.", nameof(keyword));
        }

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        if (keyword.Length == ShortLinkSettings.Length)
        {
            // If the keyword is already the full length, return it directly
            if (!await dbContext.ShortenedUrls.AnyAsync(s => s.Code == keyword))
            {
                return keyword;
            }
            else
            {
                throw new InvalidOperationException("The provided keyword is already in use.");
            }
        }
        var suggestedCodes = new HashSet<string>();
        //generate random valid codes based on the keyword
        var codeChars = new char[ShortLinkSettings.Length - keyword.Length];
        for (var i = 0; i < codeChars.Length; i++)
        {
            var randomIndex = Random.Shared.Next(ShortLinkSettings.Alphabet.Length);
            codeChars[i] = ShortLinkSettings.Alphabet[randomIndex];
        }
        var code = keyword + new string(codeChars);
        // Combine base code with random suffix
        if (!await dbContext.ShortenedUrls.AnyAsync(s => s.Code == code))
        {
            return code;
        }
        else
        {
            // If the code already exists, generate a new one
            return await GenerateKeywordSuffix(keyword);
        }
    }

    public async Task<SuggestedCodesDto> GenerateSuggestedCodes(int count, CreateShortenedUrlDto originalUrl)

    {

        if (count <= 0 || count > 20)
        {
            throw new ArgumentException("Count must be between 1 and 20.", nameof(count));
        }

        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        //generate codes based on keywords in the original URL, as well as the requested code if provided
        var suggestedCodes = new HashSet<string>();

        var baseCode = originalUrl.RequestedCode?.Trim() ?? string.Empty;
        var keywords = new List<string>();

        // Common web prefixes and TLDs to exclude
        var excludedTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "www", "www1", "www2", "www3",
            "com", "org", "net", "edu", "gov", "mil", "int",
            "co", "uk", "ca", "au", "de", "fr", "jp", "cn", "in", "br",
            "io", "me", "tv", "cc", "ly", "to", "it", "es", "nl", "be",
            "info", "biz", "name", "mobi", "pro", "travel", "museum",
            "aero", "coop", "jobs", "post", "tel", "xxx", "asia",
            "cat", "ftp", "mail", "email", "blog", "shop", "store",
            "online", "site", "website", "web", "tech", "app", "dev"
        };

        if (string.IsNullOrWhiteSpace(baseCode))
        {
            // use keywords from the original URL
            if (!string.IsNullOrWhiteSpace(originalUrl.OriginalUrl))
            {
                try
                {
                    var uri = new Uri(originalUrl.OriginalUrl);
                    keywords = uri.Host.Split('.')
                        .SelectMany(part => part.Split('-'))
                        .Where(part => !string.IsNullOrWhiteSpace(part))
                        .Where(part => !excludedTerms.Contains(part)) // Exclude common web terms
                        .Where(part => part.Length >= 2) // Exclude single characters
                        .Select(part => part.Substring(0, Math.Min(part.Length, ShortLinkSettings.Length)))
                        .ToList();
                }
                catch (UriFormatException)
                {
                    // If URL is invalid, generate random keywords
                    keywords.Add("url");
                }
            }

            // If no keywords found, add some default ones
            if (keywords.Count == 0)
            {
                keywords.AddRange(new[] { "link", "url", "short" });
            }
        }
        else
        {
            // use the requested code as the base
            keywords.Add(baseCode);
        }

        // Generate codes based on keywords
        foreach (var keyword in keywords)
        {
            if (suggestedCodes.Count >= count) break;

            if (keyword.Length > ShortLinkSettings.Length)
            {
                // Truncate keyword to fit the code length
                var truncated = keyword.Substring(0, ShortLinkSettings.Length);
                if (!await dbContext.ShortenedUrls.AnyAsync(s => s.Code == truncated))
                {
                    suggestedCodes.Add(truncated);
                }
            }
            else
            {
                var attempts = 0;
                while (suggestedCodes.Count < count && attempts < 5)
                {
                    try
                    {
                        suggestedCodes.Add(await GenerateKeywordSuffix(keyword));
                        attempts += 1;
                    }
                    catch (InvalidOperationException)
                    {
                        // If the keyword is already in use, try again
                        attempts++;
                    }
                }
            }
        }

        // If we still don't have enough suggestions, generate random ones
        while (suggestedCodes.Count < count)
        {
            try
            {
                var randomCode = await GenerateUniqueCodeAsync();
                suggestedCodes.Add(randomCode);
            }
            catch (InvalidOperationException)
            {
                // If we can't generate more codes, break
                break;
            }
        }

        return new SuggestedCodesDto
        {
            SuggestedCodes = suggestedCodes.Take(count).ToList()
        };
    }

    public async Task<bool> DeleteShortenedUrlAsync(string code)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var url = await dbContext.ShortenedUrls
            .FirstOrDefaultAsync(s => s.Code == code);

        if (url == null)
        {
            return false; // URL not found
        }

        dbContext.ShortenedUrls.Remove(url);
        await dbContext.SaveChangesAsync();
        return true; // URL deleted successfully
    }
}