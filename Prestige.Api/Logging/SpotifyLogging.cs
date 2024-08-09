using Prestige.Api.Exceptions;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Logging
{
    public static class SpotifyLogging
    {
        private static readonly Action<ILogger, string, Exception> _trackNotFound;
        private static readonly Action<ILogger, string, Exception> _albumNotFound;
        private static readonly Action<ILogger, string, Exception> _artistNotFound;
        private static readonly Action<ILogger, Exception> _spotifyTokenNotFound;
        private static readonly Action<ILogger, string, Exception> _searchNotFound;

        static SpotifyLogging()
        { 

            _trackNotFound = LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(20000, "TrackNotFound"),
                "Track not found: track id {trackId}."
            );

            _albumNotFound = LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(20001, "AlbumNotFound"),
                "Album not found: album id {albumId}."
            );

            _artistNotFound = LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(20002, "ArtistNotFound"),
                "Artist not found: artist id {artistId}."
            );

            _spotifyTokenNotFound = LoggerMessage.Define(
                LogLevel.Error,
                new EventId(20003, "SpotifyTokenNotFound"),
                "Spotify token not found: Client Credentials."
            );

            _searchNotFound = LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(20004, "SearchNotFound"),
                "Search not found: search term {searchTerm}."
            );

        }

        public static Exception TrackNotFound(this ILogger logger, string trackId)
        {
            var ex = new NotFoundException(20000, "Track not found.");
            _trackNotFound(logger, trackId, ex);
            return ex;
        }

        public static Exception AlbumNotFound(this ILogger logger, string albumId)
        {
            var ex = new NotFoundException(20001, "Album not found.");
            _albumNotFound(logger, albumId, ex);
            return ex;
        }

        public static Exception ArtistNotFound(this ILogger logger, string artistId)
        {
            var ex = new NotFoundException(20002, "Artist not found.");
            _artistNotFound(logger, artistId, ex);
            return ex;
        }

        public static Exception SpotifyTokenNotFound(this ILogger logger)
        {
            var ex = new NotFoundException(20003, "Spotify token not found.");
            _spotifyTokenNotFound(logger, ex);
            return ex;
        }

        public static Exception SearchNotFound(this ILogger logger, string searchTerm)
        {
            var ex = new NotFoundException(20004, "Search not found.");
            _searchNotFound(logger, searchTerm, ex);
            return ex;
        }

    }
}
