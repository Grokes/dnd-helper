using dnd_helper.Infrastructure.Persistence.Mongo;

namespace dnd_helper.Infrastructure.Seeding;

public sealed partial class RulesDatabaseSeeder
{
    private async Task SeedConditionsAsync(CancellationToken cancellationToken)
    {
        var conditions = new[]
        {
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "blinded", Name = "Ослеплён",
                Effects = [new EffectEntry("description", "set", 0, "Существо не видит и автоматически проваливает проверки, требующие зрения; атаки по нему с преимуществом, его атаки — с помехой.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "charmed", Name = "Очарован",
                Effects = [new EffectEntry("description", "set", 0, "Существо не может атаковать очаровавшего или делать его целью вредоносных эффектов; очаровавший имеет преимущество на социальные проверки.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "deafened", Name = "Оглохший",
                Effects = [new EffectEntry("description", "set", 0, "Существо не слышит и автоматически проваливает проверки, требующие слуха.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "exhaustion", Name = "Истощение",
                Effects = [new EffectEntry("description", "set", 0, "Имеет 6 уровней; от помехи на проверки до смерти на 6 уровне. Эффекты уровней истощения суммируются.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "poisoned", Name = "Отравлен",
                Effects = [new EffectEntry("description", "set", 0, "Существо совершает броски атаки и проверки характеристик с помехой.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "prone", Name = "Сбит с ног",
                Effects = [new EffectEntry("description", "set", 0, "Передвижение ползком; атаки по существу вблизи с преимуществом, издалека — с помехой.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "grappled", Name = "Схвачен",
                Effects = [new EffectEntry("description", "set", 0, "Скорость становится 0, состояние оканчивается при недееспособности схватившего или выходе из захвата.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "incapacitated", Name = "Недееспособен",
                Effects = [new EffectEntry("description", "set", 0, "Существо не может совершать действия и реакции.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "invisible", Name = "Невидим",
                Effects = [new EffectEntry("description", "set", 0, "Существо невозможно увидеть без магии/особых чувств; его атаки с преимуществом, атаки по нему — с помехой.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "paralyzed", Name = "Парализован",
                Effects = [new EffectEntry("description", "set", 0, "Существо недееспособно, не может двигаться/говорить, автоматически проваливает спасброски Силы и Ловкости; атаки по нему с преимуществом, криты вблизи.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "petrified", Name = "Окаменел",
                Effects = [new EffectEntry("description", "set", 0, "Существо превращается в твёрдый объект, недееспособно, не двигается и не осознаёт окружение; имеет сопротивление всем видам урона.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "frightened", Name = "Испуган",
                Effects = [new EffectEntry("description", "set", 0, "Существо совершает проверки характеристик и броски атаки с помехой, пока источник страха в поле зрения.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "restrained", Name = "Опутан",
                Effects = [new EffectEntry("description", "set", 0, "Скорость 0; атаки существа с помехой, атаки по нему с преимуществом; помеха на спасброски Ловкости.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "stunned", Name = "Ошеломлён",
                Effects = [new EffectEntry("description", "set", 0, "Существо недееспособно, не может двигаться, говорит с трудом, автоматически проваливает спасброски Силы и Ловкости; атаки по нему с преимуществом.")]
            },
            new ConditionDocument
            {
                RulesetId = RulesetId, Slug = "unconscious", Name = "Без сознания",
                Effects = [new EffectEntry("description", "set", 0, "Существо недееспособно, не двигается и не осознаёт окружение, роняет предметы; атаки по нему с преимуществом, криты вблизи.")]
            }
        };

        foreach (var item in conditions)
        {
            await rulesRepository.UpsertConditionAsync(item, cancellationToken);
        }
    }
}
