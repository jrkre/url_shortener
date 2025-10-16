
namespace url_shortener.DTO;

public class CreateShortenedUrlDto
{
    public required string OriginalUrl { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? RequestedCode { get; set; } = string.Empty;
}

public class SuggestedCodesDto
{
    public List<string> SuggestedCodes { get; set; } = new List<string>();
}