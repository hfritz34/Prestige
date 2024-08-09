namespace Prestige.Api.Exceptions
{
    public class Exception : System.Exception
    {
        public Exception() : base() { }

        public Exception(int eventId, string message) : base(message)
        {
            Data.Add("eventId", eventId);
        }

        public Exception(int eventId, string message, System.Exception inner) : base(message, inner)
        {
            Data.Add("eventId", eventId);
        }
    }
}
