using Haveno.Api.Helpers;

namespace Merkato.Middleware;

public class PreferencesMiddleware
{
    private readonly RequestDelegate _next;

    public PreferencesMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // Also breaks if user for some reason chooses to navigate to the settings page before any other page has loaded
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Cookies.ContainsKey("currency"))
        {
            context.Response.Cookies.Append(
                "currency",
                CurrencyHelper.FiatCurrenciesDictionary.First().Key,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
        }

        await _next(context);
    }
}


public static class PreferencesMiddlewareExtensions
{
    public static IApplicationBuilder UsePreferences(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PreferencesMiddleware>();
    }
}