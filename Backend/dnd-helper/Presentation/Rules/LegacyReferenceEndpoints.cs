using dnd_helper.Infrastructure.Persistence.Mongo;
using dnd_helper.Infrastructure.Seeding;

namespace dnd_helper.Presentation.Rules;

public static class LegacyReferenceEndpoints
{
    private static IReadOnlyList<string> ResolveEquipmentItemsForProficiency(
        string proficiencyName,
        IReadOnlyList<Infrastructure.Persistence.Mongo.EquipmentDocument> equipment)
    {
        IEnumerable<Infrastructure.Persistence.Mongo.EquipmentDocument> query = proficiencyName switch
        {
            "Лёгкие доспехи" => equipment.Where(item => item.Category.Equals("Armor", StringComparison.OrdinalIgnoreCase)
                && (item.Subcategory?.Equals("Light", StringComparison.OrdinalIgnoreCase) ?? false)),
            "Средние доспехи" => equipment.Where(item => item.Category.Equals("Armor", StringComparison.OrdinalIgnoreCase)
                && (item.Subcategory?.Equals("Medium", StringComparison.OrdinalIgnoreCase) ?? false)),
            "Тяжёлые доспехи" => equipment.Where(item => item.Category.Equals("Armor", StringComparison.OrdinalIgnoreCase)
                && (item.Subcategory?.Equals("Heavy", StringComparison.OrdinalIgnoreCase) ?? false)),
            "Щиты" => equipment.Where(item => item.IsShield),
            "Простое оружие" => equipment.Where(item => item.Category.Equals("Simple Weapon", StringComparison.OrdinalIgnoreCase)),
            "Воинское оружие" => equipment.Where(item => item.Category.Equals("Martial Weapon", StringComparison.OrdinalIgnoreCase)),
            _ => equipment.Where(item => item.Name.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase))
        };

        return query
            .Select(item => item.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static FeatureDetailDto ToFeatureDetail(EffectEntry effect, string defaultTitle)
    {
        if (string.IsNullOrWhiteSpace(effect.Reason))
        {
            return new FeatureDetailDto(defaultTitle, string.Empty);
        }

        // Preserve class feature titles like "1 уровень: Ярость: описание",
        // so UI can group by level and still show actual feature names.
        var classFeatureMatch = System.Text.RegularExpressions.Regex.Match(
            effect.Reason,
            @"^\s*(\d+\s*уровень)\s*:\s*([^:]+)\s*:\s*(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (classFeatureMatch.Success)
        {
            var levelPart = classFeatureMatch.Groups[1].Value.Trim();
            var featurePart = classFeatureMatch.Groups[2].Value.Trim();
            var descriptionPart = classFeatureMatch.Groups[3].Value.Trim();
            return new FeatureDetailDto($"{levelPart}: {featurePart}", descriptionPart);
        }

        var separatorIndex = effect.Reason.IndexOf(':');
        if (separatorIndex > 0 && separatorIndex < effect.Reason.Length - 1)
        {
            var title = effect.Reason[..separatorIndex].Trim();
            var description = effect.Reason[(separatorIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(description))
            {
                return new FeatureDetailDto(title, description);
            }
        }

        return new FeatureDetailDto(defaultTitle, effect.Reason.Trim());
    }

    public static IEndpointRouteBuilder MapLegacyReferenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/reference/character-options", async (
            IRulesCatalogRepository repository,
            CancellationToken cancellationToken) =>
        {
            var races = await repository.GetRacesAsync(RulesDatabaseSeeder.RulesetId, cancellationToken);
            var classes = await repository.GetClassesAsync(RulesDatabaseSeeder.RulesetId, cancellationToken);
            var backgrounds = await repository.GetBackgroundsAsync(RulesDatabaseSeeder.RulesetId, cancellationToken);
            var equipment = await repository.GetEquipmentAsync(RulesDatabaseSeeder.RulesetId, cancellationToken);

            var dto = new CharacterOptionsDto(
                races.Select(race => new RaceOptionDto(
                    race.Slug,
                    string.IsNullOrWhiteSpace(race.ParentRace) ? race.Name : race.ParentRace,
                    race.Name,
                    race.Speed,
                    race.Modifiers
                        .Where(mod => mod.Target.StartsWith("ability:", StringComparison.OrdinalIgnoreCase))
                        .Select(mod => new AbilityBonusDto(mod.Target["ability:".Length..].ToUpperInvariant(), mod.Value))
                        .ToList(),
                    race.Choices.FirstOrDefault(choice => choice.Type.Equals("ability_bonus", StringComparison.OrdinalIgnoreCase)) is { } bonusChoice
                        ? new BonusChoiceRuleDto(bonusChoice.Count, 1, bonusChoice.Options, "Выбери характеристики для расового бонуса.")
                        : null,
                    race.Grants.Where(grant => grant.Type.Equals("skill", StringComparison.OrdinalIgnoreCase)).Select(grant => grant.Value).ToList(),
                    race.Choices.FirstOrDefault(choice => choice.Type.Equals("skill", StringComparison.OrdinalIgnoreCase)) is { } skillChoice
                        ? new SkillChoiceRuleDto(skillChoice.Count, skillChoice.Options, "Выбери навыки расы.")
                        : null,
                    race.Effects
                        .Where(effect => !effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))
                        .Where(effect => !effect.Target.Equals("lore", StringComparison.OrdinalIgnoreCase))
                        .Where(effect => !string.IsNullOrWhiteSpace(effect.Reason))
                        .Select(effect => ToFeatureDetail(effect, "Расовая особенность"))
                        .ToList(),
                    race.Effects.FirstOrDefault(effect => effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))?.Reason
                        ?? race.Name,
                    race.Grants.Where(grant => grant.Type.Equals("language", StringComparison.OrdinalIgnoreCase)).Select(grant => grant.Value).Distinct().ToList(),
                    race.Effects.FirstOrDefault(effect => effect.Target.Equals("lore", StringComparison.OrdinalIgnoreCase))?.Reason
                        ?? race.Effects.FirstOrDefault(effect => effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))?.Reason
                        ?? race.Name)).ToList(),
                classes.Select(characterClass => new ClassOptionDto(
                    characterClass.Slug,
                    characterClass.Name,
                    characterClass.HitDie,
                    [],
                    characterClass.SavingThrowProficiencies,
                    new SkillChoiceRuleDto(
                        characterClass.Choices.FirstOrDefault(x => x.Type.Equals("skill", StringComparison.OrdinalIgnoreCase))?.Count ?? 0,
                        characterClass.Choices.FirstOrDefault(x => x.Type.Equals("skill", StringComparison.OrdinalIgnoreCase))?.Options ?? [],
                        "Выбери навыки класса."),
                    characterClass.Effects
                        .Where(effect => !effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))
                        .Where(effect => !effect.Target.Equals("lore", StringComparison.OrdinalIgnoreCase))
                        .Where(effect => !string.IsNullOrWhiteSpace(effect.Reason))
                        .Select(effect => ToFeatureDetail(effect, "Классовая особенность"))
                        .ToList(),
                    characterClass.Effects.FirstOrDefault(effect => effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))?.Reason
                        ?? characterClass.Name,
                    characterClass.Effects.FirstOrDefault(effect => effect.Target.Equals("lore", StringComparison.OrdinalIgnoreCase))?.Reason
                        ?? characterClass.Effects.FirstOrDefault(effect => effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))?.Reason
                        ?? characterClass.Name,
                    CharacterOptionsCatalog.ClassProficiencyGroups.TryGetValue(characterClass.Slug, out var groups)
                        ? groups.ToDictionary(
                            entry => entry.Key,
                            entry => (IReadOnlyList<string>)entry.Value
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                                .ToList())
                        : new Dictionary<string, IReadOnlyList<string>>())).ToList(),
                backgrounds.Select(background => new BackgroundOptionDto(
                    background.Slug,
                    background.Name,
                    background.Grants.Where(grant => grant.Type.Equals("skill", StringComparison.OrdinalIgnoreCase)).Select(grant => grant.Value).ToList(),
                    background.Effects
                        .Where(effect => !effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))
                        .Where(effect => !effect.Target.Equals("lore", StringComparison.OrdinalIgnoreCase))
                        .Where(effect => !string.IsNullOrWhiteSpace(effect.Reason))
                        .Select(effect => ToFeatureDetail(effect, "Особенность предыстории"))
                        .ToList(),
                    background.Effects.FirstOrDefault(effect => effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))?.Reason
                        ?? background.Name,
                    background.Effects.FirstOrDefault(effect => effect.Target.Equals("lore", StringComparison.OrdinalIgnoreCase))?.Reason
                        ?? background.Effects.FirstOrDefault(effect => effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))?.Reason
                        ?? background.Name)).ToList(),
                CharacterOptionsCatalog.SkillAbilityMap);

            return Results.Ok(dto);
        });

        return endpoints;
    }

}
