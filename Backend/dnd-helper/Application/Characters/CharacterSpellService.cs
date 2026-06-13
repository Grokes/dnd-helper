using dnd_helper.Infrastructure.Seeding;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace dnd_helper.Application.Characters;

public sealed class CharacterSpellService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IRulesCatalogRepository rulesRepository;
    private readonly DiceRoller diceRoller;

    public CharacterSpellService(IRulesCatalogRepository rulesRepository, DiceRoller diceRoller)
    {
        this.rulesRepository = rulesRepository;
        this.diceRoller = diceRoller;
    }

    public async Task<CharacterCastSpellOutcome> CastAsync(
        CharacterEntity character,
        CharacterCastSpellRequest request,
        CancellationToken cancellationToken)
    {
        var spellSlug = (request.SpellSlug ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(spellSlug))
        {
            return CharacterCastSpellOutcome.Validation("spellSlug", "Выбери заклинание.");
        }

        var knownSpells = JsonSerializer.Deserialize<List<string>>(character.KnownSpellsJson, JsonOptions) ?? [];
        if (!knownSpells.Any(item => item.Equals(spellSlug, StringComparison.OrdinalIgnoreCase)))
        {
            return CharacterCastSpellOutcome.Validation("spellSlug", "Персонаж не знает это заклинание.");
        }

        var spell = (await rulesRepository.GetSpellsAsync(RulesDatabaseSeeder.RulesetId, cancellationToken))
            .FirstOrDefault(item => item.Slug.Equals(spellSlug, StringComparison.OrdinalIgnoreCase));
        if (spell is null)
        {
            return CharacterCastSpellOutcome.NotFound();
        }

        var maxSlots = JsonSerializer.Deserialize<List<SpellSlotDto>>(character.SpellSlotsJson, JsonOptions) ?? [];
        var spentSlots = DeserializeIntMap(character.SpentSpellSlotsJson);
        var spellLevel = Math.Max(0, spell.SpellLevel);
        var chosenSlotLevel = request.SlotLevel;
        var consumedSlot = false;
        var message = $"Заклинание «{spell.Name}» применено.";

        if (spellLevel > 0)
        {
            var slotValidation = TrySpendSlot(spellLevel, chosenSlotLevel, maxSlots, spentSlots, out var levelToUse);
            if (slotValidation is not null)
            {
                return slotValidation;
            }

            spentSlots[levelToUse] = Math.Max(0, spentSlots.GetValueOrDefault(levelToUse)) + 1;
            consumedSlot = true;
            chosenSlotLevel = levelToUse;
            message = $"Заклинание «{spell.Name}» применено. Израсходована ячейка круга {levelToUse}.";
        }

        character.SpentSpellSlotsJson = JsonSerializer.Serialize(spentSlots, JsonOptions);
        character.UpdatedAtUtc = DateTime.UtcNow;

        var currentSlots = maxSlots.Select(item =>
        {
            var spent = Math.Max(0, spentSlots.GetValueOrDefault(item.SpellLevel));
            return new SpellSlotDto(item.SpellLevel, Math.Max(0, item.Slots - spent));
        }).ToList();

        var summaryText = spell.Effects.FirstOrDefault(effect => effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))?.Reason ?? string.Empty;
        var descriptionText = spell.Effects.FirstOrDefault(effect => effect.Target.Equals("description", StringComparison.OrdinalIgnoreCase))?.Reason ?? string.Empty;
        var hasDamage = TryGetSpellDamageProfile(spell.Slug, summaryText, descriptionText, out var damageDice, out var damageType);
        int? damageRoll = null;
        int? damageTotal = null;
        if (hasDamage && !string.IsNullOrWhiteSpace(damageDice) && diceRoller.TryRoll(damageDice!, out var rollResult))
        {
            damageRoll = rollResult.Total;
            damageTotal = rollResult.Total;
        }

        return CharacterCastSpellOutcome.Success(new CharacterCastSpellResultDto(
            spell.Slug,
            spell.Name,
            spellLevel,
            chosenSlotLevel,
            consumedSlot,
            currentSlots,
            maxSlots,
            damageDice,
            damageType,
            damageRoll,
            damageTotal,
            message));
    }

    private static CharacterCastSpellOutcome? TrySpendSlot(
        int spellLevel,
        int? requestedSlotLevel,
        IReadOnlyList<SpellSlotDto> maxSlots,
        Dictionary<int, int> spentSlots,
        out int levelToUse)
    {
        levelToUse = requestedSlotLevel ?? spellLevel;
        if (levelToUse < spellLevel)
        {
            return CharacterCastSpellOutcome.Validation("slotLevel", "Нельзя использовать ячейку ниже круга заклинания.");
        }

        var selectedSlotLevel = levelToUse;
        var slot = maxSlots.FirstOrDefault(item => item.SpellLevel == selectedSlotLevel);
        if (slot is null)
        {
            return CharacterCastSpellOutcome.Validation("slotLevel", "У персонажа нет ячейки этого круга.");
        }

        var spent = Math.Max(0, spentSlots.GetValueOrDefault(levelToUse));
        return spent >= slot.Slots
            ? CharacterCastSpellOutcome.Validation("slotLevel", "Все ячейки этого круга уже израсходованы.")
            : null;
    }

    private static bool TryGetSpellDamageProfile(string slug, string summary, string description, out string? damageDice, out string? damageType)
    {
        var map = new Dictionary<string, (string dice, string type)>(StringComparer.OrdinalIgnoreCase)
        {
            ["fire-bolt"] = ("1d10", "огонь"),
            ["chill-touch"] = ("1d8", "некротический"),
            ["ray-of-frost"] = ("1d8", "холод"),
            ["sacred-flame"] = ("1d8", "сияние"),
            ["thorn-whip"] = ("1d6", "колющий"),
            ["poison-spray"] = ("1d12", "яд"),
            ["burning-hands"] = ("3d6", "огонь"),
            ["thunderwave"] = ("2d8", "гром"),
            ["magic-missile"] = ("3d4+3", "силовой"),
            ["chromatic-orb"] = ("3d8", "элементальный"),
            ["guiding-bolt"] = ("4d6", "сияние"),
            ["inflict-wounds"] = ("3d10", "некротический"),
            ["witch-bolt"] = ("1d12", "молния"),
            ["scorching-ray"] = ("2d6", "огонь"),
            ["shatter"] = ("3d8", "гром"),
            ["melfs-acid-arrow"] = ("4d4", "кислота"),
            ["fireball"] = ("8d6", "огонь"),
            ["lightning-bolt"] = ("8d6", "молния"),
            ["blight"] = ("8d8", "некротический"),
            ["cone-of-cold"] = ("8d8", "холод")
        };

        if (map.TryGetValue(slug, out var profile))
        {
            damageDice = profile.dice;
            damageType = profile.type;
            return true;
        }

        var text = $"{summary} {description}";
        var diceMatch = Regex.Match(text, @"(?<dice>\d+d\d+(\s*[\+\-]\s*\d+)?)", RegexOptions.IgnoreCase);
        if (diceMatch.Success)
        {
            damageDice = diceMatch.Groups["dice"].Value.Replace(" ", string.Empty);
            damageType = InferDamageType(text);
            return true;
        }

        damageDice = null;
        damageType = null;
        return false;
    }

    private static string InferDamageType(string text)
    {
        var normalized = text.ToLowerInvariant();
        if (normalized.Contains("огн")) return "огонь";
        if (normalized.Contains("холод")) return "холод";
        if (normalized.Contains("молн")) return "молния";
        if (normalized.Contains("гром")) return "гром";
        if (normalized.Contains("кисл")) return "кислота";
        if (normalized.Contains("яд")) return "яд";
        if (normalized.Contains("некрот")) return "некротический";
        if (normalized.Contains("сиян")) return "сияние";
        if (normalized.Contains("псих")) return "психический";
        if (normalized.Contains("силов")) return "силовой";
        if (normalized.Contains("колющ")) return "колющий";
        if (normalized.Contains("рубящ")) return "рубящий";
        if (normalized.Contains("дробящ")) return "дробящий";
        return "урон";
    }

    private static Dictionary<int, int> DeserializeIntMap(string? source)
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
}

public sealed record CharacterCastSpellOutcome(
    CharacterCastSpellResultDto? Result,
    Dictionary<string, string[]>? Errors,
    bool IsNotFound)
{
    public bool IsSuccess => Result is not null && Errors is null && !IsNotFound;

    public static CharacterCastSpellOutcome Success(CharacterCastSpellResultDto result) => new(result, null, false);

    public static CharacterCastSpellOutcome Validation(string key, string message) => new(
        null,
        new Dictionary<string, string[]> { [key] = [message] },
        false);

    public static CharacterCastSpellOutcome NotFound() => new(null, null, true);
}
