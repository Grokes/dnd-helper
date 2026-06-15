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
    private sealed record SelectedOptionsState(
        List<string> BonusAbilitySelections,
        List<string> RaceSkillSelections,
        List<string> ClassSkillSelections);

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
        if (await HasTableAsync("characters", cancellationToken))
        {
            await EnsureCharacterColumnsAsync(cancellationToken);
            await EnsureNormalizedCharacterTablesAsync(cancellationToken);
        }

        if (await HasTableAsync("encounter_combatants", cancellationToken))
        {
            await EnsureEncounterCombatantColumnsAsync(cancellationToken);
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
        if (!await HasTableAsync("encounters", cancellationToken))
        {
            missingColumns.Add("encounters");
        }
        else
        {
            await EnsureEncounterCombatColumnsAsync(cancellationToken);
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

    private async Task EnsureCharacterColumnsAsync(CancellationToken cancellationToken)
    {
        var statements = new[]
        {
            "ALTER TABLE characters ADD COLUMN IF NOT EXISTS subclass character varying(120) NOT NULL DEFAULT '';",
            "ALTER TABLE characters ADD COLUMN IF NOT EXISTS weapon_damage character varying(80) NOT NULL DEFAULT '';",
            "ALTER TABLE characters ADD COLUMN IF NOT EXISTS hit_die integer NOT NULL DEFAULT 8;",
            "ALTER TABLE characters ADD COLUMN IF NOT EXISTS spell_slots_json text NOT NULL DEFAULT '[]';",
            "ALTER TABLE characters ADD COLUMN IF NOT EXISTS max_hit_points integer NOT NULL DEFAULT 0;",
            "ALTER TABLE characters ADD COLUMN IF NOT EXISTS current_hit_points integer NOT NULL DEFAULT 0;",
            "ALTER TABLE characters ADD COLUMN IF NOT EXISTS spent_hit_dice integer NOT NULL DEFAULT 0;",
            "ALTER TABLE characters ADD COLUMN IF NOT EXISTS spent_spell_slots_json text NOT NULL DEFAULT '{{}}';"
        };

        foreach (var statement in statements)
        {
            await dbContext.Database.ExecuteSqlRawAsync(statement, cancellationToken);
        }
    }

    private async Task EnsureEncounterCombatantColumnsAsync(CancellationToken cancellationToken)
    {
        var statements = new[]
        {
            "ALTER TABLE encounter_combatants ADD COLUMN IF NOT EXISTS monster_slug character varying(120) NULL;",
            "ALTER TABLE encounter_combatants ADD COLUMN IF NOT EXISTS armor_class integer NOT NULL DEFAULT 10;",
            "ALTER TABLE encounter_combatants ADD COLUMN IF NOT EXISTS max_hit_points integer NOT NULL DEFAULT 1;",
            "ALTER TABLE encounter_combatants ADD COLUMN IF NOT EXISTS damage_dice character varying(32) NULL;",
            "ALTER TABLE encounter_combatants ADD COLUMN IF NOT EXISTS challenge_rating numeric(5,2) NOT NULL DEFAULT 0;"
        };

        foreach (var statement in statements)
        {
            await dbContext.Database.ExecuteSqlRawAsync(statement, cancellationToken);
        }
    }

    private async Task EnsureEncounterCombatColumnsAsync(CancellationToken cancellationToken)
    {
        var statements = new[]
        {
            "ALTER TABLE encounters ADD COLUMN IF NOT EXISTS is_combat_active boolean NOT NULL DEFAULT false;",
            "ALTER TABLE encounters ADD COLUMN IF NOT EXISTS round_number integer NOT NULL DEFAULT 0;",
            "ALTER TABLE encounters ADD COLUMN IF NOT EXISTS current_turn_combatant_id uuid NULL;",
            "ALTER TABLE encounters ADD COLUMN IF NOT EXISTS turn_started_at_utc timestamp with time zone NULL;"
        };

        foreach (var statement in statements)
        {
            await dbContext.Database.ExecuteSqlRawAsync(statement, cancellationToken);
        }
    }

    private async Task EnsureNormalizedCharacterTablesAsync(CancellationToken cancellationToken)
    {
        var statements = new[]
        {
            """
            CREATE TABLE IF NOT EXISTS character_base_abilities (
                character_id uuid NOT NULL REFERENCES characters("Id") ON DELETE CASCADE,
                ability character varying(8) NOT NULL,
                score integer NOT NULL,
                CONSTRAINT pk_character_base_abilities PRIMARY KEY (character_id, ability)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS character_abilities (
                character_id uuid NOT NULL REFERENCES characters("Id") ON DELETE CASCADE,
                ability character varying(8) NOT NULL,
                score integer NOT NULL,
                modifier integer NOT NULL,
                CONSTRAINT pk_character_abilities PRIMARY KEY (character_id, ability)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS character_selected_options (
                id uuid NOT NULL,
                character_id uuid NOT NULL REFERENCES characters("Id") ON DELETE CASCADE,
                source character varying(40) NOT NULL,
                option_type character varying(40) NOT NULL,
                value character varying(120) NOT NULL,
                CONSTRAINT pk_character_selected_options PRIMARY KEY (id)
            );
            """,
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ux_character_selected_options_identity
                ON character_selected_options(character_id, source, option_type, value);
            """,
            """
            CREATE TABLE IF NOT EXISTS character_skill_proficiencies (
                character_id uuid NOT NULL REFERENCES characters("Id") ON DELETE CASCADE,
                skill_id character varying(80) NOT NULL,
                bonus integer NOT NULL,
                CONSTRAINT pk_character_skill_proficiencies PRIMARY KEY (character_id, skill_id)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS character_saving_throw_proficiencies (
                character_id uuid NOT NULL REFERENCES characters("Id") ON DELETE CASCADE,
                ability character varying(8) NOT NULL,
                CONSTRAINT pk_character_saving_throw_proficiencies PRIMARY KEY (character_id, ability)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS character_known_spells (
                character_id uuid NOT NULL REFERENCES characters("Id") ON DELETE CASCADE,
                spell_slug character varying(120) NOT NULL,
                CONSTRAINT pk_character_known_spells PRIMARY KEY (character_id, spell_slug)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS character_spell_slots (
                character_id uuid NOT NULL REFERENCES characters("Id") ON DELETE CASCADE,
                spell_level integer NOT NULL,
                max_slots integer NOT NULL,
                spent_slots integer NOT NULL DEFAULT 0,
                CONSTRAINT pk_character_spell_slots PRIMARY KEY (character_id, spell_level)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS character_inventory_items (
                id uuid NOT NULL,
                character_id uuid NOT NULL REFERENCES characters("Id") ON DELETE CASCADE,
                room_id uuid NULL,
                item_ref character varying(160) NOT NULL,
                quantity integer NOT NULL DEFAULT 1,
                equipment_slot character varying(40) NULL,
                is_equipped boolean NOT NULL DEFAULT false,
                created_at_utc timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT pk_character_inventory_items PRIMARY KEY (id)
            );
            """,
            """
            CREATE INDEX IF NOT EXISTS ix_character_inventory_items_lookup
                ON character_inventory_items(character_id, room_id, item_ref, equipment_slot);
            """,
            """
            CREATE TABLE IF NOT EXISTS character_calculation_trace_entries (
                id uuid NOT NULL,
                character_id uuid NOT NULL REFERENCES characters("Id") ON DELETE CASCADE,
                entry_order integer NOT NULL,
                target character varying(120) NOT NULL,
                source character varying(160) NOT NULL,
                reason text NOT NULL,
                value integer NOT NULL,
                operation character varying(40) NOT NULL,
                CONSTRAINT pk_character_calculation_trace_entries PRIMARY KEY (id)
            );
            """,
            """
            CREATE INDEX IF NOT EXISTS ix_character_calculation_trace_entries_order
                ON character_calculation_trace_entries(character_id, entry_order);
            """
        };

        foreach (var statement in statements)
        {
            await dbContext.Database.ExecuteSqlRawAsync(statement, cancellationToken);
        }
    }

    private async Task RemoveObsoletePostgresColumnsAsync(CancellationToken cancellationToken)
    {
        var obsoleteColumns = new (string TableName, string ColumnName, string DropStatement)[]
        {
            ("characters", "prepared_spells_json", "ALTER TABLE characters DROP COLUMN IF EXISTS prepared_spells_json;"),
            ("characters", "active_effects_json", "ALTER TABLE characters DROP COLUMN IF EXISTS active_effects_json;"),
            ("room_memberships", "inventory_json", "ALTER TABLE room_memberships DROP COLUMN IF EXISTS inventory_json;"),
        };

        foreach (var (tableName, columnName, dropStatement) in obsoleteColumns)
        {
            if (!await HasColumnAsync(tableName, columnName, cancellationToken))
            {
                continue;
            }

            logger.LogInformation("Removing obsolete PostgreSQL column {Table}.{Column}...", tableName, columnName);
            await dbContext.Database.ExecuteSqlRawAsync(dropStatement, cancellationToken);
        }
    }

    private async Task NormalizePostgresDataAsync(CancellationToken cancellationToken)
    {
        var characters = await dbContext.Characters
            .IncludeCharacterState()
            .ToListAsync(cancellationToken);
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

            if (character.HitDie <= 0)
            {
                character.HitDie = CharacterOptionsCatalog.Classes.FirstOrDefault(item => item.Id == character.ClassId)?.HitDie ?? 8;
                changed = true;
            }

            if (BackfillNormalizedCharacterState(character))
            {
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        logger.LogInformation("Applying PostgreSQL data normalization for characters...");
        KeepExistingNormalizedCharacterRowsUnchanged();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void KeepExistingNormalizedCharacterRowsUnchanged()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries()
                     .Where(entry => entry.State == EntityState.Modified && IsNormalizedCharacterState(entry.Entity)))
        {
            entry.State = EntityState.Unchanged;
        }
    }

    private static bool IsNormalizedCharacterState(object entity)
    {
        return entity is CharacterBaseAbilityEntity
            or CharacterAbilityEntity
            or CharacterSelectedOptionEntity
            or CharacterSkillProficiencyEntity
            or CharacterSavingThrowProficiencyEntity
            or CharacterKnownSpellEntity
            or CharacterSpellSlotEntity
            or CharacterInventoryItemEntity
            or CharacterCalculationTraceEntryEntity;
    }

    private static bool BackfillNormalizedCharacterState(CharacterEntity character)
    {
        var changed = false;

        if (character.BaseAbilities.Count == 0)
        {
            character.BaseAbilities.AddRange(ReadList<BaseAbilityScoreDto>(character.BaseAbilitiesJson)
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Select(item => new CharacterBaseAbilityEntity
                {
                    CharacterId = character.Id,
                    Character = character,
                    Ability = item.Key.ToUpperInvariant(),
                    Score = item.Score
                }));
            changed = character.BaseAbilities.Count > 0 || changed;
        }

        if (character.Abilities.Count == 0)
        {
            character.Abilities.AddRange(ReadList<AbilityScoreDto>(character.AbilitiesJson)
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Select(item => new CharacterAbilityEntity
                {
                    CharacterId = character.Id,
                    Character = character,
                    Ability = item.Key.ToUpperInvariant(),
                    Score = item.Score,
                    Modifier = item.Modifier
                }));
            changed = character.Abilities.Count > 0 || changed;
        }

        if (character.SelectedOptions.Count == 0)
        {
            var selectedOptions = ReadSelectedOptions(character.BonusAbilitySelectionsJson);
            character.SelectedOptions.AddRange(selectedOptions.BonusAbilitySelections.Select(value => CreateSelectedOption(character, "race", "ability-bonus", value)));
            character.SelectedOptions.AddRange(selectedOptions.RaceSkillSelections.Select(value => CreateSelectedOption(character, "race", "skill", value)));
            character.SelectedOptions.AddRange(selectedOptions.ClassSkillSelections.Select(value => CreateSelectedOption(character, "class", "skill", value)));
            changed = character.SelectedOptions.Count > 0 || changed;
        }

        if (character.SkillProficiencies.Count == 0)
        {
            character.SkillProficiencies.AddRange(ReadList<SkillLevelDto>(character.SkillsJson)
                .GroupBy(item => item.SkillId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Select(item => new CharacterSkillProficiencyEntity
                {
                    CharacterId = character.Id,
                    Character = character,
                    SkillId = item.SkillId,
                    Bonus = item.Level
                }));
            changed = character.SkillProficiencies.Count > 0 || changed;
        }

        if (character.SavingThrowProficiencies.Count == 0)
        {
            character.SavingThrowProficiencies.AddRange((CharacterOptionsCatalog.Classes.FirstOrDefault(item => item.Id == character.ClassId)?.SavingThrowProficiencies ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(ability => new CharacterSavingThrowProficiencyEntity
                {
                    CharacterId = character.Id,
                    Character = character,
                    Ability = ability
                }));
            changed = character.SavingThrowProficiencies.Count > 0 || changed;
        }

        if (character.KnownSpells.Count == 0)
        {
            character.KnownSpells.AddRange(ReadList<string>(character.KnownSpellsJson)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(spellSlug => new CharacterKnownSpellEntity
                {
                    CharacterId = character.Id,
                    Character = character,
                    SpellSlug = spellSlug
                }));
            changed = character.KnownSpells.Count > 0 || changed;
        }

        if (character.SpellSlots.Count == 0)
        {
            var spentSlots = ReadDictionary(character.SpentSpellSlotsJson);
            character.SpellSlots.AddRange(ReadList<SpellSlotDto>(character.SpellSlotsJson)
                .GroupBy(item => item.SpellLevel)
                .Select(group => group.First())
                .Select(slot => new CharacterSpellSlotEntity
                {
                    CharacterId = character.Id,
                    Character = character,
                    SpellLevel = slot.SpellLevel,
                    MaxSlots = slot.Slots,
                    SpentSlots = Math.Clamp(spentSlots.GetValueOrDefault(slot.SpellLevel), 0, slot.Slots)
                }));
            changed = character.SpellSlots.Count > 0 || changed;
        }

        if (character.InventoryItems.Count == 0)
        {
            var inventory = ReadList<string>(character.InventoryJson)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToList();
            character.InventoryItems.AddRange(inventory
                .Where(item => !item.StartsWith("equip:", StringComparison.OrdinalIgnoreCase))
                .GroupBy(item => item, StringComparer.OrdinalIgnoreCase)
                .Select(group => new CharacterInventoryItemEntity
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.Id,
                    Character = character,
                    ItemRef = group.Key,
                    Quantity = group.Count(),
                    CreatedAtUtc = DateTime.UtcNow
                }));
            character.InventoryItems.AddRange(inventory
                .Where(item => item.StartsWith("equip:", StringComparison.OrdinalIgnoreCase))
                .SelectMany(ParseEquipToken)
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Select(item => new CharacterInventoryItemEntity
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.Id,
                    Character = character,
                    ItemRef = item.Value,
                    Quantity = 1,
                    EquipmentSlot = item.Key,
                    IsEquipped = true,
                    CreatedAtUtc = DateTime.UtcNow
                }));
            changed = character.InventoryItems.Count > 0 || changed;
        }

        if (character.CalculationTraceEntries.Count == 0)
        {
            character.CalculationTraceEntries.AddRange(ReadList<CalculationTraceEntryDto>(character.CalculationTraceJson)
                .Select((entry, index) => new CharacterCalculationTraceEntryEntity
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.Id,
                    Character = character,
                    Order = index,
                    Target = entry.Target,
                    Source = entry.Source,
                    Reason = entry.Reason,
                    Value = entry.Value,
                    Operation = entry.Operation
                }));
            changed = character.CalculationTraceEntries.Count > 0 || changed;
        }

        return changed;
    }

    private static List<T> ReadList<T>(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(source, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static Dictionary<int, int> ReadDictionary(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(source, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static SelectedOptionsState ReadSelectedOptions(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return new SelectedOptionsState([], [], []);
        }

        try
        {
            using var document = JsonDocument.Parse(source);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                return new SelectedOptionsState(
                    ReadList<string>(source).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    [],
                    []);
            }

            var parsed = JsonSerializer.Deserialize<SelectedOptionsState>(source, JsonOptions);
            return parsed is null
                ? new SelectedOptionsState([], [], [])
                : new SelectedOptionsState(
                    (parsed.BonusAbilitySelections ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    (parsed.RaceSkillSelections ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    (parsed.ClassSkillSelections ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }
        catch
        {
            return new SelectedOptionsState([], [], []);
        }
    }

    private static CharacterSelectedOptionEntity CreateSelectedOption(
        CharacterEntity character,
        string source,
        string optionType,
        string value)
    {
        return new CharacterSelectedOptionEntity
        {
            Id = Guid.NewGuid(),
            CharacterId = character.Id,
            Character = character,
            Source = source,
            OptionType = optionType,
            Value = value
        };
    }

    private static Dictionary<string, string> ParseEquipToken(string raw)
    {
        if (!raw.StartsWith("equip:", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return raw["equip:".Length..]
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split('=', 2))
            .Where(parts => parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
            .ToDictionary(parts => NormalizeEquipmentSlot(parts[0].Trim()), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeEquipmentSlot(string slot)
    {
        return slot.Trim().ToLowerInvariant() switch
        {
            "body" or "armor" => "body",
            "main" or "mainhand" or "main-hand" or "right" => "main",
            "off" or "offhand" or "off-hand" or "left" => "off",
            var value => value
        };
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
