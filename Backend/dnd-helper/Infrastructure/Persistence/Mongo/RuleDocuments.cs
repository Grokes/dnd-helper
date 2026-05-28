using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dnd_helper.Infrastructure.Persistence.Mongo;

public sealed class RulesetDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public abstract class RuleDocumentBase
{
    [BsonId]
    [BsonIgnoreIfDefault]
    public ObjectId Id { get; set; }

    public string RulesetId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = "PHB";
    public List<GrantEntry> Grants { get; set; } = [];
    public List<RequirementEntry> Requires { get; set; } = [];
    public List<ChoiceEntry> Choices { get; set; } = [];
    public List<EffectEntry> Effects { get; set; } = [];
    public List<ModifierEntry> Modifiers { get; set; } = [];
    public List<LevelFeatureEntry> Levels { get; set; } = [];
}

public sealed class RaceDocument : RuleDocumentBase
{
    public string ParentRace { get; set; } = string.Empty;
    public int Speed { get; set; } = 30;
}

public sealed class ClassDocument : RuleDocumentBase
{
    public int HitDie { get; set; } = 8;
    public List<string> SavingThrowProficiencies { get; set; } = [];
}

public sealed class BackgroundDocument : RuleDocumentBase;
public sealed class FeatureDocument : RuleDocumentBase;
public sealed class SpellDocument : RuleDocumentBase
{
    public List<string> ClassSlugs { get; set; } = [];
    public int SpellLevel { get; set; }
    public int MinCharacterLevel { get; set; } = 1;
}
public sealed class EquipmentDocument : RuleDocumentBase
{
    public string Category { get; set; } = string.Empty;
    public string? Subcategory { get; set; }
    public decimal? CostValue { get; set; }
    public string? CostUnit { get; set; }
    public decimal? WeightLb { get; set; }
    public string? DamageDice { get; set; }
    public string? DamageType { get; set; }
    public string? AttackAbility { get; set; }
    public List<string> WeaponProperties { get; set; } = [];
    public bool IsTwoHanded { get; set; }
    public bool IsShield { get; set; }
    public int? ArmorClassBase { get; set; }
    public string? EquipSlot { get; set; }
}

public sealed class MonsterDocument : RuleDocumentBase
{
    public string Size { get; set; } = string.Empty;
    public string CreatureType { get; set; } = string.Empty;
    public string Alignment { get; set; } = string.Empty;
    public decimal ChallengeRating { get; set; }
    public int ArmorClass { get; set; }
    public int HitPoints { get; set; }
    public string HitDice { get; set; } = string.Empty;
    public int Speed { get; set; }
    public string? AttackName { get; set; }
    public int AttackBonus { get; set; }
    public string? DamageDice { get; set; }
    public int DamageBonus { get; set; }
    public string? DamageType { get; set; }
}
public sealed class ConditionDocument : RuleDocumentBase;

public sealed record GrantEntry(string Type, string Value);
public sealed record RequirementEntry(string Type, string Value);
public sealed record ChoiceEntry(string Type, int Count, List<string> Options);
public sealed record EffectEntry(string Target, string Operation, int Value, string Reason);
public sealed record ModifierEntry(string Target, int Value, string Operation, string Reason);
public sealed record LevelFeatureEntry(int Level, List<string> FeatureSlugs);
