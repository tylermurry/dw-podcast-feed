namespace DWPodcastFeed.Models;

public record PodcastListResponse(List<PodcastEpisode> ListPodcastEpisode);

public record Podcast(
    string Slug,
    string Name,
    string Description,
    string? CoverImage
);

public record PodcastEpisode(
    int? EpisodeNumber,
    string? Description,
    double? Duration,
    string? Title,
    DateTimeOffset? PublishDate,
    DateTimeOffset? ScheduleAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? CreatedAt,
    string? AudioMimeType,
    string? Audio,
    Podcast Podcast
);