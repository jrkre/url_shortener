using url_shortener.Models;

namespace url_shortener.DTO;


public class UrlAnalyticsDto
{
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int ClickCount { get; set; }

    public List<ClickEventDto> ClicksByDay { get; set; } = new List<ClickEventDto>();
    public List<ClickEventDto> TopReferrers { get; set; } = new List<ClickEventDto>();
    public List<ClickEventDto> BrowserStats { get; set; } = new List<ClickEventDto>();
    public List<ClickEventDto> RecentClicks { get; set; } = new List<ClickEventDto>();


}

public class ClickEventDto
{
    public DateTime TimeStamp { get; set; }
    public int Count { get; set; } // Used for grouping and counting clicks
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? Referrer { get; set; }
    public string Browser { get; set; } = string.Empty;
    public string MaskedIpAddress { get; set; } = string.Empty;
    public string ClickedAtFormatted => TimeStamp.ToString("yyyy-MM-dd HH:mm:ss");
    public string ClickedAtDate => TimeStamp.ToString("yyyy-MM-dd");
    public string ClickedAtTime => TimeStamp.ToString("HH:mm:ss");
    public string ClickedAtDayOfWeek => TimeStamp.DayOfWeek.ToString();
}