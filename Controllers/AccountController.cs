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

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        return Ok(new
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            ProfilePicture = user.ProfilePicture
        });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        // Update user properties
        if (!string.IsNullOrWhiteSpace(model.FullName))
        {
            user.FullName = model.FullName;
        }

        if (!string.IsNullOrWhiteSpace(model.Email) && model.Email != user.Email)
        {
            user.Email = model.Email;
            user.EmailConfirmed = false; // Reset email confirmation if email changes
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { Message = "Profile updated successfully." });
    }

    [HttpPost("profile/picture")]
    [Authorize]
    public async Task<IActionResult> UpdateProfilePicture([FromForm] IFormFile profilePicture)
    {
        if (profilePicture == null || profilePicture.Length == 0)
        {
            return BadRequest(new { Message = "No file uploaded." });
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(profilePicture.ContentType.ToLower()))
        {
            return BadRequest(new { Message = "Only image files (JPEG, PNG, GIF) are allowed." });
        }

        // Validate file size (5MB max)
        if (profilePicture.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { Message = "File size cannot exceed 5MB." });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        try
        {
            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsPath);

            // Generate unique filename
            var fileExtension = Path.GetExtension(profilePicture.FileName);
            var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Delete old profile picture if exists
            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // Save new file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(stream);
            }

            // Update user profile picture path
            user.ProfilePicture = $"/uploads/profiles/{fileName}";
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { 
                Message = "Profile picture updated successfully.",
                ProfilePicture = user.ProfilePicture
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while uploading the file." });
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
