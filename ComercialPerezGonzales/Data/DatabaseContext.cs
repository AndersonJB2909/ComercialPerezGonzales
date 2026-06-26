using Microsoft.Data.Sqlite;

namespace ComercialPerezGonzales.Data;

public class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(string dbPath)
    {
        _connectionString = $"Data Source={dbPath};";
    }

    public SqliteConnection CreateConnection() => new(_connectionString);
}
