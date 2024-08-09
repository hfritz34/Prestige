namespace Prestige.Functions.Interfaces
{
    public class PlayHistoryResponse
    {
        public string Href { get; set; }
        public List<PlayHistory> Items { get; set; }
        public Cursor Cursors { get; set; }
        public int Limit { get; set; }
        public string Next { get; set; }
    }

    public class Cursor
    {
        public string After { get; set; }
        public string Before { get; set; }
    }


}
