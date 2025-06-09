using Microsoft.AspNetCore.Identity;

namespace url_shortener.Models;

public class ApplicationUser : IdentityUser
{
    // You can add custom properties here if needed, e.g.,
    public required string FullName { get; set; }
}