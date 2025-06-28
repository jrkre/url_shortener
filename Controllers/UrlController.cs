using Microsoft.AspNetCore.Mvc;
using url_shortener.Models;
using url_shortener.Services;
using url_shortener.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Diagnostics;


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
    [Authorize(Roles = "User,Admin")] // Only authenticated users can create shortened URLs
    public async Task<ActionResult<ShortenedUrl>> CreateShortenedUrl([FromBody] CreateShortenedUrlDto shortenedUrl)
    {
        if (shortenedUrl == null || string.IsNullOrWhiteSpace(shortenedUrl.OriginalUrl))
        {
            return BadRequest(new { message = "Invalid URL data." });
        }

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

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ShortenedUrl>>> GetAllShortenedUrls()
    {
        // Get URLs for the current user only
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var shortenedUrls = await _urlService.GetAllShortenedUrlsAsync(userId);

        if (shortenedUrls == null || !shortenedUrls.Any())
        {
            return Ok(new List<ShortenedUrl>()); // Return empty list instead of NotFound
        }

        return Ok(shortenedUrls);
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

        return Ok(shortenedUrl);
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