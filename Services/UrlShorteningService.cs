namespace url_shortener.Services;

using url_shortener.Settings;
using url_shortener.Data; // Ensure this matches the namespace where ApplicationDbContext is defined
using Microsoft.EntityFrameworkCore;
using url_shortener.Models;
using url_shortener.DTO;
using System.Collections.Concurrent;

public class UrlShorteningService
{
    private ApplicationDbContext _dbContext;
    private readonly ConcurrentQueue<string> _codePool = new();
    private readonly SemaphoreSlim _poolSemaphore = new(1, 1);
    private const int MinPoolSize = 100;
    private const int MaxPoolSize = 500;

    public UrlShorteningService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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
            
            // Check batch against database in single query
            var existingCodes = new HashSet<string>(await _dbContext.ShortenedUrls
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

        while (attempts < maxAttempts)
        {
            for (var i = 0; i < ShortLinkSettings.Length; i++)
            {
                var randomIndex = Random.Shared.Next(62);
                codeChars[i] = ShortLinkSettings.Alphabet[randomIndex];
            }
            var code = new string(codeChars);

            if (!await _dbContext.ShortenedUrls.AnyAsync(s => s.Code == code))
            {
                return code;
            }
            attempts++;
        }

        throw new InvalidOperationException("Unable to generate unique code after maximum attempts.");
    }

    public async Task<ShortenedUrl> CreateShortenedUrlAsync(CreateShortenedUrlDto shortenedUrlDto)
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

        var now = DateTime.UtcNow;
        var shortenedUrl = new ShortenedUrl(
            originalUrl: shortenedUrlDto.OriginalUrl,
            shortenedUrl: null,
            code: null,
            createdAt: now,
            expirationDate: shortenedUrlDto.ExpirationDate ?? now.AddDays(ShortLinkSettings.DefaultExpirationDays))
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
            var existingActiveUrl = await _dbContext.ShortenedUrls
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

        await _dbContext.ShortenedUrls.AddAsync(shortenedUrl);
        await _dbContext.SaveChangesAsync();

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

    private async Task ValidateUrlStatusAsync(ShortenedUrl url, string code)
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
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<ShortenedUrl?> GetShortenedUrlByCodeAsync(string code)
    {
        var url = await _dbContext.ShortenedUrls
            .AsNoTracking() // Track changes for updates
            .FirstOrDefaultAsync(s => s.Code == code && s.IsActive && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow));

        if (url == null)
        {
            throw new InvalidOperationException("No active URL found for the provided code.");
        }

        // url.ClickCount++;
        await ValidateUrlStatusAsync(url, code);
        await _dbContext.SaveChangesAsync();

        return url;
    }

    public async Task<ShortenedUrl?> GetShortenedUrlAnalyticsByCodeAsync(string code)
    {
        var url = await _dbContext.ShortenedUrls
            .AsNoTracking() // Optimize for read-only query
            .Include(s => s.ClickEvents)
            .FirstOrDefaultAsync(s => s.Code == code);

        if (url != null)
        {
            await ValidateUrlStatusAsync(url, code);
        }

        return url;
    }

    public async Task<ShortenedUrl?> TrackClickAndGetUrlAsync(string code, string? userAgent, string? ipAddress, string? referrer)
    {
        var url = await _dbContext.ShortenedUrls
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

        await ValidateUrlStatusAsync(url, code);
        await _dbContext.SaveChangesAsync();

        return url;
    }

    
    public async Task<IEnumerable<ShortenedUrl>> GetAllShortenedUrlsAsync()
    {
        return await _dbContext.ShortenedUrls
            .AsNoTracking() // Optimize for read-only query
            .Where(s => s.IsActive && (s.ExpirationDate == null || s.ExpirationDate > DateTime.UtcNow))
            .ToListAsync();
    }

}