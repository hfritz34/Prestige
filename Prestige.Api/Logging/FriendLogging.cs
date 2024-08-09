using System;
using Microsoft.Extensions.Logging;
using Prestige.Api.Exceptions;
using ArgumentNullException = Prestige.Api.Exceptions.ArgumentNullException;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Logging
{
    public static class FriendLogging
    {
        private static readonly Action<ILogger, string, string, Exception> _friendshipNotFound;
        static FriendLogging()
        {
            _friendshipNotFound = LoggerMessage.Define<string, string>(
                LogLevel.Error,
                new EventId(30000, "UserTrackNotFound"),
                "Friendship not found: user id {userId}, friend id {friendId}."
            );
        }

        public static Exception FriendshipNotFound(this ILogger logger, string userId, string friendId)
        {
            var ex = new NotFoundException(30002, "User artist not found.");
            _friendshipNotFound(logger, userId, friendId, ex);
            return ex;
        }
    }
}
