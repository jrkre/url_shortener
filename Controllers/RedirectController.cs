using Microsoft.AspNetCore.Mvc;
using url_shortener.Models;
using url_shortener.Services;

namespace url_shortener.Controllers;

[ApiController]
[Route("l/{shortCode}")]
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
    
        ShortenedUrl? shortenedUrl;
        
        try
        {
            // Get the URL and track the click
            shortenedUrl = await _urlShorteningService.TrackClickAndGetUrlAsync(
                shortCode, 
                Request.Headers["User-Agent"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["Referer"].ToString());
        }
        catch (Exception ex)
        {
            return Ok($"There was an issue finding your URL: {ex.Message}");
        }
    
        if (shortenedUrl == null)
        {
            return NotFound("Shortened URL not found.");
        }
    
        return Redirect(shortenedUrl.OriginalUrl);
    }


}