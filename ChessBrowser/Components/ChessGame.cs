namespace ChessBrowser.Components
#nullable disable
{
    public class ChessGame
    {
        public string EventName { get; set; }
        public string Site { get; set; }
        public string EventDate { set; get; }
        public string Round { get; set; }
        public string WhitePlayer { get; set; }
        public string BlackPlayer { get; set; }
        public int WhiteElo { set; get; }
        public int BlackElo { set; get; }
        public char Result { set; get; }
        public string Moves { set; get; }
    }
}