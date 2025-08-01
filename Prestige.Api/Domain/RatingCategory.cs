namespace Prestige.Api.Domain
{
    public class RatingCategory
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public decimal MinScore { get; private set; }
        public decimal MaxScore { get; private set; }
        public string ColorHex { get; private set; }
        public int DisplayOrder { get; private set; }

        private RatingCategory() { }

        public RatingCategory(string name, decimal minScore, decimal maxScore, string colorHex, int displayOrder)
        {
            Name = name;
            MinScore = minScore;
            MaxScore = maxScore;
            ColorHex = colorHex;
            DisplayOrder = displayOrder;
        }
    }
}