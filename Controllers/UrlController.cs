using Microsoft.AspNetCore.Mvc;
using url_shortener.Models;
using url_shortener.Services;
using url_shortener.DTO;
using System.Threading.Tasks;


namespace url_shortener.Controllers;

[ApiController]
[Route("api/url")]
public class UrlController : ControllerBase
{

    private readonly UrlShorteningService UrlShorteningService;

    public UrlController(UrlShorteningService urlShorteningService)
    {
        UrlShorteningService = urlShorteningService;
    }

    [HttpPost("create")]
    public ActionResult<ShortenedUrl> CreateShortenedUrl([FromBody] CreateShortenedUrlDto shortenedUrl)
    {
        if (shortenedUrl == null || string.IsNullOrWhiteSpace(shortenedUrl.OriginalUrl))
        {
            return BadRequest("Invalid URL data.");
        }
        
        try
        {
            var createdUrl = UrlShorteningService.CreateShortenedUrlAsync(shortenedUrl).Result;
            Console.WriteLine($"Created URL: {createdUrl.ShortUrl} for Original URL: {createdUrl.OriginalUrl}");
            return CreatedAtAction(nameof(CreateShortenedUrl), new { code = createdUrl.Code }, createdUrl);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating shortened URL: {ex.Message}");
            return StatusCode(500, "An error occurred while creating the shortened URL.");
        }

    }

    /*[HttpGet("{code}")]
    public async Task<ActionResult<ShortenedUrl>> GetShortenedUrl(string code)
    {
        Console.WriteLine($"Received code: {code}");
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Code cannot be null or empty.");
        }

        var shortenedUrl = await UrlShorteningService.GetShortenedUrlByCodeAsync(code);
        if (shortenedUrl == null)
        {
            return NotFound("Shortened URL not found.");
        }

        return Redirect(shortenedUrl.OriginalUrl);
    }*/

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShortenedUrl>>> GetAllShortenedUrls()
    {
        var shortenedUrls = await UrlShorteningService.GetAllShortenedUrlsAsync();
        if (shortenedUrls == null || !shortenedUrls.Any())
        {
            return NotFound("No shortened URLs found.");
        }

        return Ok(shortenedUrls);
    }

    [HttpPost("suggest-codes")]
    public async Task<ActionResult> GetSuggestedCodes([FromBody] CreateShortenedUrlDto shortenedUrl, [FromQuery] int count = 5)
    {
        if (count <= 0 || count > 20)
        {
            return BadRequest("Count must be between 1 and 20.");
        }

        var suggestedCodes = await UrlShorteningService.GenerateSuggestedCodes(count, shortenedUrl);

        return Ok(suggestedCodes);
    }
    



    [HttpGet("analytics/{code}")]
    public async Task<ActionResult<ShortenedUrl>> GetAnalytics(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Code cannot be null or empty.");
        }

        var shortenedUrl = await UrlShorteningService.GetShortenedUrlAnalyticsByCodeAsync(code);
        if (shortenedUrl == null)
        {
            return NotFound("Shortened URL not found.");
        }


        // form urlanalyticsdto
        var urlAnalyticsDto = new UrlAnalyticsDto
        {
            Code = shortenedUrl.Code,
            OriginalUrl = shortenedUrl.OriginalUrl,
            CreatedAt = shortenedUrl.CreatedAt,
            ExpirationDate = shortenedUrl.ExpirationDate,
            ClickCount = shortenedUrl.ClickCount,
            IsActive = shortenedUrl.IsActive,
            ClicksByDay = shortenedUrl.ClickEvents
                .GroupBy(c => c.Timestamp.Date)
                .Select(g => new ClickEventDto { TimeStamp = g.Key, Count = g.Count() } )
                .OrderBy(x => x.TimeStamp)
                .ToList(),
            TopReferrers = shortenedUrl.ClickEvents
                .Where(c => !string.IsNullOrEmpty(c.Referrer) )
                .GroupBy(c => c.Referrer)
                .Select(g => new ClickEventDto { Referrer = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList(),
            BrowserStats = shortenedUrl.ClickEvents
                .Where(c => !string.IsNullOrEmpty(c.UserAgent))
                .GroupBy(c => GetBrowserName(c.UserAgent))
                .Select(g => new ClickEventDto { Browser = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            RecentClicks = shortenedUrl.ClickEvents
                .OrderByDescending(c => c.Timestamp)
                .Take(10)
                .Select(c => new ClickEventDto
                {
                    TimeStamp = c.Timestamp,
                    UserAgent = c.UserAgent,
                    Referrer = c.Referrer,
                    IpAddress = MaskIpAddress(c.IpAddress)
                })
                .ToList()
        };


        return Ok(urlAnalyticsDto);
    }

    private string GetBrowserName(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        
        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
        if (userAgent.Contains("Edge")) return "Edge";
        if (userAgent.Contains("MSIE") || userAgent.Contains("Trident")) return "Internet Explorer";
        
        return "Other";
    }
    
    private string MaskIpAddress(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return "Unknown";
        
        // Simple masking for privacy - show only first part of IP
        var parts = ipAddress.Split('.');
        if (parts.Length == 4) // IPv4
        {
            return $"{parts[0]}.{parts[1]}.*.*";
        }
        
        return "Masked IP";
    }

}