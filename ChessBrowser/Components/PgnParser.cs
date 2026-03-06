using System.Text;
using System.Text.RegularExpressions;

namespace ChessBrowser.Components
{
    /// <summary>
    /// Class for parsing PGN files to a list of ChessGame objects.
    /// </summary>
    public static class PgnParser
    {
        /// <summary>
        /// Function that parses data from the array of strings extracted from a PGN file. 
        /// </summary>
        /// <param name="PGNFileLines"> The lines from the PGN text file.</param>
        /// <returns></returns>
        public static List<ChessGame> parseData(string[] PGNFileLines)
        {
            List<ChessGame> games = new List<ChessGame>();
            ChessGame game = new ChessGame();
            int emptyLineCount = 0;           

            foreach (string line in PGNFileLines)
            {
                // When two empty lines have been read, its time for a the next game
                if (emptyLineCount == 2)
                {
                    games.Add(game);
                    game = new ChessGame();
                    emptyLineCount = 0;
                }

                // If line represents tagged data, it will be split into tag and value strings.
                // Otherwise the string doesn't split up
                string[] substrings = Regex.Split(line, " \"|\"]");
                if (substrings.Length > 1) {
                    string tag = substrings[0];
                    string val = substrings[1];

                    switch (tag)
                    {
                        case "[Event":
                            game.EventName = val;
                            break;
                        case "[Site":
                            game.Site = val;
                            break;
                        case "[Round":
                            game.Round = val;
                            break;
                        case "[White":
                            game.WhitePlayer = val;
                            break;
                        case "[Black":
                            game.BlackPlayer = val;
                            break;
                        case "[Result":
                            if (val == "1-0")
                                game.Result = 'W';
                            else if (val == "0-1")
                                game.Result = 'B';
                            else
                                game.Result = 'D';
                            break;
                        case "[WhiteElo":
                            game.WhiteElo = int.Parse(val);
                            break;
                        case "[BlackElo":
                            game.BlackElo = int.Parse(val);
                            break;
                        case "[EventDate":
                            game.EventDate = val;
                            break;
                        case "":
                            emptyLineCount++;
                            break;
                    } 
                }
                else
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        emptyLineCount++;
                    } else
                    {
                        StringBuilder sb = new StringBuilder(game.Moves);
                        game.Moves = sb.Append(line).ToString();
                    }
                }
            }

            return games;
        }
    }
}
