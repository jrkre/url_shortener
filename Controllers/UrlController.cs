using Microsoft.AspNetCore.Mvc;
using url_shortener.Models;
using url_shortener.Services;
using url_shortener.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;


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
    [Authorize]
    public async Task<ActionResult<ShortenedUrl>> CreateShortenedUrl([FromBody] CreateShortenedUrlDto shortenedUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (shortenedUrl == null || string.IsNullOrWhiteSpace(shortenedUrl.OriginalUrl))
        {
            return BadRequest("Invalid URL data.");
        }

        var createdUrl = _urlService.CreateShortenedUrlAsync(shortenedUrl, user.Id).Result;

        Console.WriteLine($"Created URL: {createdUrl.ShortUrl} for Original URL: {createdUrl.OriginalUrl}");

        return CreatedAtAction(nameof(CreateShortenedUrl), new { code = createdUrl.Code }, createdUrl);
    }

    /*[HttpGet("{code}")]
    public async Task<ActionResult<ShortenedUrl>> GetShortenedUrl(string code)
    {
        Console.WriteLine($"Received code: {code}");
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Code cannot be null or empty.");
        }

        var shortenedUrl = await _urlService.GetShortenedUrlByCodeAsync(code);
        if (shortenedUrl == null)
        {
            return NotFound("Shortened URL not found.");
        }

        return Redirect(shortenedUrl.OriginalUrl);
    }*/

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShortenedUrl>>> GetAllShortenedUrls()
    {
        var shortenedUrls = await _urlService.GetAllShortenedUrlsAsync();
        if (shortenedUrls == null || !shortenedUrls.Any())
        {
            return NotFound("No shortened URLs found.");
        }

        return Ok(shortenedUrls);
    }


    [HttpGet("analytics/{code}")]
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