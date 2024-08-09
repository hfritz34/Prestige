using System;
using Microsoft.Extensions.Logging;
using Prestige.Api.Exceptions;
using ArgumentNullException = Prestige.Api.Exceptions.ArgumentNullException;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Logging
{
    public static class UserLogging
    {

        private static readonly Action<ILogger, string, Exception> _configurationMissing;
        private static readonly Action<ILogger, Exception> _httpContextUserIsNull;
        private static readonly Action<ILogger, string, Exception> _userNotFound;
        private static readonly Action<ILogger, string, Exception> _userUnauthorized;
        private static readonly Action<ILogger, string, Exception> _tokenNotFound;
        private static readonly Action<ILogger, string, string, Exception> _favoritesLengthExceeded;
        static UserLogging()
        {

            _configurationMissing = LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(10000, "ConfigurationMissing"),
                "Configuration missing: key {configurationKey}."
            );

            _httpContextUserIsNull = LoggerMessage.Define(
                LogLevel.Error,
                new EventId(10001, "HttpContextUserIsNull"),
                "HttpContext user is null."
            );

            _userNotFound = LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(10002, "UserNotFound"),
                "User not found: auth id {authId}."
            );

            _userUnauthorized = LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(10003, "UserUnauthorized"),
                "User unauthorized: auth id {authId}."
            );

            _tokenNotFound = LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(10004, "TokenNotFound"),
                "Token not found: Type : {tokenType}."
            );

            _favoritesLengthExceeded = LoggerMessage.Define<string, string>(
                LogLevel.Error,
                new EventId(10005, "FavoritesLengthExceeded"),
                "Favorites length exceeded: User id {userId}, Favorite Type {favoriteType}."
            );
        }

        public static Exception ConfigurationMissing(this ILogger logger, string configurationKey)
        {
            var ex = new ArgumentNullException(10000, "Configuration missing.");
            _configurationMissing(logger, configurationKey, ex);
            return ex;
        }

        public static Exception HttpContextUserIsNull(this ILogger logger)
        {
            var ex = new Exception(10001, "HttpContext user is null.");
            _httpContextUserIsNull(logger, ex);
            return ex;
        }

        public static Exception UserNotFound(this ILogger logger, string authId)
        {
            var ex = new NotFoundException(10002, "User not found.");
            _userNotFound(logger, authId, ex);
            return ex;
        }

        public static Exception UserUnauthorized(this ILogger logger, string authId)
        {
            var ex = new UnauthorizedException(10003, "User unauthorized.");
            _userUnauthorized(logger, authId, ex);
            return ex;
        }

        public static Exception TokenNotFound(this ILogger logger, string tokenType)
        {
            var ex = new NotFoundException(10004, "Token not found.");
            _tokenNotFound(logger, tokenType, ex);
            return ex;
        }
        
        public static Exception FavoritesLengthExceeded(this ILogger logger, string userId, string favoriteType)
        {
            var ex = new Exception(10005, "Favorites length exceeded.");
            _favoritesLengthExceeded(logger, userId, favoriteType, ex);
            return ex;
        }
    }
}
