using dnd_helper.Infrastructure.Persistence.Mongo;

namespace dnd_helper.Features.Rules;

public interface IRulesCatalogRepository
{
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RulesetDocument>> GetRulesetsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RaceDocument>> GetRacesAsync(string rulesetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassDocument>> GetClassesAsync(string rulesetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackgroundDocument>> GetBackgroundsAsync(string rulesetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeatureDocument>> GetFeaturesAsync(string rulesetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpellDocument>> GetSpellsAsync(string rulesetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EquipmentDocument>> GetEquipmentAsync(string rulesetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonsterDocument>> GetMonstersAsync(string rulesetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConditionDocument>> GetConditionsAsync(string rulesetId, CancellationToken cancellationToken = default);

    Task<RaceDocument?> GetRaceBySlugAsync(string rulesetId, string slug, CancellationToken cancellationToken = default);
    Task<ClassDocument?> GetClassBySlugAsync(string rulesetId, string slug, CancellationToken cancellationToken = default);
    Task<BackgroundDocument?> GetBackgroundBySlugAsync(string rulesetId, string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeatureDocument>> GetFeaturesBySlugsAsync(string rulesetId, IEnumerable<string> slugs, CancellationToken cancellationToken = default);

    Task UpsertRulesetAsync(RulesetDocument document, CancellationToken cancellationToken = default);
    Task UpsertRaceAsync(RaceDocument document, CancellationToken cancellationToken = default);
    Task UpsertClassAsync(ClassDocument document, CancellationToken cancellationToken = default);
    Task UpsertBackgroundAsync(BackgroundDocument document, CancellationToken cancellationToken = default);
    Task UpsertFeatureAsync(FeatureDocument document, CancellationToken cancellationToken = default);
    Task UpsertSpellAsync(SpellDocument document, CancellationToken cancellationToken = default);
    Task UpsertEquipmentAsync(EquipmentDocument document, CancellationToken cancellationToken = default);
    Task UpsertMonsterAsync(MonsterDocument document, CancellationToken cancellationToken = default);
    Task UpsertConditionAsync(ConditionDocument document, CancellationToken cancellationToken = default);
}
