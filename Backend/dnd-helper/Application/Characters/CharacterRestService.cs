using System.Text.Json;

namespace dnd_helper.Application.Characters;

public sealed class CharacterRestService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DiceRoller diceRoller;

    public CharacterRestService(DiceRoller diceRoller)
    {
        this.diceRoller = diceRoller;
    }

    public CharacterRestOutcome ApplyRest(CharacterEntity character, CharacterRestRequest request, bool canEdit)
    {
        var normalizedType = (request.RestType ?? string.Empty).Trim().ToLowerInvariant();
        var maxHitPoints = character.MaxHitPoints > 0 ? character.MaxHitPoints : character.HitPoints;
        var currentHitPoints = character.MaxHitPoints > 0
            ? Math.Clamp(character.CurrentHitPoints, 0, maxHitPoints)
            : maxHitPoints;
        var previousCurrentHitPoints = currentHitPoints;
        var totalHitDice = Math.Max(1, character.Level);
        var spentHitDice = Math.Clamp(character.SpentHitDice, 0, totalHitDice);
        var details = string.Empty;

        if (normalizedType is "short" or "short-rest")
        {
            var outcome = ApplyShortRest(character, request, canEdit, maxHitPoints, currentHitPoints, totalHitDice, spentHitDice);
            if (!outcome.IsSuccess)
            {
                return outcome;
            }

            currentHitPoints = outcome.Result!.CurrentHitPoints;
            spentHitDice = outcome.Result.SpentHitDice;
            details = outcome.Result.Details;
        }
        else if (normalizedType is "long" or "long-rest")
        {
            currentHitPoints = maxHitPoints;
            var recoveredHitDice = Math.Max(1, totalHitDice / 2);
            spentHitDice = Math.Max(0, spentHitDice - recoveredHitDice);
            ResetSpentSpellSlots(character);
            details = $"Длительный отдых: хиты восстановлены полностью, кости хитов восстановлены: {recoveredHitDice}, ячейки заклинаний восстановлены.";
        }
        else if (normalizedType is "full-heal" or "heal")
        {
            currentHitPoints = maxHitPoints;
            details = "Полное лечение: текущие хиты восстановлены до максимума.";
        }
        else
        {
            return CharacterRestOutcome.Validation("restType", "Допустимые значения: short, long, full-heal.");
        }

        character.MaxHitPoints = maxHitPoints;
        character.CurrentHitPoints = currentHitPoints;
        character.SpentHitDice = Math.Clamp(spentHitDice, 0, totalHitDice);
        character.UpdatedAtUtc = DateTime.UtcNow;

        var maxSpellSlots = ReadMaxSlots(character);
        var spentSpellSlotsFinal = ReadSpentSlots(character);
        var currentSpellSlots = maxSpellSlots.Select(slot =>
        {
            var spent = Math.Max(0, spentSpellSlotsFinal.GetValueOrDefault(slot.SpellLevel));
            return new SpellSlotDto(slot.SpellLevel, Math.Max(0, slot.Slots - spent));
        }).ToList();

        return CharacterRestOutcome.Success(new CharacterRestResultDto(
            normalizedType,
            previousCurrentHitPoints,
            character.CurrentHitPoints,
            character.MaxHitPoints,
            Math.Max(0, character.CurrentHitPoints - previousCurrentHitPoints),
            character.SpentHitDice,
            Math.Max(0, totalHitDice - character.SpentHitDice),
            currentSpellSlots,
            maxSpellSlots,
            details));
    }

    private CharacterRestOutcome ApplyShortRest(
        CharacterEntity character,
        CharacterRestRequest request,
        bool canEdit,
        int maxHitPoints,
        int currentHitPoints,
        int totalHitDice,
        int spentHitDice)
    {
        var hitDiceToSpend = request.HitDiceToSpend ?? 0;
        if (hitDiceToSpend < 0)
        {
            return CharacterRestOutcome.Validation("hitDiceToSpend", "Количество костей хитов не может быть отрицательным.");
        }

        var availableHitDice = totalHitDice - spentHitDice;
        if (availableHitDice <= 0)
        {
            return CharacterRestOutcome.Validation("hitDiceToSpend", "У персонажа нет доступных костей хитов.");
        }

        if (hitDiceToSpend > availableHitDice)
        {
            return CharacterRestOutcome.Validation("hitDiceToSpend", $"Можно потратить не более {availableHitDice} костей хитов.");
        }

        var hitDie = character.HitDie > 0 ? character.HitDie : 8;
        var conModifier = character.ToDto(canEdit).Abilities.FirstOrDefault(item => item.Key == "CON")?.Modifier ?? 0;
        var rolls = new List<int>(Math.Max(0, hitDiceToSpend));
        var healed = 0;
        for (var index = 0; index < hitDiceToSpend; index++)
        {
            if (!diceRoller.TryRoll($"1d{hitDie}", out var roll))
            {
                return CharacterRestOutcome.Validation("hitDiceToSpend", "Не удалось бросить кость хитов.");
            }

            var diceValue = roll.Rolls.FirstOrDefault();
            rolls.Add(diceValue);
            healed += Math.Max(0, diceValue + conModifier);
        }

        currentHitPoints = Math.Min(maxHitPoints, currentHitPoints + healed);
        spentHitDice += hitDiceToSpend;
        var rollsText = rolls.Count > 0 ? string.Join(", ", rolls) : "без траты костей";
        var details = $"Короткий отдых: потрачено {hitDiceToSpend}к{hitDie}, броски: {rollsText}; мод. Телосложения {conModifier:+#;-#;0}.";

        if (character.ClassId.Equals("warlock", StringComparison.OrdinalIgnoreCase))
        {
            ResetSpentSpellSlots(character);
            details += " Ячейки колдуна восстановлены (классовая особенность).";
        }

        return CharacterRestOutcome.Success(new CharacterRestResultDto(
            "short",
            currentHitPoints,
            currentHitPoints,
            maxHitPoints,
            healed,
            spentHitDice,
            Math.Max(0, totalHitDice - spentHitDice),
            [],
            [],
            details));
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

    private static List<SpellSlotDto> ReadMaxSlots(CharacterEntity character)
    {
        return character.SpellSlots.Count > 0
            ? character.SpellSlots
                .OrderBy(item => item.SpellLevel)
                .Select(item => new SpellSlotDto(item.SpellLevel, item.MaxSlots))
                .ToList()
            : JsonSerializer.Deserialize<List<SpellSlotDto>>(character.SpellSlotsJson, JsonOptions) ?? [];
    }

    private static Dictionary<int, int> ReadSpentSlots(CharacterEntity character)
    {
        return character.SpellSlots.Count > 0
            ? character.SpellSlots
                .Where(item => item.SpentSlots > 0)
                .ToDictionary(item => item.SpellLevel, item => item.SpentSlots)
            : DeserializeIntMap(character.SpentSpellSlotsJson);
    }

    private static void ResetSpentSpellSlots(CharacterEntity character)
    {
        foreach (var slot in character.SpellSlots)
        {
            slot.SpentSlots = 0;
        }

        character.SpentSpellSlotsJson = "{}";
    }
}

public sealed record CharacterRestOutcome(CharacterRestResultDto? Result, Dictionary<string, string[]>? Errors)
{
    public bool IsSuccess => Errors is null;

    public static CharacterRestOutcome Success(CharacterRestResultDto result) => new(result, null);

    public static CharacterRestOutcome Validation(string key, string message) => new(
        null,
        new Dictionary<string, string[]> { [key] = [message] });
}
