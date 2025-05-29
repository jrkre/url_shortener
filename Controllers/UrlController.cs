using Microsoft.AspNetCore.Mvc;
using url_shortener.Models;
using url_shortener.Services;
using url_shortener.DTO;


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

        var createdUrl = UrlShorteningService.CreateShortenedUrlAsync(shortenedUrl).Result;

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

        var shortenedUrl = await UrlShorteningService.GetShortenedUrlByCodeAsync(code);
        if (shortenedUrl == null)
        {
            return NotFound("Shortened URL not found.");
        }

        return Redirect(shortenedUrl.OriginalUrl);
    }*/


    [HttpGet("analytics/{code}")]
    public async Task<ActionResult<ShortenedUrl>> GetAnalytics(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Code cannot be null or empty.");
        }

        var shortenedUrl = await UrlShorteningService.GetShortenedUrlByCodeAsync(code);
        if (shortenedUrl == null)
        {
            return NotFound("Shortened URL not found.");
        }

        return Ok(shortenedUrl);
    }

}