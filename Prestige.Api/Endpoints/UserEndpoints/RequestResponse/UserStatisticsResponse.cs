namespace Prestige.Api.Endpoints.UserEndpoints.RequestResponse
{
    public class UserStatisticsResponse
    {
        public int FriendsCount { get; set; }
        public int RatingsCount { get; set; }
        public int PrestigesCount { get; set; }
        
        public UserStatisticsResponse(int friendsCount, int ratingsCount, int prestigesCount)
        {
            FriendsCount = friendsCount;
            RatingsCount = ratingsCount;
            PrestigesCount = prestigesCount;
        }
    }
}