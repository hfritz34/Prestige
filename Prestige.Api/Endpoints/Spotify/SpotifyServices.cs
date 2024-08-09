using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Prestige.Api.Logging;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Spotify.RequestResponse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;

namespace Prestige.Api.Endpoints.Spotify
{
    public class SpotifyServices : BaseService
    {

        public SpotifyServices(PrestigeContext db, ILogger<SpotifyServices> logger, ClaimsPrincipal principal, IConfiguration config) : base(db, logger, principal, config)
        {

        }

        private async Task<HttpClient> getSpotifyAuthorizedClient()
        {
            var spotifyTokens = new HttpClient
            {
                BaseAddress = new Uri("https://accounts.spotify.com/api/token")
            };
            spotifyTokens.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    ASCIIEncoding.ASCII.GetBytes(Config["SPOTIFY_CLIENT_ID"] + ":" + Config["SPOTIFY_CLIENT_SECRET"])
                )
            );

            var tokensRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            });

            var tokensResponse = await spotifyTokens.PostAsync("", tokensRequest);
            tokensResponse.EnsureSuccessStatusCode();

            var tokens = await tokensResponse.Content.ReadFromJsonAsync<TokenResponse>();
            var client_credentials = tokens?.AccessToken ?? throw Logger.SpotifyTokenNotFound();

            var spotify = new HttpClient
            {
                BaseAddress = new Uri("https://api.spotify.com/v1/")
            };

            spotify.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", client_credentials);
            return spotify;
        }

        public async Task<IEnumerable<TrackResponse>> SearchForTracksAsync(SearchRequest request)
        {

            var spotify = await getSpotifyAuthorizedClient();
            var searchUrl = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(request.Query)}&type=track&limit={request.Limit}&market={request.Market}";

            var response = await spotify.GetAsync(searchUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw Logger.SearchNotFound(request.Query);
            }
            var tracksResponse = await response.Content.ReadFromJsonAsync<SearchResponse>() ?? throw Logger.SearchNotFound(request.Query);

            var tracks = tracksResponse.Tracks?.Items ?? throw Logger.SearchNotFound(request.Query);

            tracks.ToList().ForEach(track => PostTrack(track));
            PrestigeDb.SaveChanges();
            return tracks;
        }

        public async Task<IEnumerable<AlbumResponse>> SearchForAlbumsAsync(SearchRequest request)
        {
            var spotify = await getSpotifyAuthorizedClient();
            var searchUrl = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(request.Query)}&type=album&limit={request.Limit}&market={request.Market}";

            var response = await spotify.GetAsync(searchUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw Logger.SearchNotFound(request.Query);
            }
            var albumsResponse = await response.Content.ReadFromJsonAsync<SearchResponse>() ?? throw Logger.SearchNotFound(request.Query);

            var albums = albumsResponse.Albums?.Items ?? throw Logger.SearchNotFound(request.Query);
            albums.ToList().ForEach(album => PostAlbum(album));
            PrestigeDb.SaveChanges();
            return albums;
        }

        public async Task<IEnumerable<ArtistResponse>> SearchForArtistsAsync(SearchRequest request)
        {
            var spotify = await getSpotifyAuthorizedClient();
            var searchUrl = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(request.Query)}&type=artist&limit={request.Limit}&market={request.Market}";

            var response = await spotify.GetAsync(searchUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw Logger.SearchNotFound(request.Query);
            }
            var artistsResponse = await response.Content.ReadFromJsonAsync<SearchResponse>() ?? throw Logger.SearchNotFound(request.Query);

            var artists = artistsResponse.Artists?.Items ?? throw Logger.SearchNotFound(request.Query);
            artists.ToList().ForEach(artist => PostArtist(artist));
            PrestigeDb.SaveChanges();
            return artists;
        }

        public async Task<TrackResponse?> GetTrackByIdAsync(string id)
        {
            var track = await PrestigeDb.Tracks
                .Include(track => track.Album)
                    .ThenInclude(album => album.Artists)
                .Include(track => track.Album)
                    .ThenInclude(album => album.Images)
                .Include(track => track.Artists)
                    .ThenInclude(artist => artist.Images)
                .FirstOrDefaultAsync(track => track.Id == id);
            if (track != null)
            {
                return new TrackResponse(track);
            }

            var client = await getSpotifyAuthorizedClient();

            var response = await client.GetAsync($"tracks/{id}?market=US");

            if (!response.IsSuccessStatusCode)
            {
                throw Logger.TrackNotFound(id);
            }
            var trackResponse = await response.Content.ReadFromJsonAsync<TrackResponse>();

            if (trackResponse != null)
            {
                track = PostTrack(trackResponse);
                PrestigeDb.SaveChanges();
                return new TrackResponse(track);
            }
            throw Logger.TrackNotFound(id);
        }

        public async Task<IEnumerable<TrackResponse>> GetTracksByIdsAsync(string ids)
        {
            var idsArray = ids.Split(',').ToHashSet();
            var trackNotFound = new HashSet<string>();
            var artistUpdate = new HashSet<string>();
            foreach (var id in idsArray)
            {
                var track = PrestigeDb.Tracks.Find(id);
                if (track != null)
                {
                    PrestigeDb.Entry(track).Collection(track => track.Artists).Load();
                    foreach (var artist in track.Artists)
                    {
                        PrestigeDb.Entry(artist).Collection(artist => artist.Images).Load();
                        if (!artist.Images.Any())
                        {
                            artistUpdate.Add(artist.Id);
                        }
                    }
                }
                else
                {
                    trackNotFound.Add(id);
                }
            }
            if (trackNotFound.Count > 0)
            {
                var client = await getSpotifyAuthorizedClient();
                var spotifyIds = string.Join(",", trackNotFound);
                var response = await client.GetAsync($"tracks?ids={spotifyIds}");

                var spotifyTracksResponse = await response.Content.ReadFromJsonAsync<ManyTrackResponse>() ?? throw Logger.TrackNotFound(spotifyIds);
                foreach (var track in spotifyTracksResponse.Tracks)
                {
                    var newTrack = PostTrack(track);
                    foreach (var artist in newTrack.Artists)
                    {
                        if (!artist.Images.Any())
                        {
                            artistUpdate.Add(artist.Id);
                        }
                    }
                }
                PrestigeDb.SaveChanges();
            }
            if (artistUpdate.Count > 0)
            {
                var client = await getSpotifyAuthorizedClient();
                var artistIds = string.Join(",", artistUpdate);
                var spotifyArtistsResponse = await client.GetAsync($"artists?ids={artistIds}");
                spotifyArtistsResponse.EnsureSuccessStatusCode();
                var newArtists = await spotifyArtistsResponse.Content.ReadFromJsonAsync<ManyArtistResponse>() ?? throw Logger.ArtistNotFound(artistIds);
                foreach (var artist in newArtists.Artists)
                {
                    PostArtist(artist);
                }
                PrestigeDb.SaveChanges();
            }
            return PrestigeDb.Tracks
                    .Include(track => track.Album)
                        .ThenInclude(album => album.Artists)
                    .Include(track => track.Album)
                        .ThenInclude(album => album.Images)
                    .Include(track => track.Artists)
                        .ThenInclude(artist => artist.Images)
                    .Where(track => idsArray.Contains(track.Id))
                    .Select(track => new TrackResponse(track))
                    .ToList();
        }

        public async Task<AlbumResponse?> GetAlbumByIdAsync(string id)
        {

            var album = PrestigeDb.Albums
                .Include(album => album.Artists)
                    .ThenInclude(artist => artist.Images)
                .Include(album => album.Images)
                .FirstOrDefault(album => album.Id == id);
            if (album != null)
            {
                return new AlbumResponse(album);
            }

            var client = await getSpotifyAuthorizedClient();

            var response = await client.GetAsync($"albums/{id}");

            if (!response.IsSuccessStatusCode)
            {
                throw Logger.AlbumNotFound(id);
            }
            var albumResponse = await response.Content.ReadFromJsonAsync<AlbumResponse>();

            if (albumResponse != null)
            {
                album = PostAlbum(albumResponse);
                PrestigeDb.SaveChanges();
                return new AlbumResponse(album);
            }

            throw Logger.AlbumNotFound(id);
        }

        public async Task<ArtistResponse?> GetArtistByIdAsync(string id)
        {
            var artist = PrestigeDb.Artists
                .Include(artist => artist.Images)
                .FirstOrDefault(artist => artist.Id == id);
            if (artist != null)
            {
                return new ArtistResponse(artist);
            }
            var client = await getSpotifyAuthorizedClient();

            var response = await client.GetAsync($"artists/{id}");

            if (!response.IsSuccessStatusCode)
            {
                throw Logger.ArtistNotFound(id);
            }
            var artistResponse = await response.Content.ReadFromJsonAsync<ArtistResponse>();

            if (artistResponse != null)
            {
                artist = PostArtist(artistResponse);
                PrestigeDb.SaveChanges();
                return new ArtistResponse(artist);
            }
            throw Logger.ArtistNotFound(id);
        }


        // For performance reasons post functions do NOT save to db, must call SaveChanges after calling post functions
        public Track PostTrack(TrackResponse trackResponse)
        {
            var track = PrestigeDb.Tracks.Find(trackResponse.Id);
            if (track == null)
            {
                var artists = new List<Artist>();
                foreach (var artist in trackResponse.Artists)
                {
                    artists.Add(PostArtist(artist));
                }
                var album = PostAlbum(trackResponse.Album);
                track = new Track(trackResponse, album, artists);
                PrestigeDb.Tracks.Add(track);
            }
            else
            {
                track.Update(trackResponse);
                PostAlbum(trackResponse.Album);
                foreach (var artist in trackResponse.Artists)
                {
                    PostArtist(artist);
                }

            }
            return track;
        }

        public Album PostAlbum(AlbumResponse albumResponse)
        {
            var album = PrestigeDb.Albums
                .Include(album => album.Artists)
                    .ThenInclude(artist => artist.Images)
                .Include(album => album.Images)
                .FirstOrDefault(album => album.Id == albumResponse.Id)
                ?? PrestigeDb.Albums.Find(albumResponse.Id);
            if (album == null)
            {
                var artists = new List<Artist>();
                var images = new List<Image>();
                foreach (var image in albumResponse.Images)
                {
                    images.Add(PostImage(image));
                }
                foreach (var artist in albumResponse.Artists)
                {
                    artists.Add(PostArtist(artist));
                }
                album = new Album(albumResponse, artists, images);
                PrestigeDb.Albums.Add(album);
            }
            else
            {
                var images = new List<Image>();
                foreach (var image in albumResponse.Images)
                {
                    images.Add(PostImage(image));
                }
                album.Update(albumResponse, images);
            }
            return album;
        }

        public Artist PostArtist(ArtistResponse artistResponse)
        {
            var artist = PrestigeDb.Artists
                .Find(artistResponse.Id);
            if (artist == null)
            {
                var images = new List<Image>();
                foreach (var image in artistResponse.Images)
                {
                    images.Add(PostImage(image));
                }
                artist = new Artist(artistResponse, images);
                PrestigeDb.Artists.Add(artist);
            }
            else
            {
                var images = new List<Image>();
                foreach (var image in artistResponse.Images)
                {
                    images.Add(PostImage(image));
                }
                images = images.Count != 0 ? images : artist.Images?.ToList() ?? [];
                artist.Update(artistResponse, images);
            }
            return artist;
        }

        public Image PostImage(ImageResponse imageResponse)
        {
            var image = PrestigeDb.Images.Find(imageResponse.Url);
            if (image == null)
            {
                image = new Image(imageResponse);
                PrestigeDb.Images.Add(image);
            }
            return image;
        }
    }
}