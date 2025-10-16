using Microsoft.AspNetCore.Mvc;
using url_shortener.Models;
using url_shortener.Services;
using url_shortener.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Diagnostics;
using System.Threading.Tasks;


namespace url_shortener.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UrlController : ControllerBase
{

    private readonly UrlShorteningService _urlService;
    
    private readonly UserManager<ApplicationUser> _userManager;

    public UrlController(UrlShorteningService urlShorteningService, UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
        _urlService = urlShorteningService;
    }


    [HttpPost("create")]
    public async Task<ActionResult<ShortenedUrl>> CreateShortenedUrl([FromBody] CreateShortenedUrlDto shortenedUrl)
    {
        if (shortenedUrl == null || string.IsNullOrWhiteSpace(shortenedUrl.OriginalUrl))
        {
            return BadRequest(new { message = "Invalid URL data." });
        }
        
        // try
        // {
        //     var createdUrl = UrlShorteningService.CreateShortenedUrlAsync(shortenedUrl).Result;
        //     Console.WriteLine($"Created URL: {createdUrl.ShortUrl} for Original URL: {createdUrl.OriginalUrl}");
        //     return CreatedAtAction(nameof(CreateShortenedUrl), new { code = createdUrl.Code }, createdUrl);
        // }
        // catch (ArgumentException ex)
        // {
        //     return BadRequest(ex.Message);
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"Error creating shortened URL: {ex.Message}");
        //     return StatusCode(500, "An error occurred while creating the shortened URL.");
        // }

        Console.WriteLine($"Received URL to shorten: {shortenedUrl.OriginalUrl} from User: {User.Identity?.Name}");

        try
        {
            // Get the current user ID from the JWT token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"User ID from token: {userId}");

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            var createdUrl = await _urlService.CreateShortenedUrlAsync(shortenedUrl, userId);
            Debug.WriteLine($"Created URL: {createdUrl.ShortUrl} for Original URL: {createdUrl.OriginalUrl}");
            return CreatedAtAction(nameof(CreateShortenedUrl), new { code = createdUrl.Code }, createdUrl);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating shortened URL: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while creating the shortened URL." });
        }
    }

    


    [HttpGet("analytics/{code}")]
    [Authorize]
    public async Task<ActionResult<ShortenedUrl>> GetAnalytics(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Code cannot be null or empty.");
        }

        var shortenedUrl = await _urlService.GetShortenedUrlAnalyticsByCodeAsync(code);
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

    [HttpGet("my-urls")]
    [Authorize]  // Requires authentication
    public async Task<IActionResult> GetMyUrls()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var urls = await _urlService.GetShortenedUrlsByUserAsync(user.Id);
        return Ok(urls);
    }

}