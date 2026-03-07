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

                    //   Iterate through your data and generate appropriate insert commands
                    ChessGame game;
                    //foreach (ChessGame game in cgs)
                    for (int i = 0; i < cgs.Count; i++)
                    {
                        game = cgs[i];
                        MySqlCommand command = conn.CreateCommand();

                        // Batched insert commands
                        command.CommandText = 
                            "INSERT INTO Players (Name, Elo) VALUES (@wName, @wElo)" +
                            " ON DUPLICATE KEY UPDATE Elo = IF(@wElo > Elo, @wElo, Elo);" +
                            "INSERT INTO Players (Name, Elo) VALUES (@bName, @bElo)" +
                            " ON DUPLICATE KEY UPDATE Elo = IF(@bElo > Elo, @bElo, Elo);" +
                            "INSERT INTO Events (Name, Site, Date) VALUES (@eName, @site, @Date);" +
                            "INSERT INTO Games VALUES" +
                            " ((SELECT pID FROM Players WHERE Name = @wName)," +
                            " (SELECT pID FROM Players WHERE Name = @bName)," +
                            " (SELECT eID FROM Events WHERE Name = @eName AND Site = @site AND Date = @date)," +
                            " @round, @result, @moves);";

                        
                        // White Player params
                        command.Parameters.AddWithValue("@wName", game.WhitePlayer);
                        command.Parameters.AddWithValue("@wElo", game.WhiteElo);

                        // Black Player params
                        command.Parameters.AddWithValue("@bName", game.BlackPlayer);
                        command.Parameters.AddWithValue("@bElo", game.BlackElo);

                        // Event params
                        command.Parameters.AddWithValue("@eName", game.EventName);
                        command.Parameters.AddWithValue("@site", game.Site);
                        command.Parameters.AddWithValue("@Date", game.EventDate);

                        // Game params
                        command.Parameters.AddWithValue("@round", game.Round);
                        command.Parameters.AddWithValue("@result", game.Result);
                        command.Parameters.AddWithValue("@moves", game.Moves);

                        command.ExecuteNonQuery();

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

                    MySqlCommand command = conn.CreateCommand();

                    // Select from temp table that gives all needed data from
                    // Events, Players, and Games per Game
                    StringBuilder sql = new StringBuilder("SELECT * FROM" +
                        "(SELECT w.Name AS wName, b.Name AS bName,w.Elo AS wElo, b.Elo AS bElo," +
                        " e.Name AS eName, e.Date, e.Site, g.Result, g.Moves FROM" +
                        " Games AS g JOIN Players AS w ON w.pID = WhitePlayer" +
                        " JOIN Players AS b ON b.pID = BlackPlayer" +
                        " JOIN Events AS e ON g.eID = e.eID) AS gameData" +
                        " WHERE TRUE");

                    // If White Player search box is not empty
                    if (!string.IsNullOrWhiteSpace(white))
                    {
                        sql.Append(" AND wName = @white");
                        command.Parameters.AddWithValue("@white", white);
                    }

                    // If Black Player search box is not empty
                    if (!string.IsNullOrWhiteSpace(black))
                    {
                        sql.Append(" AND bName = @black");
                        command.Parameters.AddWithValue("@black", black);
                    }

                    // If Opening Move search box is not empty
                    if (!string.IsNullOrWhiteSpace(opening))
                    {
                        sql.Append(" AND Moves Like @moves");
                        command.Parameters.AddWithValue("@moves", opening + "%");
                    }

                    // If Winner search box is not selected as Any
                    if (!string.IsNullOrWhiteSpace(winner))
                    {
                        sql.Append(" AND Result = @winner");
                        command.Parameters.AddWithValue("@winner", winner);
                    }

                    // If Filter By Date is selected
                    if (useDate)
                    {
                        sql.Append(" AND Date >= @start AND Date <= @end");
                        command.Parameters.AddWithValue("@start", start);
                        command.Parameters.AddWithValue("@end", end);
                    }

                    command.CommandText = sql.ToString();

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        StringBuilder queryData = new StringBuilder();
                        while (reader.Read())
                        {
                            // Add column data in row to string in correct format
                            queryData.Append("Event: " + reader["eName"] + "\n");
                            queryData.Append("Site: " + reader["Site"] + "\n");
                            // If server throws exception when reading date, add 00/00/0000 in date's place
                            try
                            {
                                queryData.Append("Date: " + reader.GetDateTime("Date").ToString("MM/dd/yyyy") + "\n");
                            }
                            catch
                            {
                                queryData.Append("Date: 00/00/0000 \n");
                            }
                            queryData.Append("White: " + reader["wName"] + " (" + reader["wElo"] + ")\n");
                            queryData.Append("Black: " + reader["bName"] + " (" + reader["bElo"] + ")\n");
                            queryData.Append("Result: " + reader["Result"] + "\n");
                            if (showMoves)
                                queryData.Append(reader["Moves"] + "\n");
                            queryData.Append("\n");

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

                        Console.WriteLine("Event: " + game.EventName);
                        Console.WriteLine("Site: " + game.Site);
                        Console.WriteLine("Date: " + game.EventDate);
                        Console.WriteLine("ROund: " + game.Round);
                        Console.WriteLine("White: " + game.WhitePlayer + " (" + game.WhiteElo + ")");
                        Console.WriteLine("Black: " + game.BlackPlayer + " (" + game.BlackElo + ")");
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