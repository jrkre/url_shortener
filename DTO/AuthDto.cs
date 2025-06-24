namespace url_shortener.DTO;

public class RegisterDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
    public string? FullName { get; set; }
    public required string UserName { get; set; } // Optional: to allow setting a username
}

public class LoginDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}