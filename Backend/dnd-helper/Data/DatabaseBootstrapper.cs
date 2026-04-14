using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Data;

public static class DatabaseBootstrapper
{
    public static async Task RecreateIfSchemaOutdatedAsync(AppDbContext dbContext)
    {
        var connectionString = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) || !File.Exists(builder.DataSource))
        {
            return;
        }

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var requiredSchema = new Dictionary<string, string[]>
        {
            ["rooms"] = ["invite_token", "active_member_user_id", "session_updated_at_utc"],
            ["room_memberships"] = ["role", "last_seen_at_utc"]
        };

        foreach (var (tableName, requiredColumns) in requiredSchema)
        {
            if (!await TableExistsAsync(connection, tableName))
            {
                await connection.CloseAsync();
                await dbContext.Database.EnsureDeletedAsync();
                return;
            }

            var existingColumns = await GetColumnsAsync(connection, tableName);
            if (requiredColumns.Any(requiredColumn => !existingColumns.Contains(requiredColumn)))
            {
                await connection.CloseAsync();
                await dbContext.Database.EnsureDeletedAsync();
                return;
            }
        }
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName;";
        command.Parameters.AddWithValue("$tableName", tableName);
        var result = (long)(await command.ExecuteScalarAsync() ?? 0L);
        return result > 0;
    }

    private static async Task<HashSet<string>> GetColumnsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }
}
