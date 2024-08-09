using System;
using Microsoft.Extensions.Logging;
using Prestige.Api.Exceptions;
using ArgumentNullException = Prestige.Api.Exceptions.ArgumentNullException;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Logging
{
    public static class PrestigeLogging
    {
        private static readonly Action<ILogger, string, string, Exception> _userTrackNotFound;
        private static readonly Action<ILogger, string, string, Exception> _userAlbumNotFound;
        private static readonly Action<ILogger, string, string, Exception> _userArtistNotFound;
        static PrestigeLogging()
        {

            _userTrackNotFound = LoggerMessage.Define<string, string>(
                LogLevel.Error,
                new EventId(30000, "UserTrackNotFound"),
                "User track not found: user id {userId}, track id {trackId}."
            );

            _userAlbumNotFound = LoggerMessage.Define<string, string>(
                LogLevel.Error,
                new EventId(30001, "UserAlbumNotFound"),
                "User album not found: user id {userId}, album id {albumId}."
            );

            _userArtistNotFound = LoggerMessage.Define<string, string>(
                LogLevel.Error,
                new EventId(30002, "UserArtistNotFound"),
                "User artist not found: user id {userId}, artist id {artistId}."
            );
        }
        public static Exception UserTrackNotFound(this ILogger logger, string userId, string trackId)
        {
            var ex = new NotFoundException(30000, "User track not found.");
            _userTrackNotFound(logger, userId, trackId, ex);
            return ex;
        }

        public static Exception UserAlbumNotFound(this ILogger logger, string userId, string albumId)
        {
            var ex = new NotFoundException(30001, "User album not found.");
            _userAlbumNotFound(logger, userId, albumId, ex);
            return ex;
        }

        public static Exception UserArtistNotFound(this ILogger logger, string userId, string artistId)
        {
            var ex = new NotFoundException(30002, "User artist not found.");
            _userArtistNotFound(logger, userId, artistId, ex);
            return ex;
        }
    }
}
