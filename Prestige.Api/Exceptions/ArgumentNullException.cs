namespace Prestige.Api.Exceptions
{
    public class ArgumentNullException : Exception
    {
        public ArgumentNullException(int eventId, string message) : base(eventId, message) { }
    }
}
