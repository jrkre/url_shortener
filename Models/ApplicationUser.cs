using Microsoft.AspNetCore.Identity;

namespace url_shortener.Models;

public class ApplicationUser : IdentityUser
{
    public required string FullName { get; set; }
}