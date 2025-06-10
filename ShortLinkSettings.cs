namespace url_shortener.Settings;

public static class ShortLinkSettings
{
    public const int Length = 7; // Length of the shortened URL code

    public const int OriginalUrlLength = 2048; // Maximum length for the original URL

    //if production, basurl will be https://lk.shooba.info/
    //if development, baseurl will be http://localhost:5000/
    public const string BaseUrl = "https://lk.shooba.info/"; // Base URL for the shortened links
    public const int ExpirationDays = 30; // efault expiration period for shortened URLs in days
    public const bool EnableAnalytics = true; // Flag to enable or disable analytics tracking
    public const string Alphabet =
       "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public const int MaxExpirationDays = 365; // Maximum expiration period for shortened URLs in days
    public const int DefaultExpirationDays = 30; // Default expiration period for shortened URLs in days
    public const int MaxUrlLength = 2048; // Maximum length for the original URL
    public const int MaxClickCount = 1000; // Maximum click count for analytics tracking
    

}