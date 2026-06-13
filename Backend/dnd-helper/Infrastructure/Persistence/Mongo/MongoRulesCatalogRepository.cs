using MongoDB.Driver;

namespace dnd_helper.Infrastructure.Persistence.Mongo;

public sealed class MongoRulesCatalogRepository(IMongoDatabase database) : IRulesCatalogRepository
{
    private readonly IMongoCollection<RulesetDocument> _rulesets = database.GetCollection<RulesetDocument>("rulesets");
    private readonly IMongoCollection<RaceDocument> _races = database.GetCollection<RaceDocument>("races");
    private readonly IMongoCollection<ClassDocument> _classes = database.GetCollection<ClassDocument>("classes");
    private readonly IMongoCollection<BackgroundDocument> _backgrounds = database.GetCollection<BackgroundDocument>("backgrounds");
    private readonly IMongoCollection<FeatureDocument> _features = database.GetCollection<FeatureDocument>("features");
    private readonly IMongoCollection<SpellDocument> _spells = database.GetCollection<SpellDocument>("spells");
    private readonly IMongoCollection<EquipmentDocument> _equipment = database.GetCollection<EquipmentDocument>("equipment");
    private readonly IMongoCollection<MonsterDocument> _monsters = database.GetCollection<MonsterDocument>("monsters");
    private readonly IMongoCollection<ConditionDocument> _conditions = database.GetCollection<ConditionDocument>("conditions");

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await DeduplicateRulesetCollectionAsync(cancellationToken);
        await DeduplicateRulesCollectionAsync(_races, cancellationToken);
        await DeduplicateRulesCollectionAsync(_classes, cancellationToken);
        await DeduplicateRulesCollectionAsync(_backgrounds, cancellationToken);
        await DeduplicateRulesCollectionAsync(_features, cancellationToken);
        await DeduplicateRulesCollectionAsync(_spells, cancellationToken);
        await DeduplicateRulesCollectionAsync(_equipment, cancellationToken);
        await DeduplicateRulesCollectionAsync(_monsters, cancellationToken);
        await DeduplicateRulesCollectionAsync(_conditions, cancellationToken);

        await CreateRulesetSlugIndexAsync(_races, cancellationToken);
        await CreateRulesetSlugIndexAsync(_classes, cancellationToken);
        await CreateRulesetSlugIndexAsync(_backgrounds, cancellationToken);
        await CreateRulesetSlugIndexAsync(_features, cancellationToken);
        await CreateRulesetSlugIndexAsync(_spells, cancellationToken);
        await CreateRulesetSlugIndexAsync(_equipment, cancellationToken);
        await CreateRulesetSlugIndexAsync(_monsters, cancellationToken);
        await CreateRulesetSlugIndexAsync(_conditions, cancellationToken);
        await _rulesets.Indexes.CreateOneAsync(
            new CreateIndexModel<RulesetDocument>(
                Builders<RulesetDocument>.IndexKeys.Ascending(x => x.Slug),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<RulesetDocument>> GetRulesetsAsync(CancellationToken cancellationToken = default)
        => await _rulesets.Find(Builders<RulesetDocument>.Filter.Empty).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RaceDocument>> GetRacesAsync(string rulesetId, CancellationToken cancellationToken = default)
        => await GetByRulesetAsync(_races, rulesetId, cancellationToken);

    public async Task<IReadOnlyList<ClassDocument>> GetClassesAsync(string rulesetId, CancellationToken cancellationToken = default)
        => await GetByRulesetAsync(_classes, rulesetId, cancellationToken);

    public async Task<IReadOnlyList<BackgroundDocument>> GetBackgroundsAsync(string rulesetId, CancellationToken cancellationToken = default)
        => await GetByRulesetAsync(_backgrounds, rulesetId, cancellationToken);

    public async Task<IReadOnlyList<FeatureDocument>> GetFeaturesAsync(string rulesetId, CancellationToken cancellationToken = default)
        => await GetByRulesetAsync(_features, rulesetId, cancellationToken);

    public async Task<IReadOnlyList<SpellDocument>> GetSpellsAsync(string rulesetId, CancellationToken cancellationToken = default)
        => await GetByRulesetAsync(_spells, rulesetId, cancellationToken);

    public async Task<IReadOnlyList<EquipmentDocument>> GetEquipmentAsync(string rulesetId, CancellationToken cancellationToken = default)
        => await GetByRulesetAsync(_equipment, rulesetId, cancellationToken);

    public async Task<IReadOnlyList<MonsterDocument>> GetMonstersAsync(string rulesetId, CancellationToken cancellationToken = default)
        => await GetByRulesetAsync(_monsters, rulesetId, cancellationToken);

    public async Task<IReadOnlyList<ConditionDocument>> GetConditionsAsync(string rulesetId, CancellationToken cancellationToken = default)
        => await GetByRulesetAsync(_conditions, rulesetId, cancellationToken);

    public async Task<RaceDocument?> GetRaceBySlugAsync(string rulesetId, string slug, CancellationToken cancellationToken = default)
        => await _races.Find(Builders<RaceDocument>.Filter.And(
                Builders<RaceDocument>.Filter.Eq(x => x.RulesetId, rulesetId),
                Builders<RaceDocument>.Filter.Eq(x => x.Slug, slug)))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ClassDocument?> GetClassBySlugAsync(string rulesetId, string slug, CancellationToken cancellationToken = default)
        => await _classes.Find(Builders<ClassDocument>.Filter.And(
                Builders<ClassDocument>.Filter.Eq(x => x.RulesetId, rulesetId),
                Builders<ClassDocument>.Filter.Eq(x => x.Slug, slug)))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<BackgroundDocument?> GetBackgroundBySlugAsync(string rulesetId, string slug, CancellationToken cancellationToken = default)
        => await _backgrounds.Find(Builders<BackgroundDocument>.Filter.And(
                Builders<BackgroundDocument>.Filter.Eq(x => x.RulesetId, rulesetId),
                Builders<BackgroundDocument>.Filter.Eq(x => x.Slug, slug)))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<FeatureDocument>> GetFeaturesBySlugsAsync(string rulesetId, IEnumerable<string> slugs, CancellationToken cancellationToken = default)
    {
        var slugArray = slugs.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (slugArray.Length == 0)
        {
            return [];
        }

        return await _features.Find(Builders<FeatureDocument>.Filter.And(
                Builders<FeatureDocument>.Filter.Eq(x => x.RulesetId, rulesetId),
                Builders<FeatureDocument>.Filter.In(x => x.Slug, slugArray)))
            .ToListAsync(cancellationToken);
    }

    public Task UpsertRulesetAsync(RulesetDocument document, CancellationToken cancellationToken = default)
        => UpsertRulesetCoreAsync(_rulesets, document, cancellationToken);

    public Task UpsertRaceAsync(RaceDocument document, CancellationToken cancellationToken = default)
        => UpsertRuleAsync(_races, document, cancellationToken);

    public Task UpsertClassAsync(ClassDocument document, CancellationToken cancellationToken = default)
        => UpsertRuleAsync(_classes, document, cancellationToken);

    public Task UpsertBackgroundAsync(BackgroundDocument document, CancellationToken cancellationToken = default)
        => UpsertRuleAsync(_backgrounds, document, cancellationToken);

    public Task UpsertFeatureAsync(FeatureDocument document, CancellationToken cancellationToken = default)
        => UpsertRuleAsync(_features, document, cancellationToken);

    public Task UpsertSpellAsync(SpellDocument document, CancellationToken cancellationToken = default)
        => UpsertRuleAsync(_spells, document, cancellationToken);

    public Task UpsertEquipmentAsync(EquipmentDocument document, CancellationToken cancellationToken = default)
        => UpsertRuleAsync(_equipment, document, cancellationToken);

    public Task UpsertMonsterAsync(MonsterDocument document, CancellationToken cancellationToken = default)
        => UpsertRuleAsync(_monsters, document, cancellationToken);

    public Task UpsertConditionAsync(ConditionDocument document, CancellationToken cancellationToken = default)
        => UpsertRuleAsync(_conditions, document, cancellationToken);

    private static async Task CreateRulesetSlugIndexAsync<TDocument>(IMongoCollection<TDocument> collection, CancellationToken cancellationToken)
        where TDocument : RuleDocumentBase
    {
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TDocument>(
                Builders<TDocument>.IndexKeys.Ascending(x => x.RulesetId).Ascending(x => x.Slug),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);
    }

    private static async Task<IReadOnlyList<TDocument>> GetByRulesetAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        string rulesetId,
        CancellationToken cancellationToken)
        where TDocument : RuleDocumentBase
    {
        return await collection.Find(Builders<TDocument>.Filter.Eq(x => x.RulesetId, rulesetId)).ToListAsync(cancellationToken);
    }

    private static async Task UpsertRuleAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        TDocument document,
        CancellationToken cancellationToken)
        where TDocument : RuleDocumentBase
    {
        await collection.ReplaceOneAsync(
            Builders<TDocument>.Filter.And(
                Builders<TDocument>.Filter.Eq(x => x.RulesetId, document.RulesetId),
                Builders<TDocument>.Filter.Eq(x => x.Slug, document.Slug)),
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private static async Task UpsertRulesetCoreAsync(
        IMongoCollection<RulesetDocument> collection,
        RulesetDocument document,
        CancellationToken cancellationToken)
    {
        await collection.ReplaceOneAsync(
            Builders<RulesetDocument>.Filter.Eq(x => x.Slug, document.Slug),
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private async Task DeduplicateRulesetCollectionAsync(CancellationToken cancellationToken)
    {
        var rulesets = await _rulesets
            .Find(Builders<RulesetDocument>.Filter.Empty)
            .SortByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        var duplicateIds = rulesets
            .GroupBy(item => item.Slug, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => group.Skip(1))
            .Select(item => item.Id)
            .ToArray();

        if (duplicateIds.Length == 0)
        {
            return;
        }

        await _rulesets.DeleteManyAsync(
            Builders<RulesetDocument>.Filter.In(item => item.Id, duplicateIds),
            cancellationToken);
    }

    private static async Task DeduplicateRulesCollectionAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken)
        where TDocument : RuleDocumentBase
    {
        var documents = await collection
            .Find(Builders<TDocument>.Filter.Empty)
            .SortByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        var duplicateIds = documents
            .GroupBy(item => $"{item.RulesetId}::{item.Slug}", StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => group.Skip(1))
            .Select(item => item.Id)
            .ToArray();

        if (duplicateIds.Length == 0)
        {
            return;
        }

        await collection.DeleteManyAsync(
            Builders<TDocument>.Filter.In(item => item.Id, duplicateIds),
            cancellationToken);
    }
}
