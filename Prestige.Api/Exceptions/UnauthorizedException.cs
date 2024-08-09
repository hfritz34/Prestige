namespace Prestige.Api.Exceptions
{
	public class UnauthorizedException : Exception
	{
		public UnauthorizedException(int eventId, string message) : base(eventId, message) { }
	}
}