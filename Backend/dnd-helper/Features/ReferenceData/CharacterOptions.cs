namespace dnd_helper.Features.ReferenceData;

public sealed record CharacterOptionsDto(
    IReadOnlyList<RaceOptionDto> Races,
    IReadOnlyList<ClassOptionDto> Classes,
    IReadOnlyList<BackgroundOptionDto> Backgrounds);

public sealed record RaceOptionDto(
    string Id,
    string ParentRace,
    string Name,
    int Speed,
    IReadOnlyList<AbilityBonusDto> Bonuses,
    BonusChoiceRuleDto? BonusChoiceRule,
    IReadOnlyList<string> GrantedSkillProficiencies,
    SkillChoiceRuleDto? SkillChoiceRule,
    IReadOnlyList<FeatureDetailDto> Details,
    string Summary);

public sealed record ClassOptionDto(
    string Id,
    string Name,
    int HitDie,
    IReadOnlyList<string> PrimaryAbilities,
    IReadOnlyList<string> SavingThrowProficiencies,
    SkillChoiceRuleDto SkillChoiceRule,
    IReadOnlyList<FeatureDetailDto> Details,
    string Summary);

public sealed record BackgroundOptionDto(
    string Id,
    string Name,
    IReadOnlyList<string> GrantedSkillProficiencies,
    IReadOnlyList<FeatureDetailDto> Details,
    string Summary);

public sealed record AbilityBonusDto(string Ability, int Value);

public sealed record BonusChoiceRuleDto(
    int Count,
    int BonusValue,
    IReadOnlyList<string> AllowedAbilities,
    string Summary);

public sealed record SkillChoiceRuleDto(
    int Count,
    IReadOnlyList<string> AvailableSkills,
    string Summary);

public sealed record FeatureDetailDto(string Title, string Description);

public static class CharacterOptionsCatalog
{
    private static readonly string[] AllSkills =
    [
        "Acrobatics", "AnimalHandling", "Arcana", "Athletics", "Deception", "History",
        "Insight", "Intimidation", "Investigation", "Medicine", "Nature", "Perception",
        "Performance", "Persuasion", "Religion", "SleightOfHand", "Stealth", "Survival"
    ];

    public static readonly IReadOnlyList<RaceOptionDto> Races =
    [
        new(
            "human",
            "Человек",
            "Базовый человек",
            30,
            [new("STR", 1), new("DEX", 1), new("CON", 1), new("INT", 1), new("WIS", 1), new("CHA", 1)],
            null,
            [],
            null,
            [
                new("Повышение характеристик", "+1 ко всем шести характеристикам."),
                new("Скорость", "Базовая скорость перемещения составляет 30 футов.")
            ],
            "Универсальная раса с равномерным усилением всех характеристик."),
        new(
            "hill-dwarf",
            "Дворф",
            "Холмовой дворф",
            25,
            [new("CON", 2), new("WIS", 1)],
            null,
            [],
            null,
            [
                new("Повышение характеристик", "+2 к Телосложению и +1 к Мудрости."),
                new("Дворфийская стойкость", "Высокая выносливость и традиционная дворфийская живучесть.")
            ],
            "Выносливый персонаж с акцентом на живучесть и мудрость."),
        new(
            "mountain-dwarf",
            "Дворф",
            "Горный дворф",
            25,
            [new("STR", 2), new("CON", 2)],
            null,
            [],
            null,
            [
                new("Повышение характеристик", "+2 к Силе и +2 к Телосложению."),
                new("Боевой уклон", "Подходит для фронтовых бойцов и тяжёлой экипировки.")
            ],
            "Боевой дворф с сильными физическими характеристиками."),
        new(
            "high-elf",
            "Эльф",
            "Высший эльф",
            30,
            [new("DEX", 2), new("INT", 1)],
            null,
            ["Perception"],
            null,
            [
                new("Повышение характеристик", "+2 к Ловкости и +1 к Интеллекту."),
                new("Острое восприятие", "Автоматическое владение навыком Внимательность."),
                new("Эльфийская скорость", "Базовая скорость 30 футов.")
            ],
            "Ловкий и интеллектуальный эльф с магическим уклоном."),
        new(
            "wood-elf",
            "Эльф",
            "Лесной эльф",
            35,
            [new("DEX", 2), new("WIS", 1)],
            null,
            ["Perception"],
            null,
            [
                new("Повышение характеристик", "+2 к Ловкости и +1 к Мудрости."),
                new("Острое восприятие", "Автоматическое владение навыком Внимательность."),
                new("Быстрые ноги", "Скорость повышена до 35 футов.")
            ],
            "Быстрый следопыт с упором на ловкость и мудрость."),
        new(
            "dark-elf",
            "Эльф",
            "Тёмный эльф",
            30,
            [new("DEX", 2), new("CHA", 1)],
            null,
            ["Perception"],
            null,
            [
                new("Повышение характеристик", "+2 к Ловкости и +1 к Харизме."),
                new("Острое восприятие", "Автоматическое владение навыком Внимательность."),
                new("Социальный уклон", "Подходит для ловких и харизматичных персонажей.")
            ],
            "Ловкий харизматичный эльф с выраженной социальной стороной."),
        new(
            "lightfoot-halfling",
            "Полурослик",
            "Легконогий полурослик",
            25,
            [new("DEX", 2), new("CHA", 1)],
            null,
            [],
            null,
            [
                new("Повышение характеристик", "+2 к Ловкости и +1 к Харизме."),
                new("Скрытность", "Подходит для лёгких и незаметных персонажей.")
            ],
            "Скрытный персонаж с бонусом к харизме."),
        new(
            "stout-halfling",
            "Полурослик",
            "Коренастый полурослик",
            25,
            [new("DEX", 2), new("CON", 1)],
            null,
            [],
            null,
            [
                new("Повышение характеристик", "+2 к Ловкости и +1 к Телосложению."),
                new("Стойкость", "Более крепкий вариант полурослика.")
            ],
            "Ловкий и стойкий полурослик."),
        new(
            "dragonborn",
            "Драконорождённый",
            "Драконорождённый",
            30,
            [new("STR", 2), new("CHA", 1)],
            null,
            [],
            null,
            [
                new("Повышение характеристик", "+2 к Силе и +1 к Харизме."),
                new("Драконья кровь", "Хорошо подходит для харизматичных фронтовых бойцов.")
            ],
            "Сильный и харизматичный герой с драконьим наследием."),
        new(
            "forest-gnome",
            "Гном",
            "Лесной гном",
            25,
            [new("INT", 2), new("DEX", 1)],
            null,
            [],
            null,
            [
                new("Повышение характеристик", "+2 к Интеллекту и +1 к Ловкости."),
                new("Лесная сообразительность", "Подходит для ловких исследователей и магов.")
            ],
            "Интеллектуальный и ловкий гном."),
        new(
            "rock-gnome",
            "Гном",
            "Скальный гном",
            25,
            [new("INT", 2), new("CON", 1)],
            null,
            [],
            null,
            [
                new("Повышение характеристик", "+2 к Интеллекту и +1 к Телосложению."),
                new("Изобретательность", "Подходит для мастеров и исследователей.")
            ],
            "Гном-изобретатель с упором на интеллект и стойкость."),
        new(
            "half-elf",
            "Полуэльф",
            "Полуэльф",
            30,
            [new("CHA", 2)],
            new(2, 1, ["STR", "DEX", "CON", "INT", "WIS"], "Выбери две разные характеристики, кроме Харизмы, чтобы получить +1."),
            [],
            new(2, AllSkills, "Выбери любые два навыка, которыми персонаж будет владеть по расе."),
            [
                new("Повышение характеристик", "+2 к Харизме и ещё +1 к двум разным характеристикам на выбор."),
                new("Универсальность навыков", "Можно выбрать любые два навыка из полного списка.")
            ],
            "Гибкая раса: харизма, выбор бонусов и дополнительные навыки."),
        new(
            "half-orc",
            "Полуорк",
            "Полуорк",
            30,
            [new("STR", 2), new("CON", 1)],
            null,
            ["Intimidation"],
            null,
            [
                new("Повышение характеристик", "+2 к Силе и +1 к Телосложению."),
                new("Угрожающий вид", "Автоматическое владение навыком Запугивание.")
            ],
            "Мощный и выносливый боец."),
        new(
            "tiefling",
            "Тифлинг",
            "Тифлинг",
            30,
            [new("CHA", 2), new("INT", 1)],
            null,
            [],
            null,
            [
                new("Повышение характеристик", "+2 к Харизме и +1 к Интеллекту."),
                new("Инфернальная наследственность", "Подходит для харизматичных магов и манипуляторов.")
            ],
            "Харизматичный персонаж с бонусом к интеллекту.")
    ];

    public static readonly IReadOnlyList<ClassOptionDto> Classes =
    [
        new("barbarian", "Варвар", 12, ["STR", "CON"], ["STR", "CON"], new(2, ["AnimalHandling", "Athletics", "Intimidation", "Nature", "Perception", "Survival"], "Выбери 2 навыка класса."), [new("Спасброски", "Владение спасбросками Силы и Телосложения."), new("Навыки класса", "Можно выбрать 2 навыка из перечня варвара.")], "Передовая линия и высокий запас хитов."),
        new("bard", "Бард", 8, ["CHA", "DEX"], ["DEX", "CHA"], new(3, AllSkills, "Выбери 3 навыка класса."), [new("Спасброски", "Владение спасбросками Ловкости и Харизмы."), new("Навыки класса", "Можно выбрать любые 3 навыка.")], "Поддержка, контроль и социальные взаимодействия."),
        new("cleric", "Жрец", 8, ["WIS", "CON"], ["WIS", "CHA"], new(2, ["History", "Insight", "Medicine", "Persuasion", "Religion"], "Выбери 2 навыка класса."), [new("Спасброски", "Владение спасбросками Мудрости и Харизмы."), new("Навыки класса", "Можно выбрать 2 навыка из перечня жреца.")], "Заклинатель с опорой на мудрость."),
        new("druid", "Друид", 8, ["WIS", "CON"], ["INT", "WIS"], new(2, ["Arcana", "AnimalHandling", "Insight", "Medicine", "Nature", "Perception", "Religion", "Survival"], "Выбери 2 навыка класса."), [new("Спасброски", "Владение спасбросками Интеллекта и Мудрости."), new("Навыки класса", "Можно выбрать 2 навыка из перечня друида.")], "Гибкий заклинатель природы."),
        new("fighter", "Воин", 10, ["STR", "DEX"], ["STR", "CON"], new(2, ["Acrobatics", "AnimalHandling", "Athletics", "History", "Insight", "Intimidation", "Perception", "Survival"], "Выбери 2 навыка класса."), [new("Спасброски", "Владение спасбросками Силы и Телосложения."), new("Навыки класса", "Можно выбрать 2 навыка из перечня воина.")], "Универсальный боевой класс."),
        new("monk", "Монах", 8, ["DEX", "WIS"], ["STR", "DEX"], new(2, ["Acrobatics", "Athletics", "History", "Insight", "Religion", "Stealth"], "Выбери 2 навыка класса."), [new("Спасброски", "Владение спасбросками Силы и Ловкости."), new("Навыки класса", "Можно выбрать 2 навыка из перечня монаха.")], "Мобильный класс с опорой на ловкость."),
        new("paladin", "Паладин", 10, ["STR", "CHA"], ["WIS", "CHA"], new(2, ["Athletics", "Insight", "Intimidation", "Medicine", "Persuasion", "Religion"], "Выбери 2 навыка класса."), [new("Спасброски", "Владение спасбросками Мудрости и Харизмы."), new("Навыки класса", "Можно выбрать 2 навыка из перечня паладина.")], "Защитник с харизмой и силой."),
        new("ranger", "Следопыт", 10, ["DEX", "WIS"], ["STR", "DEX"], new(3, ["AnimalHandling", "Athletics", "Insight", "Investigation", "Nature", "Perception", "Stealth", "Survival"], "Выбери 3 навыка класса."), [new("Спасброски", "Владение спасбросками Силы и Ловкости."), new("Навыки класса", "Можно выбрать 3 навыка из перечня следопыта.")], "Следопыт с мобильностью и наблюдательностью."),
        new("rogue", "Плут", 8, ["DEX", "INT"], ["DEX", "INT"], new(4, ["Acrobatics", "Athletics", "Deception", "Insight", "Intimidation", "Investigation", "Perception", "Performance", "Persuasion", "SleightOfHand", "Stealth"], "Выбери 4 навыка класса."), [new("Спасброски", "Владение спасбросками Ловкости и Интеллекта."), new("Навыки класса", "Можно выбрать 4 навыка из перечня плута.")], "Скрытность, мобильность и точность."),
        new("sorcerer", "Чародей", 6, ["CHA", "CON"], ["CON", "CHA"], new(2, ["Arcana", "Deception", "Insight", "Intimidation", "Persuasion", "Religion"], "Выбери 2 навыка класса."), [new("Спасброски", "Владение спасбросками Телосложения и Харизмы."), new("Навыки класса", "Можно выбрать 2 навыка из перечня чародея.")], "Заклинатель с сильной харизмой."),
        new("warlock", "Колдун", 8, ["CHA", "CON"], ["WIS", "CHA"], new(2, ["Arcana", "Deception", "History", "Intimidation", "Investigation", "Nature", "Religion"], "Выбери 2 навыка класса."), [new("Спасброски", "Владение спасбросками Мудрости и Харизмы."), new("Навыки класса", "Можно выбрать 2 навыка из перечня колдуна.")], "Гибкий заклинатель с мистическим источником."),
        new("wizard", "Волшебник", 6, ["INT", "CON"], ["INT", "WIS"], new(2, ["Arcana", "History", "Insight", "Investigation", "Medicine", "Religion"], "Выбери 2 навыка класса."), [new("Спасброски", "Владение спасбросками Интеллекта и Мудрости."), new("Навыки класса", "Можно выбрать 2 навыка из перечня волшебника.")], "Заклинатель с опорой на интеллект.")
    ];

    public static readonly IReadOnlyList<BackgroundOptionDto> Backgrounds =
    [
        new("acolyte", "Послушник", ["Insight", "Religion"], [new("Навыки", "Владение навыками Проницательность и Религия.")], "Религиозное прошлое и связи с храмами."),
        new("charlatan", "Шарлатан", ["Deception", "SleightOfHand"], [new("Навыки", "Владение навыками Обман и Ловкость рук.")], "Ложные личности, обман и социальная гибкость."),
        new("criminal", "Преступник", ["Deception", "Stealth"], [new("Навыки", "Владение навыками Обман и Скрытность.")], "Контакты в теневом мире и ловкость."),
        new("entertainer", "Артист", ["Acrobatics", "Performance"], [new("Навыки", "Владение навыками Акробатика и Выступление.")], "Публичность, выступления и харизма."),
        new("folk-hero", "Народный герой", ["AnimalHandling", "Survival"], [new("Навыки", "Владение навыками Уход за животными и Выживание.")], "Поддержка простых людей и репутация."),
        new("guild-artisan", "Ремесленник гильдии", ["Insight", "Persuasion"], [new("Навыки", "Владение навыками Проницательность и Убеждение.")], "Профессия, ремесло и деловые связи."),
        new("hermit", "Отшельник", ["Medicine", "Religion"], [new("Навыки", "Владение навыками Медицина и Религия.")], "Изоляция, размышления и внутреннее знание."),
        new("noble", "Дворянин", ["History", "Persuasion"], [new("Навыки", "Владение навыками История и Убеждение.")], "Статус, происхождение и общественное влияние."),
        new("outlander", "Чужеземец", ["Athletics", "Survival"], [new("Навыки", "Владение навыками Атлетика и Выживание.")], "Выживание вдали от цивилизации."),
        new("sage", "Мудрец", ["Arcana", "History"], [new("Навыки", "Владение навыками Магия и История.")], "Учёность, архивы и знания."),
        new("sailor", "Моряк", ["Athletics", "Perception"], [new("Навыки", "Владение навыками Атлетика и Внимательность.")], "Опыт путешествий и командной работы."),
        new("soldier", "Солдат", ["Athletics", "Intimidation"], [new("Навыки", "Владение навыками Атлетика и Запугивание.")], "Дисциплина, армейское прошлое и опыт боя."),
        new("urchin", "Беспризорник", ["SleightOfHand", "Stealth"], [new("Навыки", "Владение навыками Ловкость рук и Скрытность.")], "Выживание на улицах и скрытность.")
    ];

    public static readonly CharacterOptionsDto All = new(
        Races,
        Classes,
        Backgrounds);
}
