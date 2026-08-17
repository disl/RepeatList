using Microsoft.Data.Sqlite;
using RepeatList.Models;
using System.Globalization;


namespace RepeatList.Services
{
    public class DatabaseService
    {
        // Serialisiert alle Zugriffe auf die eine SqliteConnection (nicht thread-sicher bei paralleler Nutzung).
        private readonly SemaphoreSlim _gate = new(1, 1);
        private string? _connectionString;
        private SqliteConnection? _connection;

        public DatabaseService()
        {
            // FileSystem.AppDataDirectory wird NICHT mehr im Konstruktor ausgelesen: Beim sehr
            // frühen XAML-Load (ResourcesViewModel als Shell.BindingContext) ist der MAUI-Platform-
            // Kontext teils noch nicht bereit und FileSystem.AppDataDirectory kann sporadisch eine
            // NullReferenceException werfen. Der Pfad wird stattdessen lazy beim erstmaligen
            // Öffnen der Verbindung ermittelt (dort ist der Kontext garantiert verfügbar).
        }

        // Ermittelt den lokalen DB-Pfad lazy — erst wenn eine Verbindung tatsächlich nötig ist.
        private string ConnectionString
        {
            get
            {
                if (_connectionString == null)
                {
                    var localDbPath = Path.Combine(FileSystem.AppDataDirectory, "todo.db3");
                    _connectionString = $"Data Source={localDbPath}";
                }
                return _connectionString;
            }
        }

        // Öffnet die Verbindung + legt das Schema lazy und auf einem Hintergrund-Thread an.
        private async Task<SqliteConnection> GetConnectionAsync()
        {
            if (_connection != null)
                return _connection;

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_connection == null)
                {
                    _connection = await Task.Run(() =>
                    {
                        var conn = new SqliteConnection(ConnectionString);
                        conn.Open();

                        // Tabellen erstellen, falls sie nicht existieren
                        var command = conn.CreateCommand();
                        command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Header (
                            Id TEXT PRIMARY KEY,
                            ListName TEXT NOT NULL,
                            UpdatedAt TEXT NOT NULL,
                            IsSynchronized INTEGER DEFAULT 0
                        );
                        CREATE TABLE IF NOT EXISTS Position (
                            Id TEXT PRIMARY KEY,
                            HeaderId TEXT NOT NULL,
                            Title TEXT NOT NULL,
                            IsCompleted INTEGER DEFAULT 0,
                            UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY (HeaderId) REFERENCES Header(Id)
                        );
                        CREATE TABLE IF NOT EXISTS Setup(
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            DefaultLanguage TEXT NOT NULL DEFAULT 'en',
                            DefaultAppTheme TEXT NOT NULL DEFAULT 'Dark'
                        );
                        CREATE TABLE IF NOT EXISTS CategoryPosition(
                            Position TEXT PRIMARY KEY,
                            Category TEXT NOT NULL
                        ); ";
                        command.ExecuteNonQuery();
                        return conn;
                    }).ConfigureAwait(false);
                }
            }
            finally
            {
                _gate.Release();
            }

            return _connection;
        }

        // Führt die DB-Arbeit serialisiert auf einem Thread-Pool-Thread aus.
        // Die Continuation des Aufrufers läuft weiterhin auf dessen SynchronizationContext
        // (bei UI-Handlern = Main-Thread), da hier kein ConfigureAwait(false) verwendet wird.
        private async Task<T> RunExclusiveAsync<T>(Func<SqliteConnection, Task<T>> work)
        {
            var connection = await GetConnectionAsync().ConfigureAwait(false);
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                return await Task.Run(() => work(connection)).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        void AddColumnIfNotExists(string tableName, string columnName, string columnType, SqliteConnection? connection, string Default = "")
        {
            string checkColumnQuery = $"PRAGMA table_info({tableName});";
            using var command = new SqliteCommand(checkColumnQuery, connection);
            using var reader = command.ExecuteReader();

            bool columnExists = false;
            while (reader.Read())
            {
                if (reader["name"].ToString() == columnName)
                {
                    columnExists = true;
                    break;
                }
            }

            if (!columnExists)
            {
                string alterTableQuery = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType} " + Default + ";";
                using var alterCommand = new SqliteCommand(alterTableQuery, connection);
                alterCommand.ExecuteNonQuery();
            }
        }

        // Header CRUD
        public async Task<List<Header>> GetHeadersAsync()
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var headers = new List<Header>();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Header ORDER BY ListName";

                try
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var _id = reader.GetString(0);
                            var _listName = reader.GetString(1);
                            var _updatedAt = reader.GetString(2);
                            var _IsSynchronized = reader.GetBoolean(3);

                            headers.Add(new Header
                            {
                                Id = _id,
                                ListName = _listName,
                                UpdatedAt = DateTime.Parse(_updatedAt),
                                IsSynchronized = _IsSynchronized
                            });
                        }
                    }

                    return headers;
                }
                catch (Exception ex)
                {
                    if (ex != null)
                        SentrySdk.CaptureException(ex);
                    return headers;
                }
            });
        }

        public async Task<Header?> GetHeaderAsync(string Id)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var headers = new List<Header>();
                var header = new Header();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Header WHERE Id=@Id";
                command.Parameters.AddWithValue("@Id", Id);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        headers.Add(new Header
                        {
                            Id = reader.GetString(0),
                            ListName = reader.GetString(1),
                            UpdatedAt = DateTime.Parse(reader.GetString(2)),
                            IsSynchronized = reader.GetBoolean(3)
                        });
                    }
                }

                if (headers != null && headers.Count == 1)
                    header = headers[0];
                else header = null;

                return header;
            });
        }

        public async Task<string> AddHeaderAsync(Header header, string? old_guid)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                Guid new_guid;

                if (old_guid == null || old_guid == Guid.Empty.ToString())
                    new_guid = Guid.NewGuid();
                else
                    new_guid = Guid.Parse(old_guid);

                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Header (Id, ListName, UpdatedAt,IsSynchronized) VALUES (@Id, @ListName, @UpdatedAt,@IsSynchronized)";
                command.Parameters.AddWithValue("@Id", new_guid.ToString());
                command.Parameters.AddWithValue("@ListName", header.ListName);
                command.Parameters.AddWithValue("@UpdatedAt", header.UpdatedAt.ToString("u"));
                command.Parameters.AddWithValue("@IsSynchronized", header.IsSynchronized);
                await command.ExecuteNonQueryAsync();

                // Die letzte eingefügte ID abrufen
                var idCommand = connection.CreateCommand();
                idCommand.CommandText = "SELECT last_insert_rowid()";
                var newId_obj = await idCommand.ExecuteScalarAsync();
                if (newId_obj != DBNull.Value)
                    return new_guid.ToString();
                else
                    return Guid.Empty.ToString();
            });
        }

        public async Task<int> DeleteHeaderAsync(string id)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Header WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);

                return await command.ExecuteNonQueryAsync();
            });
        }

        public async Task<int> EditHeadersTitleAsync(Header header, string new_list_name)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Header SET ListName=@ListName WHERE Id=@Id";
                command.Parameters.AddWithValue("@Id", header.Id);
                command.Parameters.AddWithValue("@ListName", new_list_name);

                return await command.ExecuteNonQueryAsync();
            });
        }

        public async Task<int> EditHeadersIsSynchronizedAsync(Header header, bool IsSynchronized)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Header SET IsSynchronized=@IsSynchronized WHERE Id=@Id";
                command.Parameters.AddWithValue("@Id", header.Id);
                command.Parameters.AddWithValue("@IsSynchronized", IsSynchronized);

                return await command.ExecuteNonQueryAsync();
            });
        }



        // Position CRUD
        public async Task<List<Position>> GetPositionsAsync(string headerId)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                if (headerId == Guid.Empty.ToString())
                    return new List<Position>();

                var positions = new List<Position>();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Position WHERE HeaderId = @HeaderId Order By IsCompleted DESC, Title";
                command.Parameters.AddWithValue("@HeaderId", headerId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        positions.Add(new Position
                        {
                            Id = reader.GetString(0),
                            HeaderId = reader.GetString(1),
                            Title = reader.GetString(2),
                            IsCompleted = reader.GetBoolean(3),
                            UpdatedAt = reader.IsDBNull(4)
                                ? (DateTime?)null
                                : DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture,
                                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
                        });
                    }
                }
                return positions;
            });
        }

        public async Task<string> AddPositionAsync(Position position, bool generate_new_guid = true)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                string? new_guid = null;

                if (generate_new_guid)
                    new_guid = Guid.NewGuid().ToString();
                else
                    new_guid = position.Id;

                var command = connection.CreateCommand();
                command.CommandText =
                    "INSERT INTO Position (Id,   HeaderId,  Title,  IsCompleted,  UpdatedAt) " +
                    "VALUES               (@Id, @HeaderId, @Title, @IsCompleted, @UpdatedAt)";
                command.Parameters.AddWithValue("@Id", new_guid);
                command.Parameters.AddWithValue("@HeaderId", position.HeaderId);
                command.Parameters.AddWithValue("@Title", position.Title);
                command.Parameters.AddWithValue("@IsCompleted", position.IsCompleted);
                var updatedAt = position.UpdatedAt ?? DateTime.UtcNow;
                command.Parameters.AddWithValue("@UpdatedAt", updatedAt);

                await command.ExecuteNonQueryAsync();
                return new_guid;
            });
        }

        public async Task<int> UpdatePositionAsync(Position position)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                if (position == null || position.Id == Guid.Empty.ToString())
                    return 0;

                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Position SET Title = @Title, IsCompleted = @IsCompleted, UpdatedAt=@UpdatedAt WHERE Id = @Id";
                command.Parameters.AddWithValue("@Title", position.Title);
                command.Parameters.AddWithValue("@IsCompleted", position.IsCompleted);
                command.Parameters.AddWithValue("@Id", position.Id);
                var updatedAt = position.UpdatedAt ?? DateTime.UtcNow;
                command.Parameters.AddWithValue("@UpdatedAt", updatedAt);

                var ret_val = await command.ExecuteNonQueryAsync();

                return ret_val;
            });
        }

        public async Task<int> DeletePositionAsync(string id)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Position WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);

                return await command.ExecuteNonQueryAsync();
            });
        }

        public async Task<int> DeletePositionsByHeaderIdAsync(string HeaderId)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Position WHERE  HeaderId = @HeaderId";
                command.Parameters.AddWithValue("@HeaderId", HeaderId);

                return await command.ExecuteNonQueryAsync();
            });
        }

        public async Task<int> EditPositionsTitleAsync(Position position, string new_title)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Position SET Title=@title WHERE Id=@Id";
                command.Parameters.AddWithValue("@Id", position.Id);
                command.Parameters.AddWithValue("@title", new_title);

                return await command.ExecuteNonQueryAsync();
            });
        }

        public async Task<int> UpdateIsCompletedPositionsAsync(string HeaderId, bool IsCompleted)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Position SET IsCompleted=@IsCompleted WHERE  HeaderId = @HeaderId";
                command.Parameters.AddWithValue("@HeaderId", HeaderId);
                command.Parameters.AddWithValue("@IsCompleted", IsCompleted);

                return await command.ExecuteNonQueryAsync();
            });
        }

        public async Task<int> UpdateIsSynchronizedHeaderAsync(string HeaderId, bool IsSynchronized)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Header SET IsSynchronized=@IsSynchronized WHERE  Id = @HeaderId";
                command.Parameters.AddWithValue("@HeaderId", HeaderId);
                command.Parameters.AddWithValue("@IsSynchronized", IsSynchronized);

                return await command.ExecuteNonQueryAsync();
            });
        }

        #region SETUP


        // Setup CRUD
        public async Task<List<Setup>> GetSetupsAsync()
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var Setups = new List<Setup>();

                var command = connection.CreateCommand();
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
            });
        }

        public async Task<Setup?> GetSetupAsync(int Id)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var Setups = new List<Setup>();
                var Setup = new Setup();

                var command = connection.CreateCommand();
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
            });
        }

        // Altlast aus der alten Version bereinigen: Mehrere parallele Load()-Aufrufe beim
        // App-Start haben früher doppelte Setup-Datensätze angelegt. Dieses DELETE behält
        // nur den ersten (kleinste Id) und entfernt alle Duplikate. Idempotent: Bei 0 oder
        // 1 Datensätzen ist die Zahl der betroffenen Zeilen 0.
        public async Task<int> DeduplicateSetupsAsync()
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Setup WHERE Id != (SELECT MIN(Id) FROM Setup)";
                return await command.ExecuteNonQueryAsync();
            });
        }

        public async Task<int> AddSetupAsync(Setup Setup)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Setup (DefaultLanguage, DefaultAppTheme) VALUES (@DefaultLanguage, @DefaultAppTheme)";
                command.Parameters.AddWithValue("@DefaultLanguage", Setup.DefaultLanguage);
                command.Parameters.AddWithValue("@DefaultAppTheme", Setup.DefaultAppTheme);
                await command.ExecuteNonQueryAsync();

                // Die letzte eingefügte ID abrufen
                var idCommand = connection.CreateCommand();
                idCommand.CommandText = "SELECT last_insert_rowid()";
                var newId_obj = await idCommand.ExecuteScalarAsync();
                if (newId_obj != DBNull.Value)
                    return Convert.ToInt32(newId_obj);
                else
                    return -1;
            });
        }

        public async Task<int> DeleteSetupAsync(int id)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Setup WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);

                return await command.ExecuteNonQueryAsync();
            });
        }

        public async Task<int> UpdateSetupAsync(Setup position)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                if (position == null || position.Id == 0)
                    return 0;

                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Setup SET DefaultLanguage = @DefaultLanguage, DefaultAppTheme = @DefaultAppTheme WHERE Id = @Id";
                command.Parameters.AddWithValue("@DefaultLanguage", position.DefaultLanguage);
                command.Parameters.AddWithValue("@DefaultAppTheme", position.DefaultAppTheme);
                command.Parameters.AddWithValue("@Id", position.Id);

                var ret_val = await command.ExecuteNonQueryAsync();

                return ret_val;
            });
        }

        #endregion


        #region CategoryPosition


        public async Task<List<CategoryPosition>> GetCategoryPositionsAsync()
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var CategoryPositions = new List<CategoryPosition>();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM CategoryPosition";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        CategoryPositions.Add(new CategoryPosition(
                            reader.GetString(0),
                            reader.GetString(1)
                        ));
                    }
                }
                return CategoryPositions;
            });
        }

        public async Task<CategoryPosition?> GetCategoryPositionAsync(string Position)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var CategoryPositions = new List<CategoryPosition>();
                var CategoryPosition = new CategoryPosition();

                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM CategoryPosition WHERE Position=@Position";
                    command.Parameters.AddWithValue("@Position", Position);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            CategoryPositions.Add(new CategoryPosition
                            (
                                reader.GetString(0),
                                reader.GetString(1)
                            ));
                        }
                    }

                    if (CategoryPositions != null && CategoryPositions.Count == 1)
                        CategoryPosition = CategoryPositions[0];
                    else CategoryPosition = null;

                    return CategoryPosition;
                }
                catch (Exception ex)
                {
                    return null;
                }
            });
        }

        public async Task<int> AddCategoryPositionAsync(CategoryPosition CategoryPosition)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO CategoryPosition (Position, Category) VALUES (@Position, @Category)";
                    command.Parameters.AddWithValue("@Position", CategoryPosition.Position);
                    command.Parameters.AddWithValue("@Category", CategoryPosition.Category);
                    await command.ExecuteNonQueryAsync();

                    // Die letzte eingefügte ID abrufen
                    var idCommand = connection.CreateCommand();
                    idCommand.CommandText = "SELECT last_insert_rowid()";
                    var newId_obj = await idCommand.ExecuteScalarAsync();
                    if (newId_obj != DBNull.Value)
                        return Convert.ToInt32(newId_obj);
                    else
                        return -1;
                }
                catch (Exception ex)
                {
                    return -1;
                }
            });
        }

        public async Task<int> DeleteCategoryPositionAsync(string position)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM CategoryPosition WHERE Position = @Position";
                command.Parameters.AddWithValue("@Position", position);

                return await command.ExecuteNonQueryAsync();
            });
        }

        public async Task<int> UpdateCategoryPositionAsync(CategoryPosition position)
        {
            return await RunExclusiveAsync(async (connection) =>
            {
                if (position == null || string.IsNullOrEmpty(position.Position))
                    return 0;

                var command = connection.CreateCommand();
                command.CommandText = "UPDATE CategoryPosition SET Category = @Category WHERE Position = @Position";
                command.Parameters.AddWithValue("@Position", position.Position);
                command.Parameters.AddWithValue("@Category", position.Category);

                var ret_val = await command.ExecuteNonQueryAsync();

                return ret_val;
            });
        }

        #endregion
    }
}
