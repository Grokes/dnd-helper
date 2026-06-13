using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace dnd_helper.Infrastructure.Seeding;

public sealed class DatabaseInitializer(
    AppDbContext dbContext,
    RulesDatabaseSeeder rulesSeeder,
    ApplicationDatabaseSeeder applicationSeeder,
    IHostEnvironment environment,
    ILogger<DatabaseInitializer> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Initializing PostgreSQL schema...");
        await EnsurePostgresSchemaAsync(cancellationToken);
        await RemoveObsoletePostgresColumnsAsync(cancellationToken);
        await NormalizePostgresDataAsync(cancellationToken);

        logger.LogInformation("Initializing MongoDB rules catalog...");
        await rulesSeeder.SeedAsync(cancellationToken);

        logger.LogInformation("Initializing application seed data...");
        await applicationSeeder.SeedAsync(cancellationToken);

        logger.LogInformation("Database initialization completed.");
    }

    private async Task EnsurePostgresSchemaAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        var missingColumns = new List<string>();
        if (!await HasColumnAsync("room_memberships", "inventory_json", cancellationToken))
        {
            missingColumns.Add("room_memberships.inventory_json");
        }

        if (!await HasColumnAsync("characters", "subclass", cancellationToken))
        {
            missingColumns.Add("characters.subclass");
        }
        if (!await HasColumnAsync("characters", "weapon_damage", cancellationToken))
        {
            missingColumns.Add("characters.weapon_damage");
        }
        if (!await HasColumnAsync("characters", "spell_slots_json", cancellationToken))
        {
            missingColumns.Add("characters.spell_slots_json");
        }
        if (!await HasColumnAsync("characters", "max_hit_points", cancellationToken))
        {
            missingColumns.Add("characters.max_hit_points");
        }
        if (!await HasColumnAsync("characters", "current_hit_points", cancellationToken))
        {
            missingColumns.Add("characters.current_hit_points");
        }
        if (!await HasColumnAsync("characters", "spent_hit_dice", cancellationToken))
        {
            missingColumns.Add("characters.spent_hit_dice");
        }
        if (!await HasColumnAsync("characters", "spent_spell_slots_json", cancellationToken))
        {
            missingColumns.Add("characters.spent_spell_slots_json");
        }
        if (!await HasTableAsync("room_membership_characters", cancellationToken))
        {
            missingColumns.Add("room_membership_characters");
        }
        if (!await HasColumnAsync("encounter_combatants", "monster_slug", cancellationToken))
        {
            missingColumns.Add("encounter_combatants.monster_slug");
        }
        if (!await HasColumnAsync("encounter_combatants", "armor_class", cancellationToken))
        {
            missingColumns.Add("encounter_combatants.armor_class");
        }
        if (!await HasColumnAsync("encounter_combatants", "max_hit_points", cancellationToken))
        {
            missingColumns.Add("encounter_combatants.max_hit_points");
        }
        if (!await HasColumnAsync("encounter_combatants", "damage_dice", cancellationToken))
        {
            missingColumns.Add("encounter_combatants.damage_dice");
        }
        if (!await HasColumnAsync("encounter_combatants", "challenge_rating", cancellationToken))
        {
            missingColumns.Add("encounter_combatants.challenge_rating");
        }

        if (missingColumns.Count == 0)
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"PostgreSQL schema is outdated. Missing columns: {string.Join(", ", missingColumns)}");
        }

        logger.LogWarning(
            "PostgreSQL schema is outdated ({MissingColumns}). Recreating database in Development mode.",
            string.Join(", ", missingColumns));

        await dbContext.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS public CASCADE;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA public;", cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private async Task RemoveObsoletePostgresColumnsAsync(CancellationToken cancellationToken)
    {
        var obsoleteColumns = new[]
        {
            ("characters", "prepared_spells_json"),
            ("characters", "active_effects_json"),
        };

        foreach (var (tableName, columnName) in obsoleteColumns)
        {
            if (!await HasColumnAsync(tableName, columnName, cancellationToken))
            {
                continue;
            }

            logger.LogInformation("Removing obsolete PostgreSQL column {Table}.{Column}...", tableName, columnName);
            await dbContext.Database.ExecuteSqlAsync(
                $"ALTER TABLE {tableName} DROP COLUMN IF EXISTS {columnName};",
                cancellationToken);
        }
    }

    private async Task NormalizePostgresDataAsync(CancellationToken cancellationToken)
    {
        var characters = await dbContext.Characters.ToListAsync(cancellationToken);
        var changed = false;

        foreach (var character in characters)
        {
            var normalizedKnownSpells = NormalizeStringArray(character.KnownSpellsJson);
            if (!string.Equals(character.KnownSpellsJson, normalizedKnownSpells, StringComparison.Ordinal))
            {
                character.KnownSpellsJson = normalizedKnownSpells;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(character.SpentSpellSlotsJson))
            {
                character.SpentSpellSlotsJson = "{}";
                changed = true;
            }

            if (character.MaxHitPoints <= 0 && character.HitPoints > 0)
            {
                character.MaxHitPoints = character.HitPoints;
                character.CurrentHitPoints = Math.Clamp(character.CurrentHitPoints > 0 ? character.CurrentHitPoints : character.HitPoints, 0, character.MaxHitPoints);
                changed = true;
            }

            if (character.MaxHitPoints > 0)
            {
                var clampedCurrentHitPoints = Math.Clamp(character.CurrentHitPoints, 0, character.MaxHitPoints);
                if (clampedCurrentHitPoints != character.CurrentHitPoints)
                {
                    character.CurrentHitPoints = clampedCurrentHitPoints;
                    changed = true;
                }
            }
        }

        if (!changed)
        {
            return;
        }

        logger.LogInformation("Applying PostgreSQL data normalization for characters...");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HasColumnAsync(string tableName, string columnName, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = @tableName AND column_name = @columnName
            );
            """;
        command.Parameters.AddWithValue("tableName", tableName);
        command.Parameters.AddWithValue("columnName", columnName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    private async Task<bool> HasTableAsync(string tableName, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = @tableName
            );
            """;
        command.Parameters.AddWithValue("tableName", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    private static string NormalizeStringArray(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return "[]";
        }

        try
        {
            var values = JsonSerializer.Deserialize<List<string>>(source, JsonOptions) ?? [];
            var normalized = values
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return JsonSerializer.Serialize(normalized, JsonOptions);
        }
        catch
        {
            return "[]";
        }
    }
}
