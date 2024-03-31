using System.Xml;
using System.Xml.Linq;
using DWPodcastFeed.Models;

namespace DWPodcastFeed.Services;

public class PodcastFeedService(DailyWireApiService dailyWireApiService)
{
    public async Task<Stream> GetPodcastFeed(string feedUrl, string podcastId, string accessToken, CancellationToken cancellationToken)
    {
        var podcastSeasonIdMap = new Dictionary<string, string>
        {
            {"matt-walsh", "clqxyxgj258dp0806ub9ot7ah" }
        };
        
        var podcastEpisodes = await dailyWireApiService.GetPodcastEpisodes(podcastSeasonIdMap[podcastId], accessToken);
        var podcastXmlDocument = await BuildPodcastFeedXml(feedUrl, podcastEpisodes);

        return await GetStreamFromDocument(podcastXmlDocument, cancellationToken);
    }

    private async Task<XDocument> BuildPodcastFeedXml(string feedUrl, List<PodcastEpisode> podcastEpisodes)
    {
        XNamespace itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
        XNamespace googlePlay = "http://www.google.com/schemas/play-podcasts/1.0";
        XNamespace atom = "http://www.w3.org/2005/Atom";
        XNamespace media = "http://search.yahoo.com/mrss/";
        XNamespace content = "http://purl.org/rss/1.0/modules/content/";

        var document = new XDocument();
        var podcast = podcastEpisodes[0].Podcast;

        var root = new XElement("rss",
            new XAttribute(XNamespace.Xmlns + "itunes", itunes.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "googleplay", googlePlay.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "media", media.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "content", content.NamespaceName),
            new XAttribute("version", "2.0")
        );

        document.Add(root);

        var channel = new XElement("channel",
            new XElement(atom + "link",
                new XAttribute("href", feedUrl),
                new XAttribute("rel", "self"),
                new XAttribute("type", "application/rss+xml")
            ),
            new XElement("link", $"https://www.dailywire.com/show/{podcast.Slug}"),
            new XElement("title", podcast.Name),
            new XElement("description", podcast.Description),
            new XElement(itunes + "subtitle", podcast.Description),
            new XElement("language", "en"),
            new XElement("copyright", "All rights reserved"),
            new XElement(itunes + "author", "The Daily Wire"),
            new XElement(itunes + "explicit", "no"),
            new XElement(itunes + "type", "episodic"),
            new XElement(itunes + "summary", podcast.Description),
            new XElement(content + "encoded", new XCData($"<p>{podcast.Description}</p>")),
            new XElement(
                itunes + "owner",
                new XElement(itunes + "name", podcast.Name),
                new XElement(itunes + "email", "podcasts@dailywire.com")
            )
        );

        if (podcast.CoverImage is not null)
        {
            channel.Add(
                new XElement("image",
                    new XElement("url", podcast.CoverImage),
                    new XElement("title", podcast.Name),
                    new XElement("link", $"https://www.dailywire.com/show/{podcast.Slug}")
                ),
                new XElement(itunes + "image",
                    new XAttribute("href", podcast.CoverImage)
                )
            );
        }

        foreach (var episode in podcastEpisodes)
        {
            var item = new XElement("item",
                new XElement(itunes + "episodeType", "full"),
                new XElement(itunes + "episode", episode.EpisodeNumber),
                new XElement(itunes + "author", "The Daily Wire"),
                new XElement(itunes + "summary", episode.Description),
                new XElement(content + "encoded", new XCData($"<p>{episode.Description}</p>")),
                new XElement(itunes + "duration", episode.Duration.HasValue ? Math.Round(episode.Duration.Value) : null)
            );

            if (!string.IsNullOrEmpty(episode.Title))
            {
                item.Add(new XElement("title", episode.Title));
                item.Add(new XElement(itunes + "title", episode.Title));
            }

            if (!string.IsNullOrEmpty(episode.Description))
            {
                item.Add(new XElement("description", episode.Description));
            }

            var episodeDate = episode.PublishDate ?? episode.ScheduleAt ?? episode.UpdatedAt ?? episode.CreatedAt;

            if (episodeDate.HasValue)
            {
                var timestamp = episodeDate.Value.ToString("ddd, dd MMM yyyy HH:mm:ss zz") + episodeDate.Value.Offset.ToString("mm");

                item.Add(new XElement("pubDate", timestamp));
            }

            var enclosure = new XElement("enclosure", new XAttribute("type", episode.AudioMimeType ?? "application/octet-stream"));

            if (episode.Audio is not null)
            {
                enclosure.Add(new XAttribute("url", episode.Audio));
            }

            if (episode.Duration.HasValue)
            {
                enclosure.Add(new XAttribute("length", Math.Round(episode.Duration.Value)));
            }

            item.Add(enclosure);
            channel.Add(item);
        }

        root.Add(channel);

        return document;
    }
    
    private async Task<Stream> GetStreamFromDocument(XDocument document, CancellationToken cancellationToken)
    {
        var stream = new MemoryStream();
        var settings = new XmlWriterSettings { Async = true };
        var writer = XmlWriter.Create(stream, settings);

        await document.WriteToAsync(writer, cancellationToken);

        writer.Close();
        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }
}