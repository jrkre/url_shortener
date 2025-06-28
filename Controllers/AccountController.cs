using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using url_shortener.Models;
using url_shortener.DTO;
using Microsoft.AspNetCore.Authorization;
using url_shortener.Services;

namespace url_shortener.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly UrlShorteningService _urlService;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, UrlShorteningService urlShorteningService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _urlService = urlShorteningService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FullName = model.FullName ?? string.Empty
        };
        
        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded) return Ok(new { Message = "User registered successfully." });
        return BadRequest(result.Errors);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, false, false);
        Console.WriteLine($"Login attempt for {model.Username}: {result.Succeeded}, {result.ToString()}");
        if (!result.Succeeded) return Unauthorized(new { Message = "Invalid login attempt." });

        var user = await _userManager.FindByNameAsync(model.Username);

        if (user == null) return NotFound(new { Message = "User not found." });

        var token = GenerateJwtToken(user);

        return Ok(new { Token = token });
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

    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            // new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())

        };

        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT key is not configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
