using DWPodcastFeed.Services;
using Flurl;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<DailyWireApiService>();
builder.Services.AddSingleton<DailyWireAuthService>();
builder.Services.AddSingleton<PodcastFeedService>();

var app = builder.Build();


app.MapGet("/", () => "Hi");

app.MapGet("/podcasts/{podcastId}/feed", async Task<IResult> (
    [FromRoute] string podcastId, 
    [FromQuery] string username, 
    [FromQuery] string password, 
    [FromServices] PodcastFeedService podcastFeedService,
    [FromServices] DailyWireAuthService authService,
    HttpRequest request,
    CancellationToken cancellationToken) =>
{
    var feedUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
    var accessToken = await authService.GetAccessToken(username, password, cancellationToken);
    var feedXmlStream = await podcastFeedService.GetPodcastFeed(feedUrl, podcastId.ToLower(), accessToken, cancellationToken);
    
    return Results.File(fileStream: feedXmlStream, contentType: "application/xml");
});
app.Run();