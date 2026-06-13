using dnd_helper.Infrastructure.Persistence.Mongo;
using Microsoft.Extensions.Logging;

namespace dnd_helper.Infrastructure.Seeding;

public sealed partial class RulesDatabaseSeeder(
    IRulesCatalogRepository rulesRepository,
    ILogger<RulesDatabaseSeeder> logger)
{
    public const string RulesetId = "phb-5e-rus";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Mongo: creating indexes for rules catalog...");
        await rulesRepository.EnsureIndexesAsync(cancellationToken);

        logger.LogInformation("Mongo: upserting ruleset and PHB data...");
        await rulesRepository.UpsertRulesetAsync(new RulesetDocument
        {
            Id = RulesetId,
            Slug = "phb-5e-rus",
            Name = "D&D 5e Player's Handbook (RUS)",
            Source = "5e Players Handbook"
        }, cancellationToken);

        await SeedRacesAsync(cancellationToken);
        await SeedClassesAsync(cancellationToken);
        await SeedBackgroundsAsync(cancellationToken);
        await SeedFeaturesAsync(cancellationToken);
        await SeedSpellsAsync(cancellationToken);
        await SeedEquipmentAsync(cancellationToken);
        await SeedMonstersAsync(cancellationToken);
        await SeedConditionsAsync(cancellationToken);
    }

    private async Task SeedRacesAsync(CancellationToken cancellationToken)
    {
        foreach (var race in CharacterOptionsCatalog.Races)
        {
            var mergedDetails = MergeRaceDetails(race);
            var grants = race.GrantedSkillProficiencies.Select(skill => new GrantEntry("skill", skill)).ToList();
            grants.AddRange(GetRaceLanguageGrants(race).Select(language => new GrantEntry("language", language)));

            var document = new RaceDocument
            {
                RulesetId = RulesetId,
                Slug = race.Id,
                ParentRace = race.ParentRace,
                Name = race.Name,
                Speed = race.Speed,
                Source = "PHB",
                Modifiers = race.Bonuses
                    .Select(bonus => new ModifierEntry($"ability:{bonus.Ability}", bonus.Value, "add", "Расовый бонус"))
                    .ToList(),
                Grants = grants.DistinctBy(grant => $"{grant.Type}:{grant.Value}").ToList(),
                Choices = BuildRaceChoices(race),
                Effects =
                [
                    new EffectEntry("summary", "set", 0, BuildRaceSummary(race)),
                    new EffectEntry("lore", "set", 0, BuildRaceLore(race)),
                    ..mergedDetails.Select(detail => new EffectEntry("feature", "set", 0, $"{detail.Title}: {detail.Description}"))
                ]
            };

            await rulesRepository.UpsertRaceAsync(document, cancellationToken);
        }
    }

    private static IReadOnlyList<string> GetRaceLanguageGrants(RaceOptionDto race)
    {
        return race.Id switch
        {
            "half-orc" => ["Общий", "Орочий"],
            "half-elf" => ["Общий", "Эльфийский", "Дополнительный язык на выбор"],
            "tiefling" => ["Общий", "Инфернальный"],
            _ => race.ParentRace switch
            {
                "Человек" => ["Общий", "Дополнительный язык на выбор"],
                "Дворф" => ["Общий", "Дварфийский"],
                "Эльф" => ["Общий", "Эльфийский"],
                "Полурослик" => ["Общий", "Полуросличий"],
                "Драконорождённый" => ["Общий", "Драконий"],
                "Гном" => ["Общий", "Гномий"],
                _ => ["Общий"]
            }
        };
    }

    private static IReadOnlyList<FeatureDetailDto> MergeRaceDetails(RaceOptionDto race)
    {
        var common = race.ParentRace switch
        {
            "Дворф" => new[]
            {
                new FeatureDetailDto("Тёмное зрение", "Вы видите в тусклом свете в пределах 60 футов как при ярком свете, а в темноте — как при тусклом."),
                new FeatureDetailDto("Дворфийская устойчивость", "Вы совершаете с преимуществом спасброски от яда и получаете сопротивление урону ядом."),
                new FeatureDetailDto("Дворфийская боевая подготовка", "Вы владеете боевым топором, ручным топором, лёгким молотом и боевым молотом."),
                new FeatureDetailDto("Владение инструментами", "Вы владеете одним набором ремесленных инструментов на выбор: инструменты кузнеца, пивовара или каменщика."),
                new FeatureDetailDto("Знание камня", "При проверках Истории, связанных с происхождением каменной кладки, вы добавляете удвоенный бонус мастерства."),
                new FeatureDetailDto("Медлительность в доспехах не мешает", "Ваша скорость не уменьшается от ношения тяжёлых доспехов.")
            },
            "Эльф" => new[]
            {
                new FeatureDetailDto("Тёмное зрение", "Вы видите в тусклом свете в пределах 60 футов как при ярком свете, а в темноте — как при тусклом."),
                new FeatureDetailDto("Наследие фей", "Вы совершаете с преимуществом спасброски от состояния очарования, и вас невозможно магически усыпить."),
                new FeatureDetailDto("Транс", "Эльфы не спят. Вместо сна вы медитируете 4 часа в сутки."),
                new FeatureDetailDto("Обострённые чувства", "Вы владеете навыком Внимательность.")
            },
            "Полурослик" => new[]
            {
                new FeatureDetailDto("Везучий", "Если при броске атаки, проверке характеристики или спасброске выпало «1» на d20, вы можете перебросить кость."),
                new FeatureDetailDto("Храбрый", "Вы совершаете с преимуществом спасброски от состояния испуга."),
                new FeatureDetailDto("Проворство полуросликов", "Вы можете проходить сквозь пространство существ размером как минимум на одну категорию больше вашего.")
            },
            "Гном" => new[]
            {
                new FeatureDetailDto("Тёмное зрение", "Вы видите в тусклом свете в пределах 60 футов как при ярком свете, а в темноте — как при тусклом."),
                new FeatureDetailDto("Гномья хитрость", "Вы совершаете с преимуществом спасброски Интеллекта, Мудрости и Харизмы против магии.")
            },
            _ => Array.Empty<FeatureDetailDto>()
        };

        var merged = common.Concat(race.Details).ToList();
        return merged
            .GroupBy(item => item.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static string BuildRaceSummary(RaceOptionDto race)
    {
        return race.Id switch
        {
            "human" => "Человек амбициозен и универсален, быстро приспосабливается к любой роли и культуре.",
            "hill-dwarf" => "Холмовой дворф — стойкий хранитель традиций с природной выносливостью и крепким здоровьем.",
            "mountain-dwarf" => "Горный дворф — суровый воитель кланов, привыкший к битве, металлу и камню.",
            "high-elf" => "Высший эльф — утончённый наследник древних королевств, сочетающий грацию и магию.",
            "wood-elf" => "Лесной эльф — быстрый страж чащоб, незаметный разведчик и мастер природной охоты.",
            "dark-elf" => "Тёмный эльф — опасный и гордый изгнанник Подземья, владеющий врождённой магией дроу.",
            "lightfoot-halfling" => "Легконогий полурослик — общительный странник, живущий хитростью, удачей и лёгкостью шага.",
            "stout-halfling" => "Коренастый полурослик — упрямый и выносливый малый народ, устойчивый к ядам и невзгодам.",
            "dragonborn" => "Драконорождённый — наследник драконьей крови, чести клана и разрушительного дыхания.",
            "forest-gnome" => "Лесной гном — скрытный друг зверей и мастер мелкой иллюзии, живущий в тени чащ.",
            "rock-gnome" => "Скальный гном — любознательный изобретатель, ценящий ремесло, механизмы и знания.",
            "half-elf" => "Полуэльф — посредник двух миров, сочетающий человеческую гибкость и эльфийскую чуткость.",
            "half-orc" => "Полуорк — могучий выживший с неукротимой яростью и упорством в бою.",
            "tiefling" => "Тифлинг — носитель инфернального наследия, привыкший к предубеждению и тайной силе.",
            _ => race.Summary
        };
    }

    private static string BuildRaceLore(RaceOptionDto race)
    {
        return race.Id switch
        {
            "human" => "Люди живут недолго, но действуют решительно. Они строят империи, кочуют, торгуют и смешиваются с другими народами, стремясь оставить след в мире уже при своей жизни.",
            "hill-dwarf" => "Холмовые дворфы славятся крепким здоровьем, долголетием и преданностью клану. Они чтят предков, ремесло и каменные залы, где поколениями хранятся клятвы и богатства рода.",
            "mountain-dwarf" => "Горные дворфы вырастают среди суровых хребтов и крепостей, вырубленных в скале. Они горды, дисциплинированы и привыкли защищать честь клана сталью и щитом.",
            "high-elf" => "Высшие эльфы хранят древние знания, поэзию и магические традиции. Их культура утончённа, а взгляд на мир охватывает века, поэтому они действуют неторопливо, но дальновидно.",
            "wood-elf" => "Лесные эльфы живут в гармонии с дикой природой. Они осторожны с чужаками, но бесценны как следопыты и стражи, умеющие растворяться среди ветвей и теней.",
            "dark-elf" => "Дроу происходят из подземных городов, где выживают хитростью, волей и жёсткой иерархией. На поверхности они редки и почти всегда вызывают настороженность.",
            "lightfoot-halfling" => "Легконогие полурослики любят дороги, истории и хорошие компании. Их дружелюбие и природная удачливость помогают им выбираться из неприятностей, в которые они нередко попадают.",
            "stout-halfling" => "Коренастые полурослики плотнее сложены и известны стойкостью к ядам. Они семейны, практичны и упрямы, а их спокойный нрав скрывает удивительную внутреннюю крепость.",
            "dragonborn" => "Драконорождённые ценят личную честь, самоконтроль и верность слову. Их внешность напоминает о древних драконах, а дыхание отражает силу родовой линии.",
            "forest-gnome" => "Лесные гномы скрытны, доброжелательны и особенно близки к мелким зверям. Они любят тихие поселения, иллюзии и тонкие шутки, незаметные для чужака.",
            "rock-gnome" => "Скальные гномы прославлены любознательностью и ремесленной смекалкой. Для них изобретение — форма искусства, а любая сложная задача вызывает азарт.",
            "half-elf" => "Полуэльфы редко чувствуют себя полностью «своими» в любом из двух народов, но именно это делает их наблюдательными, дипломатичными и гибкими в общении.",
            "half-orc" => "Полуорки живут на стыке двух жёстких миров. Им приходится постоянно доказывать себя, и потому они часто вырастают сильными, решительными и бесстрашными.",
            "tiefling" => "Тифлинги несут на себе печать инфернальных сделок прошлого. Их внешность вызывает подозрение, но многие из них строят собственный путь и ломают чужие предрассудки.",
            _ => race.Summary
        };
    }

    private async Task SeedClassesAsync(CancellationToken cancellationToken)
    {
        foreach (var classOption in CharacterOptionsCatalog.Classes)
        {
            var classDetails = BuildClassDetailsFromPhb(classOption);
            var featureSlug = $"{classOption.Id}-core-feature";
            var classDocument = new ClassDocument
            {
                RulesetId = RulesetId,
                Slug = classOption.Id,
                Name = classOption.Name,
                HitDie = classOption.HitDie,
                Source = "PHB",
                SavingThrowProficiencies = classOption.SavingThrowProficiencies.ToList(),
                Choices =
                [
                    new ChoiceEntry(
                        "skill",
                        classOption.SkillChoiceRule.Count,
                        classOption.SkillChoiceRule.AvailableSkills.ToList())
                ],
                Effects =
                [
                    new EffectEntry("summary", "set", 0, classOption.Summary),
                    new EffectEntry("lore", "set", 0, classOption.Description ?? classOption.Summary),
                    ..classDetails.Select(detail => new EffectEntry("feature", "set", 0, $"{detail.Title}: {detail.Description}"))
                ],
                Levels = [new LevelFeatureEntry(1, [featureSlug])]
            };

            await rulesRepository.UpsertClassAsync(classDocument, cancellationToken);
        }
    }

    private async Task SeedBackgroundsAsync(CancellationToken cancellationToken)
    {
        foreach (var background in CharacterOptionsCatalog.Backgrounds)
        {
            var details = BuildBackgroundDetailsFromPhb(background);
            await rulesRepository.UpsertBackgroundAsync(new BackgroundDocument
            {
                RulesetId = RulesetId,
                Slug = background.Id,
                Name = background.Name,
                Source = "PHB",
                Grants = background.GrantedSkillProficiencies.Select(skill => new GrantEntry("skill", skill)).ToList(),
                Effects =
                [
                    new EffectEntry("summary", "set", 0, BuildBackgroundSummary(background)),
                    new EffectEntry("lore", "set", 0, BuildBackgroundLore(background)),
                    ..details.Select(detail => new EffectEntry("feature", "set", 0, $"{detail.Title}: {detail.Description}"))
                ]
            }, cancellationToken);
        }
    }

    private async Task SeedFeaturesAsync(CancellationToken cancellationToken)
    {
        var features = new List<FeatureDocument>();

        foreach (var classOption in CharacterOptionsCatalog.Classes)
        {
            var classDetails = BuildClassDetailsFromPhb(classOption);
            features.Add(new FeatureDocument
            {
                RulesetId = RulesetId,
                Slug = $"{classOption.Id}-core-feature",
                Name = $"{classOption.Name}: базовые особенности",
                Source = "PHB",
                Effects = classDetails
                    .Select(detail => new EffectEntry("feature", "set", 0, $"{detail.Title}: {detail.Description}"))
                    .ToList()
            });
        }

        foreach (var race in CharacterOptionsCatalog.Races)
        {
            foreach (var detail in race.Details)
            {
                features.Add(new FeatureDocument
                {
                    RulesetId = RulesetId,
                    Slug = $"{race.Id}-{Slugify(detail.Title)}",
                    Name = $"{race.Name}: {detail.Title}",
                    Source = "PHB",
                    Effects = [new EffectEntry("feature", "set", 0, detail.Description)]
                });
            }
        }

        foreach (var item in features)
        {
            await rulesRepository.UpsertFeatureAsync(item, cancellationToken);
        }
    }

    private async Task SeedSpellsAsync(CancellationToken cancellationToken)
    {
        var spells = new[]
        {
            // Cantrips
            Spell("light", "Свет", 0, ["bard", "cleric", "sorcerer", "wizard"], "Создаёт источник света.", "Один объект начинает светиться: яркий свет в радиусе 20 футов и тусклый ещё на 20 футов."),
            Spell("mage-hand", "Волшебная рука", 0, ["bard", "sorcerer", "warlock", "wizard"], "Призрачная рука для простых манипуляций.", "Создаёт спектральную руку, способную взаимодействовать с объектами в пределах действия."),
            Spell("minor-illusion", "Малая иллюзия", 0, ["bard", "sorcerer", "warlock", "wizard"], "Небольшая иллюзия звука или изображения.", "Создаёт звук или неподвижное изображение в пределах действия заклинания."),
            Spell("sacred-flame", "Священное пламя", 0, ["cleric"], "Луч света поражает цель.", "Цель совершает спасбросок Ловкости, получая урон излучением при провале."),
            Spell("eldritch-blast", "Мистический разряд", 0, ["warlock"], "Фирменный атакующий заговор колдуна.", "Луч энергии наносит урон силовым полем при попадании."),
            Spell("guidance", "Наставление", 0, ["cleric", "druid"], "Кратковременное усиление проверки.", "Касанием даёте цели 1d4 к одной проверке характеристики перед броском."),
            Spell("prestidigitation", "Малое волшебство", 0, ["bard", "sorcerer", "warlock", "wizard"], "Набор мелких магических эффектов.", "Создаёт небольшой сенсорный/визуальный эффект, очищает, подогревает и выполняет простые трюки."),
            Spell("ray-of-frost", "Луч холода", 0, ["sorcerer", "wizard"], "Холодный луч замедляет цель.", "При попадании наносит урон холодом и уменьшает скорость цели до начала вашего следующего хода."),
            Spell("fire-bolt", "Огненный снаряд", 0, ["sorcerer", "wizard"], "Дальний огненный заговор.", "Наносит урон огнём при попадании; может поджечь легко воспламеняющиеся объекты."),
            Spell("shocking-grasp", "Электрошок", 0, ["sorcerer", "wizard"], "Ближний электрический разряд.", "Наносит урон электричеством; цель не может совершать реакции до своего следующего хода."),
            Spell("thaumaturgy", "Чудотворство", 0, ["cleric"], "Небольшие божественные эффекты.", "Усиливает голос, создаёт безвредные эффекты, колеблет пламя, открывает/закрывает незапертые двери."),
            Spell("vicious-mockery", "Злобная насмешка", 0, ["bard"], "Психический урон и ослабление атаки.", "Цель получает психический урон и помеху на следующий бросок атаки при провале спасброска Мудрости."),

            // 1st level
            Spell("healing-word", "Лечащее слово", 1, ["bard", "cleric", "druid"], "Лечение на расстоянии бонусным действием.", "Существо по выбору в пределах 60 футов восстанавливает 1d4 + модификатор базовой характеристики заклинателя хитов."),
            Spell("bless", "Благословение", 1, ["cleric", "paladin"], "Усиливает атаки и спасброски союзников.", "До трёх существ получают бонус 1d4 к броскам атаки и спасброскам, пока действует концентрация."),
            Spell("chaos-bolt", "Снаряд хаоса", 1, ["sorcerer"], "Непредсказуемый энергетический урон.", "Цель получает 2d8 + 1d6 урона случайного типа; при совпадении d8 разряд может перескочить на другую цель."),
            Spell("cure-wounds", "Лечение ран", 1, ["bard", "cleric", "druid", "paladin", "ranger"], "Надёжное касательное исцеление.", "Существо, которого вы касаетесь, восстанавливает 1d8 + модификатор базовой характеристики заклинателя хитов."),
            Spell("shield-of-faith", "Щит веры", 1, ["cleric", "paladin"], "Концентрационное усиление защиты.", "Выбранное существо получает +2 к КД на время действия заклинания (концентрация, до 10 минут)."),
            Spell("mage-armor", "Доспех мага", 1, ["sorcerer", "wizard"], "Магическая защита без доспеха.", "Если цель не носит доспех, её КД становится равен 13 + модификатор Ловкости на 8 часов."),
            Spell("detect-magic", "Обнаружение магии", 1, ["bard", "cleric", "druid", "paladin", "ranger", "sorcerer", "wizard"], "Выявляет магию поблизости.", "Пока действует заклинание, вы чувствуете присутствие магии в пределах 30 футов и видите школы магии объектов/существ."),
            Spell("magic-missile", "Волшебная стрела", 1, ["sorcerer", "wizard"], "Гарантированное попадание по цели.", "Три энергетических дротика автоматически попадают по существам в пределах действия."),
            Spell("shield", "Щит", 1, ["sorcerer", "wizard"], "Реакция для резкого повышения КД.", "До начала вашего следующего хода вы получаете +5 к КД, включая против вызвавшей атаку."),
            Spell("sleep", "Сон", 1, ["bard", "sorcerer", "wizard"], "Усыпляет существ с наименьшими хп.", "Существа в области засыпают, пока сумма их текущих хитов не превысит бросок 5d8."),
            Spell("chromatic-orb", "Хроматическая сфера", 1, ["sorcerer", "wizard"], "Сильный одиночный урон выбранного типа.", "Сфера энергии наносит 3d8 выбранного урона при попадании."),
            Spell("burning-hands", "Огненные ладони", 1, ["sorcerer", "wizard"], "Конус огня перед заклинателем.", "Существа в 15-футовом конусе получают 3d6 огненного урона при провале спасброска Ловкости."),
            Spell("thunderwave", "Громовая волна", 1, ["bard", "druid", "sorcerer", "wizard"], "Урон и отталкивание вокруг заклинателя.", "Существа в кубе 15 футов получают урон звуком и отталкиваются при провале спасброска Телосложения."),
            Spell("identify", "Опознание", 1, ["bard", "wizard"], "Определяет свойства магического предмета.", "Вы узнаёте магические свойства, способ активации и требования предмета или эффекта."),
            Spell("find-familiar", "Поиск фамильяра", 1, ["wizard"], "Призывает духа в облике зверя.", "Вы получаете фамильяра, который действует независимо и может помогать в разведке."),
            Spell("disguise-self", "Маскировка", 1, ["bard", "sorcerer", "wizard"], "Иллюзорно меняет внешний вид.", "Вы создаёте иллюзию, изменяющую вашу одежду, внешность и снаряжение на 1 час."),
            Spell("detect-evil-and-good", "Обнаружение добра и зла", 1, ["cleric", "paladin"], "Чувство сверхъестественных сущностей.", "Определяет присутствие исчадий, нежити, небожителей, фей и элементалей в пределах 30 футов."),
            Spell("command", "Приказ", 1, ["cleric", "paladin"], "Однословный магический приказ.", "Цель совершает спасбросок Мудрости; при провале выполняет приказ в следующий ход."),
            Spell("bane", "Порча", 1, ["bard", "cleric"], "Ослабляет врагов.", "До трёх существ получают штраф 1d4 к атакам и спасброскам, пока действует концентрация."),
            Spell("detect-poison-and-disease", "Обнаружение яда и болезней", 1, ["cleric", "druid", "paladin", "ranger"], "Выявляет яды, болезни и заражённые источники.", "На время действия вы чувствуете наличие и местоположение ядов и болезней в пределах 30 футов."),
            Spell("feather-fall", "Падение пёрышком", 1, ["bard", "sorcerer", "wizard"], "Смягчает падение реакцией.", "Выбранные падающие существа снижают скорость падения до 60 футов за раунд."),
            Spell("grease", "Масляное пятно", 1, ["wizard"], "Создаёт скользкую область.", "Квадрат 10 футов становится труднопроходимым; существа могут падать ничком при провале спасброска Ловкости."),
            Spell("heroism", "Героизм", 1, ["bard", "paladin"], "Временные хиты каждый ход.", "Пока действует концентрация, цель не может быть испугана и получает временные хиты в начале хода."),
            Spell("protection-from-evil-and-good", "Защита от добра и зла", 1, ["cleric", "paladin", "warlock", "wizard"], "Защищает от определённых типов существ.", "Цель получает защиту от аберраций, исчадий, небожителей, фей, нежити и элементалей."),
            Spell("hex", "Проклятие", 1, ["warlock"], "Проклинает цель и усиливает урон.", "Накладывает штраф на проверки выбранной характеристики и добавляет 1d6 некротического урона вашим атакам."),
            Spell("armor-of-agathys", "Доспех Агатиса", 1, ["warlock"], "Временные хиты и ответный холод.", "Даёт временные хиты; атакующий в ближнем бою получает урон холодом, пока защита держится."),
            Spell("witch-bolt", "Ведьмин снаряд", 1, ["sorcerer", "warlock", "wizard"], "Связывающий электрический луч.", "При попадании наносит электрический урон и позволяет поддерживать разряд действием в следующие ходы."),
            Spell("charm-person", "Очарование личности", 1, ["bard", "druid", "sorcerer", "warlock", "wizard"], "Временно делает гуманоида дружественным.", "Цель считает вас дружелюбным знакомым при провале спасброска Мудрости."),

            // 2nd level
            Spell("invisibility", "Невидимость", 2, ["bard", "sorcerer", "warlock", "wizard"], "Делает существо невидимым.", "Цель становится невидимой до 1 часа (концентрация) или до атаки/каста заклинания.", minCharacterLevel: 3),
            Spell("misty-step", "Туманный шаг", 2, ["sorcerer", "warlock", "wizard"], "Короткая телепортация бонусным действием.", "Вы телепортируетесь на 30 футов в видимую незанятую точку.", minCharacterLevel: 3),
            Spell("hold-person", "Удержание личности", 2, ["bard", "cleric", "druid", "sorcerer", "warlock", "wizard"], "Парализует гуманоида.", "Гуманоид в пределах действия парализуется при провале спасброска Мудрости (концентрация).", minCharacterLevel: 3),
            Spell("spiritual-weapon", "Духовное оружие", 2, ["cleric"], "Создаёт атакующее духовное оружие.", "Вы создаёте плавающее оружие, которое атакует как бонусное действие без концентрации.", minCharacterLevel: 3),
            Spell("lesser-restoration", "Малое восстановление", 2, ["bard", "cleric", "druid", "paladin", "ranger"], "Снимает состояния и болезни.", "Касанием снимает одно состояние: ослеплён, оглушён, парализован или отравлен, либо одну болезнь.", minCharacterLevel: 3),
            Spell("scorching-ray", "Палящий луч", 2, ["sorcerer", "wizard"], "Несколько огненных лучей по целям.", "Вы создаёте три луча огня; каждый требует отдельного броска атаки заклинанием.", minCharacterLevel: 3),
            Spell("mirror-image", "Зеркальное отражение", 2, ["sorcerer", "wizard"], "Создаёт иллюзорные копии для защиты.", "Три иллюзорные копии затрудняют попадание по вам до конца действия заклинания.", minCharacterLevel: 3),
            Spell("blur", "Размытый образ", 2, ["sorcerer", "wizard"], "Атакам по вам сложнее попасть.", "Ваш силуэт размывается; атаки по вам совершаются с помехой, пока действует концентрация.", minCharacterLevel: 3),
            Spell("darkness", "Тьма", 2, ["sorcerer", "warlock", "wizard"], "Создаёт область магической тьмы.", "Сфера радиусом 15 футов становится магически тёмной, и обычное тёмное зрение её не пробивает.", minCharacterLevel: 3),
            Spell("shatter", "Дребезги", 2, ["bard", "sorcerer", "warlock", "wizard"], "Звуковой урон по области.", "Существа в радиусе 10 футов получают урон звуком при провале спасброска Телосложения.", minCharacterLevel: 3),
            Spell("web", "Паутина", 2, ["sorcerer", "wizard"], "Опутывающая зона контроля.", "Область покрывается липкой паутиной, становясь труднопроходимой; существа могут быть опутаны.", minCharacterLevel: 3),
            Spell("suggestion", "Внушение", 2, ["bard", "sorcerer", "warlock", "wizard"], "Магически подталкивает к действию.", "Вы формулируете разумное предложение, которое цель пытается выполнить при провале спасброска Мудрости.", minCharacterLevel: 3),
            Spell("aid", "Подмога", 2, ["cleric", "paladin"], "Увеличивает максимальные хиты группы.", "До трёх существ получают +5 к максимуму и текущим хитам на 8 часов.", minCharacterLevel: 3),
            Spell("pass-without-trace", "Бесследное передвижение", 2, ["druid", "ranger"], "Скрытное перемещение группы.", "Существа в 30 футах получают +10 к проверкам Скрытности и не оставляют следов.", minCharacterLevel: 3),

            // 3rd level
            Spell("fireball", "Огненный шар", 3, ["sorcerer", "wizard"], "Мощный взрывной урон по площади.", "В точке на расстоянии до 150 футов происходит взрыв радиусом 20 футов; существа совершают спасбросок Ловкости, получая 8d6 урона огнём при провале.", minCharacterLevel: 5),
            Spell("counterspell", "Контрзаклинание", 3, ["sorcerer", "warlock", "wizard"], "Срывает чужое колдовство реакцией.", "Вы прерываете заклинание существа в момент его сотворения; автоматически для 3 круга и ниже, иначе проверка базовой характеристики заклинателя.", minCharacterLevel: 5),
            Spell("revivify", "Оживление", 3, ["cleric", "paladin"], "Возвращает недавно погибшего союзника к жизни.", "Вы касаетесь существа, умершего не более минуты назад: оно возвращается к жизни с 1 хитом (при наличии подходящих условий).", minCharacterLevel: 5),
            Spell("spirit-guardians", "Духовные стражи", 3, ["cleric"], "Опасная аура защитных духов.", "Духи кружат вокруг вас в радиусе 15 футов; враги в зоне замедляются и получают урон излучением/некротический при провале спасброска Мудрости.", minCharacterLevel: 5),
            Spell("fly", "Полет", 3, ["sorcerer", "warlock", "wizard"], "Даёт способность летать.", "Существо получает скорость полёта 60 футов на время действия концентрации.", minCharacterLevel: 5),
            Spell("haste", "Ускорение", 3, ["sorcerer", "wizard"], "Сильно усиливает союзника в бою.", "Увеличивает скорость, даёт +2 к КД, преимущество на спасброски Ловкости и дополнительное действие каждый ход.", minCharacterLevel: 5),
            Spell("lightning-bolt", "Молния", 3, ["sorcerer", "wizard"], "Линейный мощный электрический урон.", "Линия длиной 100 футов наносит 8d6 урона электричеством при провале спасброска Ловкости.", minCharacterLevel: 5),
            Spell("dispel-magic", "Рассеивание магии", 3, ["bard", "cleric", "druid", "paladin", "sorcerer", "warlock", "wizard"], "Снимает активные магические эффекты.", "Завершает заклинания на цели; для высоких кругов может потребоваться проверка базовой характеристики заклинателя.", minCharacterLevel: 5),
            Spell("hypnotic-pattern", "Гипнотический узор", 3, ["bard", "sorcerer", "warlock", "wizard"], "Сильный контроль по области.", "Существа в кубе 30 футов могут стать очарованными и недееспособными при провале спасброска Мудрости.", minCharacterLevel: 5),
            Spell("fear", "Страх", 3, ["bard", "sorcerer", "warlock", "wizard"], "Вынуждает врагов бежать.", "Существа в конусе 30 футов при провале спасброска Мудрости становятся испуганными и пытаются удалиться.", minCharacterLevel: 5),
            Spell("bestow-curse", "Наложение проклятия", 3, ["bard", "cleric", "wizard"], "Гибкое ослабляющее проклятие.", "При касании накладывает выбранный эффект проклятия на цель после провала спасброска Мудрости.", minCharacterLevel: 5),
            Spell("water-breathing", "Подводное дыхание", 3, ["druid", "ranger", "sorcerer", "wizard"], "Группа может дышать под водой.", "До десяти существ получают способность дышать под водой на 24 часа.", minCharacterLevel: 5),
            Spell("tongues", "Языки", 3, ["bard", "cleric", "sorcerer", "warlock", "wizard"], "Убирает языковой барьер.", "Цель понимает любой услышанный язык и её речь понимают другие.", minCharacterLevel: 5),

            // 4th level
            Spell("dimension-door", "Дверь между измерениями", 4, ["bard", "sorcerer", "warlock", "wizard"], "Телепортация на большую дистанцию.", "Вы телепортируетесь на расстояние до 500 футов и можете взять одно существо вашего размера или меньше.", minCharacterLevel: 7),
            Spell("greater-invisibility", "Высшая невидимость", 4, ["bard", "sorcerer", "wizard"], "Невидимость не спадает от атак.", "Цель остаётся невидимой на время концентрации, даже совершая атаки и накладывая заклинания.", minCharacterLevel: 7),
            Spell("ice-storm", "Ледяной шторм", 4, ["druid", "sorcerer", "wizard"], "Комбинированный урон в области.", "Существа в цилиндре получают урон дробящим и холодом; область становится труднопроходимой.", minCharacterLevel: 7),
            Spell("stoneskin", "Каменная кожа", 4, ["druid", "ranger", "sorcerer", "wizard"], "Сопротивление физическому урону.", "Цель получает сопротивление немагическому дробящему, колющему и рубящему урону.", minCharacterLevel: 7),
            Spell("banishment", "Изгнание", 4, ["cleric", "paladin", "sorcerer", "warlock", "wizard"], "Временно удаляет существо с поля боя.", "Цель при провале спасброска Харизмы изгоняется на другой план или в безвременное пространство.", minCharacterLevel: 7),
            Spell("polymorph", "Превращение", 4, ["bard", "druid", "sorcerer", "wizard"], "Преобразует существо в зверя.", "Цель превращается в зверя с КС, не превышающим её уровень/КС, сохраняя личность.", minCharacterLevel: 7),
            Spell("wall-of-fire", "Огненная стена", 4, ["druid", "sorcerer", "wizard"], "Создаёт наносящую урон стену.", "Формирует огненную стену, наносящую урон при появлении и при прохождении рядом/сквозь неё.", minCharacterLevel: 7),
            Spell("guardian-of-faith", "Страж веры", 4, ["cleric"], "Неподвижный защитник области.", "Призывает стража, который наносит урон врагам, входящим в зону его охраны.", minCharacterLevel: 7),

            // 5th level
            Spell("telekinesis", "Телекинез", 5, ["sorcerer", "wizard"], "Магическое перемещение объектов и существ.", "Позволяет перемещать большие объекты и пытаться удерживать существ силой разума (концентрация).", minCharacterLevel: 9),
            Spell("cone-of-cold", "Конус холода", 5, ["sorcerer", "wizard"], "Мощный холодный урон в конусе.", "Существа в конусе 60 футов получают 8d8 урона холодом при провале спасброска Телосложения.", minCharacterLevel: 9),
            Spell("wall-of-force", "Силовая стена", 5, ["wizard"], "Почти непреодолимый магический барьер.", "Создаёт невидимую силовую стену, неуязвимую к урону и не пропускающую физические объекты.", minCharacterLevel: 9),
            Spell("raise-dead", "Воскрешение", 5, ["bard", "cleric", "paladin"], "Возвращает умершего к жизни.", "Возвращает к жизни существо, умершее не более 10 дней назад, при соблюдении условий и с ослаблением после возврата.", minCharacterLevel: 9),
            Spell("greater-restoration", "Высшее восстановление", 5, ["bard", "cleric", "druid"], "Снимает тяжёлые негативные эффекты.", "Снимает одно из состояний/эффектов: очарование, окаменение, проклятие, снижение характеристик и т.п.", minCharacterLevel: 9),
            Spell("hold-monster", "Удержание чудовища", 5, ["bard", "sorcerer", "warlock", "wizard"], "Парализует любое существо.", "Аналог Удержания личности, но для любого существа при провале спасброска Мудрости.", minCharacterLevel: 9),
            Spell("scrying", "Наблюдение", 5, ["bard", "cleric", "druid", "warlock", "wizard"], "Магическая удалённая разведка.", "Вы создаёте сенсор и наблюдаете выбранное существо/место, если цель проваливает спасбросок Мудрости.", minCharacterLevel: 9),
            Spell("mass-cure-wounds", "Массовое лечение ран", 5, ["bard", "cleric", "druid"], "Лечение группы по области.", "До шести существ в области получают исцеление 3d8 + модификатор базовой характеристики заклинателя.", minCharacterLevel: 9),

            // 6th level
            Spell("disintegrate", "Распад", 6, ["sorcerer", "wizard"], "Мощный луч разрушительной энергии.", "При провале спасброска Ловкости цель получает 10d6 + 40 урона силовым полем; если хиты падают до 0, превращается в пыль.", minCharacterLevel: 11),
            Spell("chain-lightning", "Цепная молния", 6, ["sorcerer", "wizard"], "Молния поражает несколько целей.", "Основная цель и до трёх дополнительных целей получают урон электричеством при провале спасброска Ловкости.", minCharacterLevel: 11),
            Spell("heal", "Полное исцеление", 6, ["cleric", "druid"], "Мгновенно восстанавливает много хитов.", "Существо восстанавливает 70 хитов; также снимаются слепота, глухота и болезни.", minCharacterLevel: 11),
            Spell("heroes-feast", "Пир героев", 6, ["cleric", "druid"], "Мощное благословение группы перед приключением.", "Участники получают повышение максимума хитов, иммунитет к яду и страху, а также преимущество на спасброски Мудрости.", minCharacterLevel: 11),
            Spell("mass-suggestion", "Массовое внушение", 6, ["bard", "sorcerer", "warlock", "wizard"], "Внушение сразу нескольким существам.", "До двенадцати существ выполняют сформулированное предложение при провале спасброска Мудрости.", minCharacterLevel: 11),
            Spell("true-seeing", "Истинное зрение", 6, ["bard", "cleric", "sorcerer", "warlock", "wizard"], "Позволяет видеть истинную природу вещей.", "Цель видит невидимое, сквозь иллюзии и на Эфирный План в пределах 120 футов.", minCharacterLevel: 11),

            // 7th level
            Spell("plane-shift", "Сдвиг плоскости", 7, ["cleric", "druid", "sorcerer", "warlock", "wizard"], "Переносит существ между планами.", "Телепортирует группу на другой план бытия или пытается изгнать цель прикосновением при провале спасброска Харизмы.", minCharacterLevel: 13),
            Spell("resurrection", "Воскрешение истинное", 7, ["bard", "cleric"], "Возвращает давно умершего к жизни.", "Воскрешает существо, умершее до 100 лет назад, при наличии части тела и соблюдении условий.", minCharacterLevel: 13),
            Spell("teleport", "Телепортация", 7, ["bard", "sorcerer", "wizard"], "Мгновенно переносит группу на большое расстояние.", "Вы переносите себя и других существ/предметы к известной точке, с возможной ошибкой в зависимости от знания места.", minCharacterLevel: 13),
            Spell("finger-of-death", "Палец смерти", 7, ["sorcerer", "warlock", "wizard"], "Сильный некротический урон по одной цели.", "Цель получает большой некротический урон; гуманоид, убитый этим заклинанием, может стать зомби под вашим контролем.", minCharacterLevel: 13),
            Spell("fire-storm", "Огненная буря", 7, ["cleric", "druid"], "Крупномасштабный огненный урон по выбранным зонам.", "Создаёт до десяти 10-футовых кубов огня, наносящих урон существам в области.", minCharacterLevel: 13),

            // 8th level
            Spell("earthquake", "Землетрясение", 8, ["cleric", "druid", "sorcerer"], "Масштабно разрушает местность.", "В большой области возникает сейсмическая активность, сбивающая существ и разрушающая структуры.", minCharacterLevel: 15),
            Spell("sunburst", "Солнечная вспышка", 8, ["cleric", "druid", "sorcerer", "wizard"], "Взрыв ослепляющего солнечного света.", "Существа в радиусе 60 футов получают урон излучением и могут быть ослеплены при провале спасброска Телосложения.", minCharacterLevel: 15),
            Spell("power-word-stun", "Слово силы: оглушение", 8, ["bard", "sorcerer", "warlock", "wizard"], "Оглушает цель без спасброска при низких хп.", "Если у цели 150 хитов или меньше, она становится оглушённой; в последующих ходах может пытаться выйти из эффекта.", minCharacterLevel: 15),
            Spell("dominate-monster", "Подчинение чудовища", 8, ["bard", "sorcerer", "warlock", "wizard"], "Полный контроль над существом.", "Вы берёте под контроль существо при провале спасброска Мудрости, вплоть до отдачи точных приказов.", minCharacterLevel: 15),

            // 9th level
            Spell("wish", "Исполнение желаний", 9, ["sorcerer", "wizard"], "Самое могущественное заклинание в игре.", "Позволяет воспроизвести почти любое заклинание низшего круга без компонентов или попытаться изменить реальность с рисками для заклинателя.", minCharacterLevel: 17),
            Spell("meteor-swarm", "Метеоритный дождь", 9, ["sorcerer", "wizard"], "Катастрофический урон по огромной площади.", "Четыре метеора взрываются в выбранных точках, нанося колоссальный урон огнём и дробящий урон.", minCharacterLevel: 17),
            Spell("foresight", "Предвидение", 9, ["bard", "druid", "warlock", "wizard"], "Долговременное боевое предвидение.", "Цель получает преимущество на атаки, проверки и спасброски; атаки по ней — с помехой в течение 8 часов.", minCharacterLevel: 17),
            Spell("power-word-kill", "Слово силы: смерть", 9, ["bard", "sorcerer", "warlock", "wizard"], "Убивает цель при достаточном ослаблении.", "Если у цели 100 хитов или меньше, она умирает без спасброска.", minCharacterLevel: 17),
            Spell("true-resurrection", "Истинное воскрешение", 9, ["cleric", "druid"], "Возвращает к жизни почти любого умершего.", "Воскрешает существо, умершее до 200 лет назад, даже без тела (если существо не нежить и не умерло от старости).", minCharacterLevel: 17)
        };

        foreach (var item in spells)
        {
            await rulesRepository.UpsertSpellAsync(item, cancellationToken);
        }
    }

    private async Task SeedEquipmentAsync(CancellationToken cancellationToken)
    {
        var items = new[]
        {
            // Armor and shields
            Item("padded-armor", "Стёганый доспех", "Armor", "Light", 5, "gp", 8, armorClassBase: 11, equipSlot: "body"),
            Item("leather-armor", "Кожаный доспех", "Armor", "Light", 10, "gp", 10, armorClassBase: 11, equipSlot: "body"),
            Item("studded-leather-armor", "Проклёпанный кожаный доспех", "Armor", "Light", 45, "gp", 13, armorClassBase: 12, equipSlot: "body"),
            Item("hide-armor", "Шкурный доспех", "Armor", "Medium", 10, "gp", 12, armorClassBase: 12, equipSlot: "body"),
            Item("chain-shirt", "Кольчужная рубаха", "Armor", "Medium", 50, "gp", 20, armorClassBase: 13, equipSlot: "body"),
            Item("scale-mail", "Чешуйчатый доспех", "Armor", "Medium", 50, "gp", 45, armorClassBase: 14, equipSlot: "body"),
            Item("breastplate", "Кираса", "Armor", "Medium", 400, "gp", 20, armorClassBase: 14, equipSlot: "body"),
            Item("half-plate", "Полулаты", "Armor", "Medium", 750, "gp", 40, armorClassBase: 15, equipSlot: "body"),
            Item("ring-mail", "Кольчатый доспех", "Armor", "Heavy", 30, "gp", 40, armorClassBase: 14, equipSlot: "body"),
            Item("chain-mail", "Кольчуга", "Armor", "Heavy", 75, "gp", 55, armorClassBase: 16, equipSlot: "body"),
            Item("splint-armor", "Наборный доспех", "Armor", "Heavy", 200, "gp", 60, armorClassBase: 17, equipSlot: "body"),
            Item("plate-armor", "Латы", "Armor", "Heavy", 1500, "gp", 65, armorClassBase: 18, equipSlot: "body"),
            Item("shield", "Щит", "Armor", "Shield", 10, "gp", 6, isShield: true, armorClassBase: 2, equipSlot: "off-hand"),

            // Martial melee weapons
            Weapon("battleaxe", "Боевой топор", "Martial Weapon", "Melee", 10, "gp", 4, "1d8", "slashing", weaponProperties: ["versatile"]),
            Weapon("flail", "Цеп", "Martial Weapon", "Melee", 10, "gp", 2, "1d8", "bludgeoning"),
            Weapon("glaive", "Глефа", "Martial Weapon", "Melee", 20, "gp", 6, "1d10", "slashing", isTwoHanded: true, weaponProperties: ["heavy", "reach", "two-handed"]),
            Weapon("greataxe", "Секира", "Martial Weapon", "Melee", 30, "gp", 7, "1d12", "slashing", isTwoHanded: true, weaponProperties: ["heavy", "two-handed"]),
            Weapon("greatsword", "Двуручный меч", "Martial Weapon", "Melee", 50, "gp", 6, "2d6", "slashing", isTwoHanded: true, weaponProperties: ["heavy", "two-handed"]),
            Weapon("halberd", "Алебарда", "Martial Weapon", "Melee", 20, "gp", 6, "1d10", "slashing", isTwoHanded: true, weaponProperties: ["heavy", "reach", "two-handed"]),
            Weapon("lance", "Копьё всадника", "Martial Weapon", "Melee", 10, "gp", 6, "1d12", "piercing", weaponProperties: ["reach", "special"]),
            Weapon("longsword", "Длинный меч", "Martial Weapon", "Melee", 15, "gp", 3, "1d8", "slashing", weaponProperties: ["versatile"]),
            Weapon("maul", "Молот", "Martial Weapon", "Melee", 10, "gp", 10, "2d6", "bludgeoning", isTwoHanded: true, weaponProperties: ["heavy", "two-handed"]),
            Weapon("morningstar", "Моргенштерн", "Martial Weapon", "Melee", 15, "gp", 4, "1d8", "piercing"),
            Weapon("pike", "Пика", "Martial Weapon", "Melee", 5, "gp", 18, "1d10", "piercing", isTwoHanded: true, weaponProperties: ["heavy", "reach", "two-handed"]),
            Weapon("rapier", "Рапира", "Martial Weapon", "Melee", 25, "gp", 2, "1d8", "piercing", weaponProperties: ["finesse"]),
            Weapon("scimitar", "Скимитар", "Martial Weapon", "Melee", 25, "gp", 3, "1d6", "slashing", weaponProperties: ["finesse", "light"]),
            Weapon("shortsword", "Короткий меч", "Martial Weapon", "Melee", 10, "gp", 2, "1d6", "piercing", weaponProperties: ["finesse", "light"]),
            Weapon("trident", "Трезубец", "Martial Weapon", "Melee", 5, "gp", 4, "1d6", "piercing", weaponProperties: ["thrown", "versatile"]),
            Weapon("war-pick", "Боевое кайло", "Martial Weapon", "Melee", 5, "gp", 2, "1d8", "piercing"),
            Weapon("warhammer", "Боевой молот", "Martial Weapon", "Melee", 15, "gp", 2, "1d8", "bludgeoning", weaponProperties: ["versatile"]),
            Weapon("whip", "Кнут", "Martial Weapon", "Melee", 2, "gp", 3, "1d4", "slashing", weaponProperties: ["finesse", "reach"]),

            // Martial ranged weapons
            Weapon("blowgun", "Духовая трубка", "Martial Weapon", "Ranged", 10, "gp", 1, "1", "piercing", weaponProperties: ["ammunition", "loading"]),
            Weapon("hand-crossbow", "Ручной арбалет", "Martial Weapon", "Ranged", 75, "gp", 3, "1d6", "piercing", weaponProperties: ["ammunition", "light", "loading"]),
            Weapon("heavy-crossbow", "Тяжёлый арбалет", "Martial Weapon", "Ranged", 50, "gp", 18, "1d10", "piercing", isTwoHanded: true, weaponProperties: ["ammunition", "heavy", "loading", "two-handed"]),
            Weapon("longbow", "Длинный лук", "Martial Weapon", "Ranged", 50, "gp", 2, "1d8", "piercing", isTwoHanded: true, weaponProperties: ["ammunition", "heavy", "two-handed"]),
            Weapon("net", "Сеть", "Martial Weapon", "Ranged", 1, "gp", 3, "0", "special", weaponProperties: ["thrown", "special"]),

            // Simple weapons
            Weapon("club", "Дубинка", "Simple Weapon", "Melee", 1, "sp", 2, "1d4", "bludgeoning", weaponProperties: ["light"]),
            Weapon("dagger", "Кинжал", "Simple Weapon", "Melee", 2, "gp", 1, "1d4", "piercing", weaponProperties: ["finesse", "light", "thrown"]),
            Weapon("greatclub", "Палица", "Simple Weapon", "Melee", 2, "sp", 10, "1d8", "bludgeoning", isTwoHanded: true, weaponProperties: ["two-handed"]),
            Weapon("handaxe", "Ручной топор", "Simple Weapon", "Melee", 5, "gp", 2, "1d6", "slashing", weaponProperties: ["light", "thrown"]),
            Weapon("javelin", "Метательное копьё", "Simple Weapon", "Melee", 5, "sp", 2, "1d6", "piercing", weaponProperties: ["thrown"]),
            Weapon("light-hammer", "Лёгкий молот", "Simple Weapon", "Melee", 2, "gp", 2, "1d4", "bludgeoning", weaponProperties: ["light", "thrown"]),
            Weapon("mace", "Булава", "Simple Weapon", "Melee", 5, "gp", 4, "1d6", "bludgeoning"),
            Weapon("quarterstaff", "Посох", "Simple Weapon", "Melee", 2, "sp", 4, "1d6", "bludgeoning", weaponProperties: ["versatile"]),
            Weapon("sickle", "Серп", "Simple Weapon", "Melee", 1, "gp", 2, "1d4", "slashing", weaponProperties: ["light"]),
            Weapon("spear", "Копьё", "Simple Weapon", "Melee", 1, "gp", 3, "1d6", "piercing", weaponProperties: ["thrown", "versatile"]),
            Weapon("light-crossbow", "Лёгкий арбалет", "Simple Weapon", "Ranged", 25, "gp", 5, "1d8", "piercing", isTwoHanded: true, weaponProperties: ["ammunition", "loading", "two-handed"]),
            Weapon("dart", "Дротик", "Simple Weapon", "Ranged", 5, "cp", 0.25m, "1d4", "piercing", weaponProperties: ["finesse", "thrown"]),
            Weapon("shortbow", "Короткий лук", "Simple Weapon", "Ranged", 25, "gp", 2, "1d6", "piercing", isTwoHanded: true, weaponProperties: ["ammunition", "two-handed"]),
            Weapon("sling", "Праща", "Simple Weapon", "Ranged", 1, "sp", 0, "1d4", "bludgeoning", weaponProperties: ["ammunition"]),

            // Packs and gear
            Item("backpack", "Рюкзак", "Adventuring Gear", "Container", 2, "gp", 5),
            Item("bedroll", "Спальный мешок", "Adventuring Gear", "Travel", 1, "gp", 7),
            Item("rope-hemp", "Верёвка (пеньковая, 50 фт.)", "Adventuring Gear", "Tools", 1, "gp", 10),
            Item("rope-silk", "Верёвка (шёлковая, 50 фт.)", "Adventuring Gear", "Tools", 10, "gp", 5),
            Item("rations", "Сухой паёк (1 день)", "Adventuring Gear", "Food", 5, "sp", 2),
            Item("waterskin", "Бурдюк", "Adventuring Gear", "Container", 2, "sp", 5),
            Item("torch", "Факел", "Adventuring Gear", "Light", 1, "cp", 1),
            Item("lantern-bullseye", "Фонарь с заслонкой", "Adventuring Gear", "Light", 10, "gp", 2),
            Item("lantern-hooded", "Капюшонный фонарь", "Adventuring Gear", "Light", 5, "gp", 2),
            Item("healers-kit", "Набор лекаря", "Adventuring Gear", "Medical", 5, "gp", 3),
            Item("thieves-tools", "Воровские инструменты", "Tools", "Artisan/Utility", 25, "gp", 1),
            Item("component-pouch", "Компонентная сумка", "Arcane Focus", "Spellcasting", 25, "gp", 2),
            Item("holy-symbol-amulet", "Священный символ (амулет)", "Holy Symbol", "Spellcasting", 5, "gp", 1),
            Item("druidic-focus-totem", "Друидический фокус (тотем)", "Druidic Focus", "Spellcasting", 1, "gp", 0),
            Item("lute", "Лютня", "Musical Instrument", "Instrument", 35, "gp", 2),
            Item("flute", "Флейта", "Musical Instrument", "Instrument", 2, "gp", 1),
            Item("drum", "Барабан", "Musical Instrument", "Instrument", 6, "gp", 3)
        };

        foreach (var item in items)
        {
            await rulesRepository.UpsertEquipmentAsync(item, cancellationToken);
        }
    }

    private async Task SeedMonstersAsync(CancellationToken cancellationToken)
    {
        var monsters = new[]
        {
            Creature("bat", "Летучая мышь", "Tiny", "beast", "unaligned", 0m, 12, 1, "1d4-1", 5, "Укус", 0, "1", 0, "piercing"),
            Creature("baboon", "Павиан", "Small", "beast", "unaligned", 0m, 12, 3, "1d6", 30, "Укус", 1, "1d4", 0, "piercing"),
            Creature("badger", "Барсук", "Tiny", "beast", "unaligned", 0m, 10, 3, "1d4+1", 20, "Укус", 2, "1", 0, "piercing"),
            Creature("boar", "Кабан", "Medium", "beast", "unaligned", 0.25m, 11, 11, "2d8+2", 40, "Клык", 3, "1d6", 1, "slashing"),
            Creature("cat", "Кошка", "Tiny", "beast", "unaligned", 0m, 12, 2, "1d4", 40, "Когти", 0, "1", 0, "slashing"),
            Creature("crab", "Краб", "Tiny", "beast", "unaligned", 0m, 11, 2, "1d4", 20, "Клешня", 0, "1", 0, "bludgeoning"),
            Creature("deer", "Олень", "Medium", "beast", "unaligned", 0m, 13, 4, "1d8", 50, "Удар рогами", 2, "1d4", 0, "piercing"),
            Creature("frog", "Лягушка", "Tiny", "beast", "unaligned", 0m, 11, 1, "1d4-1", 20, "Укус", 0, "1", 0, "piercing"),
            Creature("hawk", "Ястреб", "Tiny", "beast", "unaligned", 0m, 13, 1, "1d4-1", 10, "Когти", 5, "1", 0, "slashing"),
            Creature("lizard", "Ящерица", "Tiny", "beast", "unaligned", 0m, 10, 2, "1d4", 20, "Укус", 0, "1", 0, "piercing"),
            Creature("octopus", "Осьминог", "Small", "beast", "unaligned", 0m, 12, 3, "1d6", 5, "Щупальца", 5, "1", 0, "bludgeoning"),
            Creature("owl", "Сова", "Tiny", "beast", "unaligned", 0m, 11, 1, "1d4-1", 5, "Когти", 3, "1", 0, "slashing"),
            Creature("poisonous-snake", "Ядовитая змея", "Tiny", "beast", "unaligned", 0.125m, 13, 2, "1d4", 30, "Укус", 5, "1", 0, "piercing"),
            Creature("quipper", "Пиранья", "Tiny", "beast", "unaligned", 0m, 13, 1, "1d4-1", 0, "Укус", 5, "1", 0, "piercing"),
            Creature("rat", "Крыса", "Tiny", "beast", "unaligned", 0m, 10, 1, "1d4-1", 20, "Укус", 0, "1", 0, "piercing"),
            Creature("raven", "Ворон", "Tiny", "beast", "unaligned", 0m, 12, 1, "1d4-1", 10, "Клюв", 4, "1", 0, "piercing"),
            Creature("sea-horse", "Морской конёк", "Tiny", "beast", "unaligned", 0m, 11, 1, "1d4-1", 0, "Удар", 0, "1", 0, "bludgeoning"),
            Creature("spider", "Паук", "Tiny", "beast", "unaligned", 0m, 12, 1, "1d4-1", 20, "Укус", 4, "1", 0, "piercing"),
            Creature("weasel", "Ласка", "Tiny", "beast", "unaligned", 0m, 13, 1, "1d4-1", 30, "Укус", 5, "1", 0, "piercing"),
            Creature("mastiff", "Мастиф", "Medium", "beast", "unaligned", 0.125m, 12, 5, "1d8+1", 40, "Укус", 3, "1d6", 1, "piercing"),
            Creature("ape", "Обезьяна", "Medium", "beast", "unaligned", 0.5m, 12, 19, "3d8+6", 30, "Кулак", 5, "1d6", 3, "bludgeoning"),
            Creature("black-bear", "Чёрный медведь", "Medium", "beast", "unaligned", 0.5m, 11, 19, "3d8+6", 40, "Укус", 3, "1d6", 2, "piercing"),
            Creature("crocodile", "Крокодил", "Large", "beast", "unaligned", 0.5m, 12, 19, "3d10+3", 20, "Укус", 4, "1d10", 2, "piercing"),
            Creature("giant-badger", "Гигантский барсук", "Medium", "beast", "unaligned", 0.25m, 10, 13, "2d8+4", 30, "Укус", 3, "1d6", 1, "piercing"),
            Creature("giant-bat", "Гигантская летучая мышь", "Large", "beast", "unaligned", 0.25m, 13, 22, "4d10", 10, "Укус", 4, "1d6", 2, "piercing"),
            Creature("giant-centipede", "Гигантская многоножка", "Small", "beast", "unaligned", 0.25m, 13, 4, "1d6+1", 30, "Укус", 4, "1d4", 2, "piercing"),
            Creature("giant-crab", "Гигантский краб", "Medium", "beast", "unaligned", 0.125m, 15, 13, "3d8", 30, "Клешня", 3, "1d6", 1, "bludgeoning"),
            Creature("giant-frog", "Гигантская лягушка", "Medium", "beast", "unaligned", 0.25m, 11, 18, "4d8", 30, "Укус", 3, "1d6", 1, "piercing"),
            Creature("giant-goat", "Гигантский козёл", "Large", "beast", "unaligned", 0.5m, 11, 19, "3d10+3", 40, "Таран", 3, "2d4", 1, "bludgeoning"),
            Creature("giant-lizard", "Гигантская ящерица", "Large", "beast", "unaligned", 0.25m, 12, 19, "3d10+3", 30, "Укус", 4, "1d8", 2, "piercing"),
            Creature("giant-poisonous-snake", "Гигантская ядовитая змея", "Medium", "beast", "unaligned", 0.25m, 14, 11, "2d8+2", 30, "Укус", 6, "1d4", 4, "piercing"),
            Creature("giant-rat", "Гигантская крыса", "Small", "beast", "unaligned", 0.125m, 12, 7, "2d6", 30, "Укус", 4, "1d4", 2, "piercing"),
            Creature("giant-spider", "Гигантский паук", "Large", "beast", "unaligned", 1m, 14, 26, "4d10+4", 30, "Укус", 5, "1d8", 3, "piercing"),
            Creature("giant-toad", "Гигантская жаба", "Large", "beast", "unaligned", 1m, 11, 39, "6d10+6", 20, "Укус", 4, "1d10", 2, "piercing"),
            Creature("giant-vulture", "Гигантский гриф", "Large", "beast", "neutral evil", 1m, 10, 22, "3d10+6", 10, "Клюв", 4, "2d4", 2, "piercing"),
            Creature("giant-weasel", "Гигантская ласка", "Medium", "beast", "unaligned", 0.125m, 13, 9, "2d8", 40, "Укус", 5, "1d4", 3, "piercing"),
            Creature("giant-wolf-spider", "Гигантский волчий паук", "Medium", "beast", "unaligned", 0.25m, 13, 11, "2d8+2", 40, "Укус", 3, "1d6", 1, "piercing"),
            Creature("lion", "Лев", "Large", "beast", "unaligned", 1m, 12, 26, "4d10+4", 50, "Укус", 5, "1d8", 3, "piercing"),
            Creature("pony", "Пони", "Medium", "beast", "unaligned", 0.125m, 10, 11, "2d8+2", 40, "Копыта", 4, "2d4", 2, "bludgeoning"),
            Creature("riding-horse", "Верховая лошадь", "Large", "beast", "unaligned", 0.25m, 10, 13, "2d10+2", 60, "Копыта", 4, "2d4", 2, "bludgeoning"),
            Creature("tiger", "Тигр", "Large", "beast", "unaligned", 1m, 12, 37, "5d10+10", 40, "Укус", 5, "1d10", 3, "piercing"),
            Creature("wolf", "Волк", "Medium", "beast", "unaligned", 0.25m, 13, 11, "2d8+2", 40, "Укус", 4, "2d4", 2, "piercing"),
            Creature("dire-wolf", "Ужасный волк", "Large", "beast", "unaligned", 1m, 14, 37, "5d10+10", 50, "Укус", 5, "2d6", 3, "piercing"),
            Creature("panther", "Пантера", "Medium", "beast", "unaligned", 0.25m, 12, 13, "3d8", 50, "Укус", 4, "1d6", 2, "piercing"),
            Creature("brown-bear", "Бурый медведь", "Large", "beast", "unaligned", 1m, 11, 34, "4d10+12", 40, "Укус", 5, "1d8", 4, "piercing"),
            Creature("giant-eagle", "Гигантский орёл", "Large", "beast", "neutral good", 1m, 13, 26, "4d10+4", 10, "Когти", 5, "2d6", 3, "slashing"),
            Creature("giant-owl", "Гигантская сова", "Large", "beast", "neutral", 0.25m, 12, 19, "3d10+3", 5, "Когти", 3, "2d6", 1, "slashing"),
            Creature("kobold", "Кобольд", "Small", "humanoid", "lawful evil", 0.125m, 12, 5, "2d6-2", 30, "Кинжал", 4, "1d4", 2, "piercing"),
            Creature("goblin", "Гоблин", "Small", "humanoid", "neutral evil", 0.25m, 15, 7, "2d6", 30, "Скимитар", 4, "1d6", 2, "slashing"),
            Creature("grimlock", "Гримлок", "Medium", "humanoid", "neutral evil", 0.25m, 11, 11, "2d8+2", 30, "Костяное копьё", 5, "1d6", 3, "piercing"),
            Creature("orc", "Орк", "Medium", "humanoid", "chaotic evil", 0.5m, 13, 15, "2d8+6", 30, "Секира", 5, "1d12", 3, "slashing"),
            Creature("hobgoblin", "Хобгоблин", "Medium", "humanoid", "lawful evil", 0.5m, 18, 11, "2d8+2", 30, "Длинный меч", 3, "1d8", 1, "slashing"),
            Creature("gnoll", "Гнолл", "Medium", "humanoid", "chaotic evil", 0.5m, 15, 22, "5d8", 30, "Копьё", 4, "1d6", 2, "piercing"),
            Creature("bugbear", "Багбир", "Medium", "humanoid", "chaotic evil", 1m, 16, 27, "5d8+5", 30, "Утренняя звезда", 4, "2d8", 2, "piercing"),
            Creature("skeleton", "Скелет", "Medium", "undead", "lawful evil", 0.25m, 13, 13, "2d8+4", 30, "Короткий меч", 4, "1d6", 2, "piercing"),
            Creature("zombie", "Зомби", "Medium", "undead", "neutral evil", 0.25m, 8, 22, "3d8+9", 20, "Удар", 3, "1d6", 1, "bludgeoning"),
            Creature("ghoul", "Гуль", "Medium", "undead", "chaotic evil", 1m, 12, 22, "5d8", 30, "Когти", 4, "2d4", 2, "slashing"),
            Creature("wight", "Умертвие", "Medium", "undead", "neutral evil", 3m, 14, 45, "6d8+18", 30, "Длинный меч", 4, "1d8", 2, "slashing"),
            Creature("ogre", "Огр", "Large", "giant", "chaotic evil", 2m, 11, 59, "7d10+21", 40, "Палица", 6, "2d8", 4, "bludgeoning"),
            Creature("owlbear", "Сова-медведь", "Large", "monstrosity", "unaligned", 3m, 13, 59, "7d10+21", 40, "Клюв", 7, "1d10", 5, "piercing"),
            Creature("griffon", "Грифон", "Large", "monstrosity", "unaligned", 2m, 12, 59, "7d10+21", 30, "Клюв", 6, "1d8", 4, "piercing"),
            Creature("hippogriff", "Гиппогриф", "Large", "monstrosity", "unaligned", 1m, 11, 19, "3d10+3", 40, "Клюв", 5, "1d10", 3, "piercing"),
            Creature("harpy", "Гарпия", "Medium", "monstrosity", "chaotic evil", 1m, 11, 38, "7d8+7", 20, "Когти", 3, "2d4", 1, "slashing"),
            Creature("gargoyle", "Горгулья", "Medium", "elemental", "chaotic evil", 2m, 15, 52, "7d8+21", 30, "Когти", 4, "1d6", 2, "slashing"),
            Creature("gelatinous-cube", "Студенистый куб", "Large", "ooze", "unaligned", 2m, 6, 84, "8d10+40", 15, "Ложноножка", 3, "3d6", 0, "acid"),
            Creature("mimic", "Мимик", "Medium", "monstrosity", "neutral", 2m, 12, 58, "9d8+18", 15, "Укус", 5, "1d8", 3, "piercing"),
            Creature("minotaur", "Минотавр", "Large", "monstrosity", "chaotic evil", 3m, 14, 76, "9d10+27", 40, "Секира", 6, "2d12", 4, "slashing"),
            Creature("manticore", "Мантикора", "Large", "monstrosity", "lawful evil", 3m, 14, 68, "8d10+24", 30, "Укус", 5, "1d8", 3, "piercing"),
            Creature("ettin", "Эттин", "Large", "giant", "chaotic evil", 4m, 12, 85, "10d10+30", 40, "Боевой топор", 7, "2d8", 5, "slashing"),
            Creature("troll", "Тролль", "Large", "giant", "chaotic evil", 5m, 15, 84, "8d10+40", 30, "Когти", 7, "2d6", 4, "slashing"),
            Creature("young-red-dragon", "Молодой красный дракон", "Large", "dragon", "chaotic evil", 10m, 18, 178, "17d10+85", 40, "Укус", 10, "2d10", 6, "piercing")
        };

        foreach (var item in monsters)
        {
            await rulesRepository.UpsertMonsterAsync(item, cancellationToken);
        }
    }

    private static IReadOnlyList<FeatureDetailDto> BuildClassDetailsFromPhb(ClassOptionDto classOption)
    {
        return classOption.Details
            .Select(detail => new FeatureDetailDto(
                detail.Title,
                GetClassFeatureDescription(classOption.Id, detail.Title, detail.Description)))
            .ToList();
    }

    private static string GetClassFeatureDescription(string classId, string title, string fallback)
    {
        return (classId, title) switch
        {
            ("barbarian", "1 уровень: Ярость") => "В ярости варвар получает преимущество на проверки и спасброски Силы, бонус к урону рукопашных атак Силой и сопротивление дробящему, колющему и рубящему урону. Ярость заканчивается досрочно, если вы долго не атакуете и не получаете урон.",
            ("barbarian", "1 уровень: Защита без доспехов") => "Пока варвар не носит доспех, его КД равно 10 + модификатор Ловкости + модификатор Телосложения. Щит использовать можно.",
            ("barbarian", "3 уровень: Путь дикости") => "На 3 уровне вы выбираете путь варвара. В базовой книге игрока это Путь берсерка (максимальная агрессия и неистовство) и Путь тотемного воина (духовная связь с тотемом зверя, дающая устойчивые боевые бонусы).",
            ("bard", "1 уровень: Бардовское вдохновение") => "Бард вдохновляет союзника бонусным действием: цель получает кость вдохновения и может добавить её к проверке, атаке или спасброску согласно правилам умения.",
            ("bard", "2 уровень: Мастер на все руки") => "Бард добавляет половину бонуса мастерства ко всем проверкам характеристик, где у него нет владения. Это делает барда универсальным в небоевых задачах.",
            ("bard", "3 уровень: Коллегия бардов") => "Вы выбираете коллегию. В книге игрока представлены Коллегия Знаний (контроль, поддержка и расширенный доступ к навыкам/магии) и Коллегия Доблести (боевой бард с доспехами, оружием и усилением фронта).",
            ("cleric", "1 уровень: Божественный домен") => "Домен определяет роль жреца и даёт доменные заклинания/умения. В базовой книге: Знание, Жизнь, Свет, Природа, Буря, Обман, Война.",
            ("cleric", "2 уровень: Божественный канал") => "Жрец направляет силу божества для мощного эффекта домена. Базовый вариант — Изгнание нежити; дополнительные варианты зависят от выбранного домена.",
            ("cleric", "10 уровень: Божественное вмешательство") => "Жрец просит божество о прямой помощи. При успехе эффект определяется Мастером и отражает сферу влияния божества.",
            ("druid", "2 уровень: Круг друидов") => "Вы выбираете круг друида. В книге игрока: Круг Земли (магическая универсальность и устойчивость к истощению ресурсов) и Круг Луны (сильный боевой Дикий облик).",
            ("druid", "2 уровень: Дикий облик") => "Друид принимает звериную форму, используя показатели выбранного зверя и сохраняя ограничения по КС и доступным формам. Это инструмент выживания, разведки и ближнего боя.",
            ("druid", "18 уровень: Заклинания зверя") => "На высоком уровне друид может накладывать многие заклинания, находясь в Диком облике, что радикально расширяет тактические возможности.",
            ("fighter", "2 уровень: Всплеск действий") => "Один раз между отдыхами воин получает дополнительное действие в свой ход. Это позволяет резко наращивать урон или совмещать атаку с тактическим действием.",
            ("fighter", "3 уровень: Воинский архетип") => "На 3 уровне выбирается архетип: Чемпион (простая, надёжная боевая эффективность), Мастер боевых искусств (манёвры и превосходство тактики) или Мистический рыцарь (сочетание оружия и арканной магии).",
            ("fighter", "9 уровень: Неукротимость") => "Воин может перебросить проваленный спасбросок, но обязан принять новый результат. Количество применений растёт на высоких уровнях.",
            ("monk", "2 уровень: Ки") => "Монах получает очки Ки и тратит их на ключевые техники: Шквал ударов, Терпеливая защита и Поступь ветра. Ки восстанавливается после короткого отдыха.",
            ("monk", "3 уровень: Монашеская традиция") => "Вы выбираете традицию: Путь открытой ладони (контроль и давление в ближнем бою), Путь тени (мобильность, скрытность и темповый контроль), Путь четырёх стихий (эффекты стихий через Ки).",
            ("monk", "5 уровень: Ошеломляющий удар") => "После попадания оружием монах может потратить Ки, чтобы попытаться ошеломить цель через спасбросок Телосложения. Это одна из сильнейших контролирующих техник класса.",
            ("paladin", "3 уровень: Священная клятва") => "На 3 уровне паладин приносит клятву. В книге игрока: Клятва преданности (классический святой защитник), Клятва древних (природная и светлая стойкость), Клятва мести (охота на приоритетную цель).",
            ("paladin", "2 уровень: Божественная кара") => "После попадания рукопашной атакой паладин может потратить ячейку заклинания, чтобы нанести дополнительный урон излучением. Чем выше ячейка, тем сильнее всплеск.",
            ("paladin", "6 уровень: Аура защиты") => "Паладин и союзники рядом добавляют модификатор Харизмы паладина к своим спасброскам. Это одна из самых сильных групповых защит в игре.",
            ("ranger", "1 уровень: Избранный враг") => "Следопыт выбирает типы врагов, которых лучше знает: получает бонусы к отслеживанию, знаниям и взаимодействию с их языками в рамках правил.",
            ("ranger", "1 уровень: Исследователь природы") => "Выбранная местность даёт следопыту преимущества при путешествиях: ускоренное передвижение, лучшая разведка и надёжная навигация группы.",
            ("ranger", "3 уровень: Архетип следопыта") => "На 3 уровне выбирается архетип. В книге игрока: Охотник (настройка под стиль охоты и урон) и Повелитель зверей (боевой союз с животным-компаньоном).",
            ("rogue", "1 уровень: Скрытая атака") => "Плут наносит дополнительный урон один раз в ход, если атакует с преимуществом или рядом с целью есть союзник, а оружие подходит по условиям.",
            ("rogue", "2 уровень: Хитрое действие") => "Плут выполняет Рывок, Отход или Засаду бонусным действием. Это ключ к мобильности и безопасному позиционированию каждый ход.",
            ("rogue", "3 уровень: Архетип плута") => "Вы выбираете архетип: Вор (мобильность и работа с объектами), Убийца (высокий стартовый урон и засады), Мистический ловкач (арканная поддержка в стиле плута).",
            ("sorcerer", "1 уровень: Чародейское происхождение") => "Происхождение определяет источник врождённой магии. В книге игрока: Драконья кровь (живучесть и элементальная тематика) и Дикая магия (непредсказуемые всплески силы).",
            ("sorcerer", "2 уровень: Источник магии") => "Чародей получает очки чародейства и может обменивать их на ячейки заклинаний и обратно. Это фундамент гибкого управления ресурсами.",
            ("sorcerer", "3 уровень: Метамагия") => "Метамагия позволяет менять свойства заклинаний: ускорять их, усиливать спасброски, убирать вербальные/соматические компоненты и т.д.",
            ("warlock", "1 уровень: Потусторонний покровитель") => "Покровитель определяет стиль колдуна. В книге игрока: Архифея (контроль и хитрость), Исчадие (агрессия и выживание), Великий Древний (ментальное давление и нестандартные эффекты).",
            ("warlock", "1 уровень: Договорная магия") => "Колдун использует немного ячеек, но они всегда максимального круга и восстанавливаются после короткого отдыха. Это задаёт особый ритм игры классом.",
            ("warlock", "2 уровень: Потусторонние воззвания") => "Воззвания — постоянные мистические улучшения: от усиления Мистического разряда до дополнительных заклинаний и новых способов восприятия мира.",
            ("wizard", "2 уровень: Магическая традиция") => "На 2 уровне выбирается школа магии: Ограждение, Вызов, Прорицание, Очарование, Воплощение, Иллюзия, Некромантия или Преобразование. Традиция задаёт специализацию волшебника на все уровни.",
            ("wizard", "1 уровень: Книга заклинаний") => "Волшебник ведёт книгу заклинаний, копирует в неё новые формулы и готовит список заклинаний на день. Гибкость книги — главный ресурс класса.",
            ("wizard", "18 уровень: Мастер заклинаний") => "Два выбранных заклинания низких кругов становятся практически «бесконечными» в применении, что сильно повышает стабильность в приключениях.",
            _ => fallback
        };
    }

    private static string BuildBackgroundSummary(BackgroundOptionDto background)
    {
        return background.Summary;
    }

    private static string BuildBackgroundLore(BackgroundOptionDto background)
    {
        return background.Id switch
        {
            "acolyte" => "Послушник вырос в религиозной традиции, изучал обряды, догматы и иерархию храма. Он ориентируется в жизни общин веры и знает, как получить поддержку единоверцев.",
            "charlatan" => "Шарлатан живёт масками, легендами и социальными манёврами. Для него ложь — ремесло, а поддельная личность и документы такой же инструмент, как клинок для воина.",
            "criminal" => "Преступник знаком с теневой экономикой, кодексом улиц и способами скрываться от закона. Контакты в подполье помогают добывать информацию и решать вопросы неофициально.",
            "entertainer" => "Артист умеет завоёвывать внимание публики: на ярмарке, в трактире и при дворе. Сцена для него не только работа, но и способ добывать связи и влияние.",
            "folk-hero" => "Народный герой вышел из простого сословия и заслужил уважение сообщества поступком, который помнят люди. Ему доверяют там, где не доверяют благородным и чиновникам.",
            "guild-artisan" => "Ремесленник гильдии встроен в систему цеховых правил, заказов и профессиональной репутации. Его имя и качество труда ценятся внутри ремесленного сообщества.",
            "hermit" => "Отшельник провёл годы в уединении, исследуя философскую, магическую или религиозную истину. Его открытие может стать ключом к сюжету кампании.",
            "noble" => "Дворянин воспитан в культуре титулов, протокола и долговых обязательств. Он знает, как работает власть, и умеет использовать социальный статус как ресурс.",
            "outlander" => "Чужеземец выживает в дикой местности там, где горожанин потеряется за день. Его опыт охоты, навигации и переходов делает группу устойчивой в экспедициях.",
            "sage" => "Мудрец посвятил жизнь чтению, исследованиям и сбору знаний. Он может не знать ответ сразу, но обычно знает, где его искать и как проверить достоверность.",
            "sailor" => "Моряк закалён штормами, дисциплиной команды и долгими переходами. Он понимает портовый быт, морские традиции и не теряется в экстремальных условиях.",
            "soldier" => "Солдат прошёл строевую и боевую школу: приказы, субординация, лагерная дисциплина и выживание на войне. Опыт службы формирует его реакцию в кризисе.",
            "urchin" => "Беспризорник вырос на улицах и знает город по тайным тропам, крышам и дворам. Он привык выживать хитростью, внимательностью и скоростью решений.",
            _ => background.Summary
        };
    }

    private static IReadOnlyList<FeatureDetailDto> BuildBackgroundDetailsFromPhb(BackgroundOptionDto background)
    {
        return background.Id switch
        {
            "acolyte" =>
            [
                new FeatureDetailDto("Навыки", "Владение навыками Проницательность и Религия отражает богословскую подготовку и работу с людьми в рамках религиозной общины."),
                new FeatureDetailDto("Умение: Приют веры", "Вы и ваши спутники можете рассчитывать на бесплатное лечение и кров в храмах вашей веры при разумном поведении и уважении к уставу.")
            ],
            "charlatan" =>
            [
                new FeatureDetailDto("Навыки", "Обман и Ловкость рук позволяют поддерживать легенду, управлять вниманием собеседника и незаметно подменять предметы."),
                new FeatureDetailDto("Умение: Поддельная личность", "У вас есть хорошо проработанная альтернативная персона с документами, внешними атрибутами и сетью контактов.")
            ],
            "entertainer" =>
            [
                new FeatureDetailDto("Навыки", "Акробатика и Выступление отражают сценическую подготовку, чувство ритма и уверенную работу с публикой."),
                new FeatureDetailDto("Умение: Народный любимец", "Вы можете найти площадку для выступления и получить еду/ночлег в обмен на представление.")
            ],
            "criminal" =>
            [
                new FeatureDetailDto("Навыки", "Обман и Скрытность делают вас эффективным в слежке, проникновении и сокрытии намерений."),
                new FeatureDetailDto("Умение: Криминальные связи", "У вас есть надёжный контакт в преступном мире для передачи сообщений и поиска теневых услуг.")
            ],
            "folk-hero" =>
            [
                new FeatureDetailDto("Навыки", "Уход за животными и Выживание отражают практический опыт сельской жизни и работы в тяжёлых условиях."),
                new FeatureDetailDto("Умение: Деревенское гостеприимство", "Простые люди охотно укроют и накормят вас, если вы не представляете угрозы их сообществу.")
            ],
            "guild-artisan" =>
            [
                new FeatureDetailDto("Навыки", "Проницательность и Убеждение помогают вести переговоры о заказах, цене и репутации внутри ремесленного сообщества."),
                new FeatureDetailDto("Умение: Членство в гильдии", "Гильдия предоставляет информационную и организационную поддержку, а также признаёт ваш профессиональный статус.")
            ],
            "hermit" =>
            [
                new FeatureDetailDto("Навыки", "Медицина и Религия отражают годы созерцательной практики, ухода за собой и духовного поиска."),
                new FeatureDetailDto("Умение: Открытие", "Во время уединения вы сделали важное открытие о мире; Мастер и игрок совместно определяют его сюжетную ценность.")
            ],
            "noble" =>
            [
                new FeatureDetailDto("Навыки", "История и Убеждение поддерживают вашу роль в политике, дипломатии и общении с людьми высокого статуса."),
                new FeatureDetailDto("Умение: Привилегированное положение", "Ваше происхождение обеспечивает уважительное отношение в высшем обществе и доступ к соответствующим кругам.")
            ],
            "outlander" =>
            [
                new FeatureDetailDto("Навыки", "Атлетика и Выживание отражают опыт кочевой жизни, дальних переходов и добычи ресурсов вдали от цивилизации."),
                new FeatureDetailDto("Умение: Скиталец", "Вы прекрасно запоминаете карты и местность, находите пищу и воду для себя и спутников в дикой природе.")
            ],
            "sage" =>
            [
                new FeatureDetailDto("Навыки", "Магия и История дают базу для исследования артефактов, древних текстов и природы сверхъестественных явлений."),
                new FeatureDetailDto("Умение: Исследователь", "Если вы не знаете ответа, вы обычно знаете, где искать источник: библиотеку, архив, наставника или каталог.")
            ],
            "sailor" =>
            [
                new FeatureDetailDto("Навыки", "Атлетика и Внимательность отражают труд на корабле, наблюдение за обстановкой и действия в опасных морских условиях."),
                new FeatureDetailDto("Умение: Переход на корабле", "Вы можете получить бесплатный проход для себя и спутников на кораблях, где экипаж относится к вам как к своему.")
            ],
            "soldier" =>
            [
                new FeatureDetailDto("Навыки", "Атлетика и Запугивание помогают действовать в строю, удерживать позицию и подавлять сопротивление дисциплиной и авторитетом."),
                new FeatureDetailDto("Умение: Воинское звание", "Военные структуры признают ваше звание и могут оказать ограниченную организационную поддержку по уставу.")
            ],
            "urchin" =>
            [
                new FeatureDetailDto("Навыки", "Ловкость рук и Скрытность отражают практику выживания на улицах: быстрые решения, незаметность и точные движения."),
                new FeatureDetailDto("Умение: Городские тайны", "Вы знаете скрытые проходы и короткие маршруты в городе, позволяющие быстро перемещаться между районами.")
            ],
            _ => background.Details
        };
    }

    private static List<ChoiceEntry> BuildRaceChoices(RaceOptionDto race)
    {
        var choices = new List<ChoiceEntry>();
        if (race.BonusChoiceRule is not null)
        {
            choices.Add(new ChoiceEntry(
                "ability_bonus",
                race.BonusChoiceRule.Count,
                race.BonusChoiceRule.AllowedAbilities.ToList()));
        }

        if (race.SkillChoiceRule is not null)
        {
            choices.Add(new ChoiceEntry(
                "skill",
                race.SkillChoiceRule.Count,
                race.SkillChoiceRule.AvailableSkills.ToList()));
        }

        return choices;
    }

    private static string Slugify(string value)
    {
        var normalized = value.ToLowerInvariant();
        var chars = normalized.Select(character =>
            char.IsLetterOrDigit(character) ? character : '-').ToArray();
        return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private static EquipmentDocument Item(
        string slug,
        string name,
        string category,
        string? subcategory,
        decimal? costValue,
        string? costUnit,
        decimal? weightLb,
        bool isShield = false,
        int? armorClassBase = null,
        string? equipSlot = null)
    {
        return new EquipmentDocument
        {
            RulesetId = RulesetId,
            Slug = slug,
            Name = name,
            Category = category,
            Subcategory = subcategory,
            CostValue = costValue,
            CostUnit = costUnit,
            WeightLb = weightLb,
            IsShield = isShield,
            ArmorClassBase = armorClassBase,
            EquipSlot = equipSlot
        };
    }

    private static EquipmentDocument Weapon(
        string slug,
        string name,
        string category,
        string? subcategory,
        decimal? costValue,
        string? costUnit,
        decimal? weightLb,
        string? damageDice,
        string? damageType,
        bool isTwoHanded = false,
        params string[] weaponProperties)
    {
        var normalizedProperties = weaponProperties
            .Where(property => !string.IsNullOrWhiteSpace(property))
            .Select(property => property.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var attackAbility = normalizedProperties.Contains("finesse", StringComparer.OrdinalIgnoreCase)
            ? "finesse"
            : string.Equals(subcategory, "Ranged", StringComparison.OrdinalIgnoreCase)
                ? "dexterity"
                : "strength";

        return new EquipmentDocument
        {
            RulesetId = RulesetId,
            Slug = slug,
            Name = name,
            Category = category,
            Subcategory = subcategory,
            CostValue = costValue,
            CostUnit = costUnit,
            WeightLb = weightLb,
            DamageDice = damageDice,
            DamageType = damageType,
            AttackAbility = attackAbility,
            WeaponProperties = normalizedProperties,
            IsTwoHanded = isTwoHanded,
            EquipSlot = "hand"
        };
    }

    private static MonsterDocument Creature(
        string slug,
        string name,
        string size,
        string creatureType,
        string alignment,
        decimal challengeRating,
        int armorClass,
        int hitPoints,
        string hitDice,
        int speed,
        string attackName,
        int attackBonus,
        string damageDice,
        int damageBonus,
        string damageType)
    {
        return new MonsterDocument
        {
            RulesetId = RulesetId,
            Slug = slug,
            Name = name,
            Size = size,
            CreatureType = creatureType,
            Alignment = alignment,
            ChallengeRating = challengeRating,
            ArmorClass = armorClass,
            HitPoints = hitPoints,
            HitDice = hitDice,
            Speed = speed,
            AttackName = attackName,
            AttackBonus = attackBonus,
            DamageDice = damageDice,
            DamageBonus = damageBonus,
            DamageType = damageType
        };
    }

    private static SpellDocument Spell(
        string slug,
        string name,
        int spellLevel,
        List<string> classSlugs,
        string summary,
        string description,
        int minCharacterLevel = 1)
    {
        return new SpellDocument
        {
            RulesetId = RulesetId,
            Slug = slug,
            Name = name,
            SpellLevel = spellLevel,
            ClassSlugs = classSlugs,
            MinCharacterLevel = minCharacterLevel,
            Effects =
            [
                new EffectEntry("summary", "set", 0, summary),
                new EffectEntry("description", "set", 0, description)
            ]
        };
    }

}
