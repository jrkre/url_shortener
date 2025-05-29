using Microsoft.AspNetCore.Mvc;
using url_shortener.Services;

namespace url_shortener.Controllers;

[ApiController]
[Route("{shortCode}")]
public class RedirectController : ControllerBase
{
    private readonly UrlShorteningService _urlShorteningService;

    public RedirectController(UrlShorteningService urlShorteningService)
    {
        _urlShorteningService = urlShorteningService;
    }

    [HttpGet]
    public async Task<IActionResult> RedirectToOriginalUrl(string shortCode)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
        {
            return BadRequest("Short code cannot be null or empty.");
        }
    
        var shortenedUrl = await _urlShorteningService.GetShortenedUrlByCodeAsync(shortCode);
        if (shortenedUrl == null)
        {
            return NotFound("Shortened URL not found.");
        }

        // Check if the URL has expired
        if (shortenedUrl.ExpirationDate.HasValue && shortenedUrl.ExpirationDate <= DateTime.UtcNow)
        {
            return NotFound("This shortened URL has expired.");
        }

        return Redirect(shortenedUrl.OriginalUrl);
        
    }


}