namespace url_shortener.Settings;

public static class ShortLinkSettings
{
    public const int Length = 6; // Length of the shortened URL code
    public const string BaseUrl = "https://localhost:7179/"; // Base URL for the shortened links
    public const int ExpirationDays = 30; // efault expiration period for shortened URLs in days
    public const bool EnableAnalytics = true; // Flag to enable or disable analytics tracking
    public const string Alphabet =
       "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public const int MaxExpirationDays = 365; // Maximum expiration period for shortened URLs in days
    public const int DefaultExpirationDays = 30; // Default expiration period for shortened URLs in days
    public const int MaxUrlLength = 2048; // Maximum length for the original URL
    public const int MaxClickCount = 1000; // Maximum click count for analytics tracking
    

}