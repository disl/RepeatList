using Microsoft.Data.Sqlite;
using RepeatList.Models;


namespace RepeatList.Services
{


    public class DatabaseService
    {
        private SqliteConnection _connection;

        public DatabaseService()
        {
            var localDbPath = Path.Combine(FileSystem.AppDataDirectory, "todo.db3");
            var connectionString = $"Data Source={localDbPath}";
            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            // Tabellen erstellen, falls sie nicht existieren
            var command = _connection.CreateCommand();
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Header (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ListName TEXT NOT NULL,
                Date TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Position (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                HeaderId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                IsCompleted INTEGER DEFAULT 0,
                FOREIGN KEY (HeaderId) REFERENCES Header(Id)
            );
            CREATE TABLE IF NOT EXISTS Setup(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DefaultLanguage TEXT NOT NULL DEFAULT 'en-US',
                DefaultAppTheme TEXT NOT NULL DEFAULT 'Dark'
            );"
            ;
            command.ExecuteNonQuery();

        }

        // Header CRUD
        public async Task<List<Header>> GetHeadersAsync()
        {
            var headers = new List<Header>();

            var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Header ORDER BY ListName";  //Date DESC";

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    headers.Add(new Header
                    {
                        Id = reader.GetInt32(0),
                        ListName = reader.GetString(1),
                        Date = DateTime.Parse(reader.GetString(2))
                    });
                }
            }

            return headers;
        }

        public async Task<Header?> GetHeaderAsync(int Id)
        {
            var headers = new List<Header>();
            var header = new Header();

            var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Header WHERE Id=@Id";
            command.Parameters.AddWithValue("@Id", Id);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    headers.Add(new Header
                    {
                        Id = reader.GetInt32(0),
                        ListName = reader.GetString(1),
                        Date = DateTime.Parse(reader.GetString(2))
                    });
                }
            }

            if (headers != null && headers.Count == 1)
                header = headers[0];
            else header = null;

            return header;
        }

        public async Task<int> AddHeaderAsync(Header header)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Header (ListName, Date) VALUES (@ListName, @Date)";
            command.Parameters.AddWithValue("@ListName", header.ListName);
            command.Parameters.AddWithValue("@Date", header.Date.ToString("u"));
            await command.ExecuteNonQueryAsync();

            // Die letzte eingefügte ID abrufen
            var idCommand = _connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid()";
            var newId_obj = await idCommand.ExecuteScalarAsync();
            if (newId_obj != DBNull.Value)
                return Convert.ToInt32(newId_obj);
            else
                return -1;
        }

        public async Task<int> DeleteHeaderAsync(int id)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM Header WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync();
        }

        // Position CRUD
        public async Task<List<Position>> GetPositionsAsync(int headerId)
        {
            if (headerId == 0)
                return new List<Position>();

            var positions = new List<Position>();

            var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Position WHERE HeaderId = @HeaderId Order By IsCompleted DESC, Title";
            command.Parameters.AddWithValue("@HeaderId", headerId);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    positions.Add(new Position
                    {
                        Id = reader.GetInt32(0),
                        HeaderId = reader.GetInt32(1),
                        Title = reader.GetString(2),
                        IsCompleted = reader.GetBoolean(3)
                    });
                }
            }

            return positions;
        }

        public async Task<int> AddPositionAsync(Position position)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Position (HeaderId, Title, IsCompleted) VALUES (@HeaderId, @Title, @IsCompleted)";
            command.Parameters.AddWithValue("@HeaderId", position.HeaderId);
            command.Parameters.AddWithValue("@Title", position.Title);
            command.Parameters.AddWithValue("@IsCompleted", position.IsCompleted);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdatePositionAsync(Position position)
        {
            if (position == null || position.Id==0)
                return 0;

            var command = _connection.CreateCommand();
            command.CommandText = "UPDATE Position SET Title = @Title, IsCompleted = @IsCompleted WHERE Id = @Id";
            command.Parameters.AddWithValue("@Title", position.Title);
            command.Parameters.AddWithValue("@IsCompleted", position.IsCompleted);
            command.Parameters.AddWithValue("@Id", position.Id);

            var ret_val = await command.ExecuteNonQueryAsync();

            //await GetPositionsAsync(position.HeaderId);

            return ret_val;
        }

        public async Task<int> DeletePositionAsync(int id)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM Position WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> DeletePositionsByHeaderIdAsync(int HeaderId)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM Position WHERE  HeaderId = @HeaderId";
            command.Parameters.AddWithValue("@HeaderId", HeaderId);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> EditHeadersTitleAsync(Header header, string new_list_name)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "UPDATE Header SET ListName=@ListName WHERE Id=@Id";
            command.Parameters.AddWithValue("@Id", header.Id);
            command.Parameters.AddWithValue("@ListName", new_list_name);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> EditPositionsTitleAsync(Position position, string new_title)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "UPDATE Position SET Title=@title WHERE Id=@Id";
            command.Parameters.AddWithValue("@Id", position.Id);
            command.Parameters.AddWithValue("@title", new_title);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdateIsCompletedPositionsAsync(int HeaderId, bool IsCompleted)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "UPDATE Position SET IsCompleted=@IsCompleted WHERE  HeaderId = @HeaderId";
            command.Parameters.AddWithValue("@HeaderId", HeaderId);
            command.Parameters.AddWithValue("@IsCompleted", IsCompleted);

            return await command.ExecuteNonQueryAsync();
        }

        #region SETUP


        // Header CRUD
        public async Task<List<Setup>> GetSetupsAsync()
        {
            var Setups = new List<Setup>();

            var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Setup LIMIT 1"; 

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Setups.Add(new Setup
                    {
                        Id = reader.GetInt32(0),
                        DefaultLanguage = reader.GetString(1),
                        DefaultAppTheme = reader.GetString(2)
                    });
                }
            }
            return Setups;
        }

        public async Task<Setup?> GetSetupAsync(int Id)
        {
            var Setups = new List<Setup>();
            var Setup = new Setup();

            var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM Setup WHERE Id=@Id";
            command.Parameters.AddWithValue("@Id", Id);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Setups.Add(new Setup
                    {
                        Id = reader.GetInt32(0),
                        DefaultLanguage = reader.GetString(1),
                        DefaultAppTheme = reader.GetString(2)
                    });
                }
            }

            if (Setups != null && Setups.Count == 1)
                Setup = Setups[0];
            else Setup = null;

            return Setup;
        }

        public async Task<int> AddSetupAsync(Setup Setup)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Setup (DefaultLanguage, DefaultAppTheme) VALUES (@DefaultLanguage, @DefaultAppTheme)";
            command.Parameters.AddWithValue("@DefaultLanguage", Setup.DefaultLanguage);
            command.Parameters.AddWithValue("@DefaultAppTheme", Setup.DefaultAppTheme);
            await command.ExecuteNonQueryAsync();

            // Die letzte eingefügte ID abrufen
            var idCommand = _connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid()";
            var newId_obj = await idCommand.ExecuteScalarAsync();
            if (newId_obj != DBNull.Value)
                return Convert.ToInt32(newId_obj);
            else
                return -1;
        }

        public async Task<int> DeleteSetupAsync(int id)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM Setup WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdateSetupAsync(Setup position)
        {
            if (position == null || position.Id==0)
                return 0;

            var command = _connection.CreateCommand();
            command.CommandText = "UPDATE Setup SET DefaultLanguage = @DefaultLanguage, DefaultAppTheme = @DefaultAppTheme WHERE Id = @Id";
            command.Parameters.AddWithValue("@DefaultLanguage", position.DefaultLanguage);
            command.Parameters.AddWithValue("@DefaultAppTheme", position.DefaultAppTheme);
            command.Parameters.AddWithValue("@Id", position.Id);

            var ret_val = await command.ExecuteNonQueryAsync();

            //await GetPositionsAsync(position.HeaderId);

            return ret_val;
        }

        #endregion
    }
}
