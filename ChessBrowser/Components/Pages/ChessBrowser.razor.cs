using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics;
using System.Text;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace ChessBrowser.Components.Pages
{
    public partial class ChessBrowser
    {
        /// <summary>
        /// Bound to the Unsername form input
        /// </summary>
        private string Username = "";

        /// <summary>
        /// Bound to the Password form input
        /// </summary>
        private string Password = "";

        /// <summary>
        /// Bound to the Database form input
        /// </summary>
        private string Database = "";

        /// <summary>
        /// Represents the progress percentage of the current
        /// upload operation. Update this value to update 
        /// the progress bar.
        /// </summary>
        private int Progress = 0;

        /// <summary>
        /// This method runs when a PGN file is selected for upload.
        /// Given a list of lines from the selected file, parses the 
        /// PGN data, and uploads each chess game to the user's database.
        /// </summary>
        /// <param name="PGNFileLines">The lines from the selected file</param>
        private async Task InsertGameData(string[] PGNFileLines)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've filled in the credentials in the GUI
            string connection = GetConnectionString();

            List<ChessGame> cgs = PgnParser.parseData(PGNFileLines);
            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // TODO:
                    //   Iterate through your data and generate appropriate insert commands

                    //foreach (ChessGame game in cgs)
                    for (int i = 0; i < cgs.Count; i++)
                    {
                        ChessGame game = cgs[i];

                        int whiteId = GetOrInsertPlayer(conn, game.WhitePlayer, game.WhiteElo);
                        int blackId = GetOrInsertPlayer(conn, game.BlackPlayer, game.BlackElo);
                        int eventId = GetOrInsertEvent(conn, game.EventName, game.Site, game.EventDate);
                        InsertGame(conn, game, whiteId, blackId, eventId);

                        Progress = (int)((i + 1) * 100.0 / cgs.Count);
                        await InvokeAsync(StateHasChanged);
                    }

                    // This tells the GUI to redraw after you update Progress (this should go inside your loop)
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        ///  find player or insert them
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="playerName"></param>
        /// <param name="playerElo"></param>
        /// <returns></returns>
        private int GetOrInsertPlayer(MySqlConnection conn, string playerName, int playerElo)
        {
            // assume player doesn't exist until its actually found
            int playerId = -1;
            int existingElo = 0;
            Debug.WriteLine("getting/inserting " + playerName + " Elo: " + playerElo);

            // check to see if the player already exists
            MySqlCommand command = conn.CreateCommand();
            command.CommandText = "SELECT pID, Elo FROM Players WHERE Name = @name";
            command.Parameters.AddWithValue("@name", playerName);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    playerId = Convert.ToInt32(reader["pID"]);
                    existingElo = Convert.ToInt32(reader["Elo"]);

                    Debug.WriteLine("Player exists ID = " + playerId + "  Elo = " + existingElo);
                }
            }

            // if the player is not already in, create a new player 
            if (playerId == -1)
            {
                Debug.WriteLine("inserting player" + playerName);
                MySqlCommand insertCommand = conn.CreateCommand();
                insertCommand.CommandText = "INSERT INTO Players (Name, Elo) VALUES (@name, @elo)";
                insertCommand.Parameters.AddWithValue("@name", playerName);
                insertCommand.Parameters.AddWithValue("@elo", playerElo);

                insertCommand.ExecuteNonQuery();
                Debug.WriteLine("Player inserted????????");

                MySqlCommand idCommand = conn.CreateCommand();
                idCommand.CommandText = "SELECT pID FROM Players WHERE Name = @name";
                idCommand.Parameters.AddWithValue("@name", playerName);

                using (MySqlDataReader reader = idCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        playerId = Convert.ToInt32(reader["pID"]);
                        Debug.WriteLine("New playerID:: " + playerId);
                    }
                }
            }
            // if player already exists, check if we need to update their elo 
            else if (playerElo > existingElo)
            {
                MySqlCommand updateCommand = conn.CreateCommand();
                updateCommand.CommandText = "UPDATE Players SET Elo = @elo WHERE pID = @id";
                updateCommand.Parameters.AddWithValue("@elo", playerElo);
                updateCommand.Parameters.AddWithValue("@id", playerId);

                updateCommand.ExecuteNonQuery();
            }

            Debug.WriteLine(playerId);
            return playerId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="eventName"></param>
        /// <param name="site"></param>
        /// <param name="eventDate"></param>
        /// <returns></returns>
        private int GetOrInsertEvent(MySqlConnection conn, string eventName, string site, string eventDate)
        {
            int eventId = -1;
            string normalizedDate = NormalizeDate(eventDate);

            // check for existing event
            MySqlCommand command = conn.CreateCommand();
            command.CommandText =
                "SELECT eID FROM Events WHERE Name = @name AND Site = @site AND Date = @eventDate";
            command.Parameters.AddWithValue("@name", eventName);
            command.Parameters.AddWithValue("@site", site);
            command.Parameters.AddWithValue("@eventDate", normalizedDate);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    eventId = Convert.ToInt32(reader["eID"]);
                }
            }

            // if the event was not found, insert it
            if (eventId == -1)
            {
                MySqlCommand insertCommand = conn.CreateCommand();
                insertCommand.CommandText =
                    "INSERT INTO Events (Name, Site, Date) VALUES (@name, @site, @eventDate)";
                insertCommand.Parameters.AddWithValue("@name", eventName);
                insertCommand.Parameters.AddWithValue("@site", site);
                insertCommand.Parameters.AddWithValue("@eventDate", normalizedDate);
                
                insertCommand.ExecuteNonQuery();

                // get the event using tis id
                MySqlCommand idCommand = conn.CreateCommand();
                idCommand.CommandText =
                    "SELECT eID FROM Events WHERE Name = @name AND Site = @site AND Date = @eventDate";
                idCommand.Parameters.AddWithValue("@name", eventName);
                idCommand.Parameters.AddWithValue("@site", site);
                idCommand.Parameters.AddWithValue("@eventDate", normalizedDate);

                using (MySqlDataReader reader = idCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        eventId = Convert.ToInt32(reader["eID"]);
                    }
                }
            }

            return eventId;
        }

        /// <summary>
        /// default date for dirty/partial dates
        /// </summary>
        /// <param name="rawDate"></param>
        /// <returns></returns>
        private string NormalizeDate(string rawDate)
        {
            if (!string.IsNullOrEmpty(rawDate) &&
                Regex.IsMatch(rawDate, @"^\d{4}\.\d{2}\.\d{2}$"))
            {
                return rawDate.Replace('.', '-');
            }

            return "0000-00-00";
        }

        private void InsertGame(MySqlConnection conn, ChessGame game, int whitePlayerId, int blackPlayerId, int eventId)
        {
            MySqlCommand command = conn.CreateCommand();
            command.CommandText =
                "INSERT INTO Games (WhitePlayer, BlackPlayer, eID, Round, Result, Moves) " +
                "VALUES (@whiteId, @blackId, @eventId, @round, @result, @moves)";

            command.Parameters.AddWithValue("@whiteId", whitePlayerId);
            command.Parameters.AddWithValue("@blackId", blackPlayerId);
            command.Parameters.AddWithValue("@eventId", eventId);
            command.Parameters.AddWithValue("@round", game.Round);
            command.Parameters.AddWithValue("@result", game.Result.ToString());
            command.Parameters.AddWithValue("@moves", game.Moves);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Queries the database for games that match all the given filters.
        /// The filters are taken from the various controls in the GUI.
        /// </summary>
        /// <param name="white">The white player, or "" if none</param>
        /// <param name="black">The black player, or "" if none</param>
        /// <param name="opening">The first move, e.g. "1.e4", or "" if none</param>
        /// <param name="winner">The winner as "W", "B", "D", or "" if none</param>
        /// <param name="useDate">true if the filter includes a date range, false otherwise</param>
        /// <param name="start">The start of the date range</param>
        /// <param name="end">The end of the date range</param>
        /// <param name="showMoves">true if the returned data should include the PGN moves</param>
        /// <returns>A string separated by newlines containing the filtered games</returns>
        private string PerformQuery(string white, string black, string opening,
            string winner, bool useDate, DateTime start, DateTime end, bool showMoves)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've typed a user and password in the GUI
            string connection = GetConnectionString();

            // Build up this string containing the results from your query
            string parsedResult = "";

            // Use this to count the number of rows returned by your query
            // (see below return statement)
            int numRows = 0;

            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // TODO:
                    //   Generate and execute an SQL command,
                    //   then parse the results into an appropriate string and return it.

                    MySqlCommand command = conn.CreateCommand();

                    //StringBuilder sql = new StringBuilder("SELECT g.Result, e.Name as eName, e.Date, e.Site FROM " +
                    //    "Players JOIN Games as g NATURAL JOIN Events e Where True");
                    StringBuilder sql = new StringBuilder("SELECT w.Name AS wName, b.Name AS bName," +
                        " w.Elo AS wElo, b.Elo AS bElo, e.Name AS eName, e.Date, e.Site, g.Result FROM" +
                        " Games AS g JOIN Players AS w ON w.pID = WhitePlayer" +
                        " JOIN Players AS b ON b.pID = BlackPlayer" +
                        " JOIN Events AS e ON g.eID = e.eID");


                    if (!string.IsNullOrWhiteSpace(black) || !string.IsNullOrWhiteSpace(white))
                    {
                        sql.Insert(13, " Players Join");
                        
                        if (!string.IsNullOrWhiteSpace(white))
                        {

                            sql.Append(" AND WhitePlayer = @white");
                            command.Parameters.AddWithValue("@white", white);
                        }

                        if (!string.IsNullOrWhiteSpace(black))
                        {
                            sql.Append(" AND BlackPlayer = @black");
                            command.Parameters.AddWithValue("@black", black);
                        }
                    }
                    
                    if(!string.IsNullOrWhiteSpace(opening))
                    {
                        sql.Append(" And Moves Like @moves");
                        command.Parameters.AddWithValue("@moves", opening + "%");
                    }
                    
                    if(!string.IsNullOrWhiteSpace(winner))
                    {
                        sql.Append(" AND winner = @winner");
                        command.Parameters.AddWithValue("@winner", winner);
                    }
                    
                    if (useDate)
                    {
                        sql.Append("And Date ");
                    }
                    
                    if(showMoves){
                        // append moves
                    }

                    command.CommandText = sql.ToString();

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        StringBuilder queryData = new StringBuilder();
                        while (reader.Read())
                        {
                            queryData.Append("Event: " + reader["eName"]+ "\n");
                            queryData.Append("Site: " + reader["Site"] + "\n");
                            queryData.Append("Date: " + reader.GetDateTime("Date").ToString("MM/dd/yyyy") + "\n");
                            queryData.Append("White: " + reader["wName"] + " (" + reader["wElo"] + ")\n");
                            queryData.Append("Black: " + reader["bName"] + " (" + reader["bElo"] + ")\n");
                            queryData.Append("Result: " + reader["Result"]+ "\n\n");
                            numRows++;  
                        }
                        parsedResult = queryData.ToString();
                    }

                }
                
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    Debug.WriteLine(e.ToString()); //query bugs
                }
            }

            return numRows + " results\n\n" + parsedResult;
        }


        private string GetConnectionString()
        {
            return "server=atr.eng.utah.edu;database=" + Database + ";uid=" + Username + ";password=" + Password;
        }


        /// <summary>
        /// This method will run when the file chooser is used.
        /// It loads the files contents as an array of strings,
        /// then invokes the InsertGameData method.
        /// </summary>
        /// <param name="args">The event arguments, which contains the selected file name</param>
        private async void HandleFileChooser(EventArgs args)
        {
            try
            {
                string fileContent = string.Empty;

                InputFileChangeEventArgs eventArgs =
                    args as InputFileChangeEventArgs ?? throw new Exception("unable to get file name");
                if (eventArgs.FileCount == 1)
                {
                    var file = eventArgs.File;
                    if (file is null)
                    {
                        return;
                    }

                    // load the chosen file and split it into an array of strings, one per line
                    using var stream = file.OpenReadStream(1000000); // max 1MB
                    using var reader = new StreamReader(stream);
                    fileContent = await reader.ReadToEndAsync();
                    string[] fileLines =
                        fileContent.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                   
                    //TODO TESTING STUFF DELETE LATer
                    List<ChessGame> games = PgnParser.parseData(fileLines);

                    //Math.Min(1, games.Count)
                    for (int i = 0; i < 3; i++)
                    {
                        ChessGame game = games[i];

                        Console.WriteLine("Event: " +game.EventName);
                        Console.WriteLine("Site: " +game.Site);
                        Console.WriteLine("Date: " +game.EventDate);
                        Console.WriteLine("ROund: " + game.Round);
                        Console.WriteLine("White: " +game.WhitePlayer + " (" +game.WhiteElo + ")");
                        Console.WriteLine("Black: " +game.BlackPlayer + " (" +game.BlackElo + ")");
                        Console.WriteLine("Result: " + game.Result);
                        Console.WriteLine(game.Moves);
                    }
                    Console.WriteLine("Total count of parsed games: " + games.Count);
                    
                    // insert the games, and don't wait for it to finish
                    // _ = throws away the task result, since we aren't waiting for it
                    _ = InsertGameData(fileLines);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("an error occurred while loading the file..." + e);
            }
        }
    }
}