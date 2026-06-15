
namespace dnd_helper.Application.Characters;

public sealed class CharacterCreationService(
    IRulesCatalogRepository rulesRepository,
    RuleResolutionService ruleResolutionService)
{
    private const string DefaultRulesetId = "phb-5e-rus";
    private static readonly HashSet<string> AllowedAbilities = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];

    public async Task<CharacterCreationResult> BuildCharacterAsync(
        CreateCharacterRequest request,
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var race = await rulesRepository.GetRaceBySlugAsync(DefaultRulesetId, request.RaceId, cancellationToken);
        var characterClass = await rulesRepository.GetClassBySlugAsync(DefaultRulesetId, request.ClassId, cancellationToken);
        var background = await rulesRepository.GetBackgroundBySlugAsync(DefaultRulesetId, request.BackgroundId, cancellationToken);
        var spellsCatalog = await rulesRepository.GetSpellsAsync(DefaultRulesetId, cancellationToken);
        var equipmentCatalog = await rulesRepository.GetEquipmentAsync(DefaultRulesetId, cancellationToken);

        var validationErrors = ValidateRequest(request, race, characterClass, background);
        if (validationErrors.Count > 0)
        {
            return CharacterCreationResult.Failed(validationErrors);
        }

        var spellErrors = ValidateSpells(request, characterClass!, spellsCatalog);
        if (spellErrors.Count > 0)
        {
            return CharacterCreationResult.Failed(new Dictionary<string, string[]> { ["spells"] = spellErrors.ToArray() });
        }

        var allFeatures = await rulesRepository.GetFeaturesAsync(DefaultRulesetId, cancellationToken);
        var classFeatures = ruleResolutionService.ResolveClassFeaturesByLevel(characterClass!, allFeatures, request.Level);
        var requireErrors = ruleResolutionService.ValidateRequires(request, race!, characterClass!, background!, classFeatures);
        if (requireErrors.Count > 0)
        {
            return CharacterCreationResult.Failed(new Dictionary<string, string[]> { ["requires"] = requireErrors.ToArray() });
        }

        var resolution = ruleResolutionService.Resolve(request, race!, characterClass!, background!, classFeatures, equipmentCatalog);

        var computedCharacter = new CharacterComputationResult(
            request.RaceId,
            request.ClassId,
            string.Empty,
            request.BackgroundId,
            request.Name.Trim(),
            resolution.RaceName,
            resolution.ClassName,
            resolution.BackgroundName,
            request.Level,
            request.Alignment.Trim(),
            request.Notes.Trim(),
            request.BaseAbilities.ToList(),
            request.BonusAbilitySelections.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            request.RaceSkillSelections.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            request.ClassSkillSelections.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            resolution.ArmorClass,
            resolution.WeaponDamage,
            resolution.HitDie,
            resolution.HitPoints,
            resolution.Speed,
            resolution.ProficiencyBonus,
            resolution.PassivePerception,
            resolution.Abilities,
            resolution.Skills,
            resolution.SpellSlots,
            resolution.SavingThrowProficiencies,
            request.Spells.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            request.Inventory.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            resolution.ComputedSnapshot,
            resolution.CalculationTrace);

        var entity = CharacterEntity.FromComputed(computedCharacter, ownerUserId);
        return CharacterCreationResult.Success(entity);
    }

    private static Dictionary<string, string[]> ValidateRequest(
        CreateCharacterRequest request,
        Infrastructure.Persistence.Mongo.RaceDocument? race,
        Infrastructure.Persistence.Mongo.ClassDocument? characterClass,
        Infrastructure.Persistence.Mongo.BackgroundDocument? background)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["У персонажа должно быть имя."];
        }

        if (race is null)
        {
            errors["raceId"] = ["Выбери расу из справочника правил."];
        }

        if (characterClass is null)
        {
            errors["classId"] = ["Выбери класс из справочника правил."];
        }

        if (background is null)
        {
            errors["backgroundId"] = ["Выбери предысторию из справочника правил."];
        }

        if (request.Level is < 1 or > 20)
        {
            errors["level"] = ["Уровень персонажа должен быть от 1 до 20."];
        }

        if (request.BaseAbilities.Count != 6 ||
            request.BaseAbilities.Any(x => !AllowedAbilities.Contains(x.Key.ToUpperInvariant())) ||
            request.BaseAbilities.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() != 1) ||
            request.BaseAbilities.Any(x => x.Score < 3 || x.Score > 18))
        {
            errors["baseAbilities"] = ["Нужно передать шесть базовых характеристик в диапазоне 3..18."];
        }

        return errors;
    }

    private static List<string> ValidateSpells(
        CreateCharacterRequest request,
        Infrastructure.Persistence.Mongo.ClassDocument characterClass,
        IReadOnlyList<Infrastructure.Persistence.Mongo.SpellDocument> spellsCatalog)
    {
        var errors = new List<string>();
        if (request.Spells.Count == 0)
        {
            return errors;
        }

        var selected = request.Spells.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
        foreach (var spellName in selected)
        {
            var spell = spellsCatalog.FirstOrDefault(item =>
                item.Slug.Equals(spellName, StringComparison.OrdinalIgnoreCase) ||
                item.Name.Equals(spellName, StringComparison.OrdinalIgnoreCase));

            if (spell is null)
            {
                errors.Add($"Заклинание '{spellName}' не найдено в справочнике.");
                continue;
            }

            if (!spell.ClassSlugs.Contains(characterClass.Slug, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Заклинание '{spell.Name}' недоступно классу {characterClass.Name}.");
            }

            if (request.Level < spell.MinCharacterLevel)
            {
                errors.Add($"Заклинание '{spell.Name}' требует уровень {spell.MinCharacterLevel}+.");
            }
        }

        return errors;
    }
}

public sealed record CharacterCreationResult(CharacterEntity? Character, Dictionary<string, string[]>? Errors)
{
    public bool IsSuccess => Character is not null;

    public static CharacterCreationResult Success(CharacterEntity entity) => new(entity, null);
    public static CharacterCreationResult Failed(Dictionary<string, string[]> errors) => new(null, errors);
}
