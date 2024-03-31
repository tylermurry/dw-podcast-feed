using System.Net.Http.Headers;
using DWPodcastFeed.Models;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace DWPodcastFeed.Services;

public class DailyWireApiService(IHttpClientFactory _httpClientFactory)
{
    public async Task<List<PodcastEpisode>> GetPodcastEpisodes(string podcastSeasonId, string accessToken, int first = 10)
    {
        var query = """
            query getPodcastEpisodes
            (
                $where: PodcastEpisodeWhereInput,
                $orderBy: PodcastEpisodeOrderBy,
                $first: Int,
                $skip: Int
            )
            {
              listPodcastEpisode(where: $where, orderBy: $orderBy, first: $first, skip: $skip) { ...ResPodcastEpisode }
            }
            fragment ResPodcastEpisode on getPodcastEpisodeRes 
            {
              id
              title
              description
              slug
              episodeNumber
              thumbnail
              weight
              thumbnail
              rating
              duration
              audioState
              audioMuxPlaybackId
              durationWithAds
              audioWithAdsMuxPlaybackId
                publishDate
              createdAt
              scheduleAt
              podcast {
                id
                name
                slug
                description
                belongsTo
                logoImage
                coverImage
                backgroundImage
                author {
                  id
                  firstName
                  lastName
                }
              }
            }
        """;
        
        var response = await BuildClient(accessToken).SendQueryAsync<PodcastListResponse>(new GraphQLRequest(
            query: query,
            variables: new
            {
                where = new { season = new { id = podcastSeasonId } },
                first = first
                
            })
        );
        
        return response.Data.ListPodcastEpisode;
    }
    
    

    private IGraphQLClient BuildClient(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {accessToken}");

        return new GraphQLHttpClient(
            options: new GraphQLHttpClientOptions
            {
                EndPoint = new Uri("https://v2server.dailywire.com/app/graphql")
            },
            serializer: new GraphQL.Client.Serializer.Newtonsoft.NewtonsoftJsonSerializer(), 
            httpClient: client
        );  
    }
}