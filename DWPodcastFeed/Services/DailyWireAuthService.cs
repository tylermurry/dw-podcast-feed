using System.Text.RegularExpressions;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Flurl;
using Microsoft.Playwright;

namespace DWPodcastFeed.Services;

public class DailyWireAuthService
{
    private readonly AuthenticationApiClient _authClient = new(Issuer);
    
    private const string Issuer = "authorize.dailywire.com";
    private const string Audience = "https://api.dailywire.com/";
    private const string ClientId = "hDgwLR0K67GTe9IuVKATlbohhsAbD37H";
    private const string RedirectUrl = "https://www.dailywire.com/callback";
    private static readonly string[] Scopes = new[] { "openid", "profile", "email", "offline_access" };

    public async Task<string> GetAccessToken(string username, string password, CancellationToken cancellationToken)
    {
        var authCode = await GetAuthCode(username, password);
        var tokens = await _authClient.GetTokenAsync(new AuthorizationCodeTokenRequest
        {
            ClientId = ClientId,
            Code = authCode,
            RedirectUri = RedirectUrl
        }, cancellationToken);

        return tokens.AccessToken;
    }
    
    private async Task<string> GetAuthCode(string username, string password)
    {
        var authUrl = _authClient.BuildAuthorizationUrl()
            .WithAudience(Audience)
            .WithClient(ClientId)
            .WithRedirectUrl(RedirectUrl)
            .WithScopes(Scopes)
            .WithResponseType(AuthorizationResponseType.Code)
            .Build().ToString();
        
        var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(authUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.FillAsync("input[name=email]", username);
        await page.FillAsync("input[name=password]", password);
        await page.Keyboard.PressAsync("Enter");

        await page.WaitForURLAsync(new Regex("https://www\\.dailywire\\.com/callback\\?code="));
        
        return new Url(page.Url).QueryParams.FirstOrDefault("code")?.ToString()!;
    }
}