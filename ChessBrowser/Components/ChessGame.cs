namespace ChessBrowser.Components
#nullable disable
{
    /// <summary>
    /// Represents the data from a single Chess game in a PGN file.
    /// Only collects the data we need for the database.
    /// </summary>
    public class ChessGame
    {
        /// <summary>
        /// Name of the event a chess game is held at.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Name of the site a chess event is held at.
        /// </summary>
        public string Site { get; set; }

        /// <summary>
        /// Start date of the chess event.
        /// </summary>
        public string EventDate { set; get; }

        /// <summary>
        /// The round of the chess game.
        /// </summary>
        public string Round { get; set; }

        /// <summary>
        /// The white player for the chess game.
        /// </summary>
        public string WhitePlayer { get; set; }

        /// <summary>
        /// The black player for the chess game.
        /// </summary>
        public string BlackPlayer { get; set; }

        /// <summary>
        /// The white player's elo.
        /// </summary>
        public int WhiteElo { set; get; }

        /// <summary>
        /// The black player's elo.
        /// </summary>
        public int BlackElo { set; get; }

        /// <summary>
        /// The result of the chess game. Win, loss, or draw.
        /// </summary>
        public char Result { set; get; }

        /// <summary>
        /// The moves for the chess game.
        /// </summary>
        public string Moves { set; get; }
    }
}