export type CatalogTabKey = 'races' | 'classes' | 'backgrounds' | 'spells' | 'equipment' | 'monsters' | 'conditions'

export type CatalogDetailModal = {
  title: string
  subtitle?: string
  blocks: Array<{ title: string; text: string }>
}

export const catalogTabs: Array<{ key: CatalogTabKey; label: string }> = [
  { key: 'races', label: 'Расы' },
  { key: 'classes', label: 'Классы' },
  { key: 'backgrounds', label: 'Предыстории' },
  { key: 'spells', label: 'Заклинания' },
  { key: 'equipment', label: 'Снаряжение' },
  { key: 'monsters', label: 'Существа' },
  { key: 'conditions', label: 'Состояния' },
]

export function translateToken(value?: string) {
  if (!value) return ''
  const map: Record<string, string> = {
    Armor: 'Доспехи',
    'Martial Weapon': 'Воинское оружие',
    'Simple Weapon': 'Простое оружие',
    'Adventuring Gear': 'Походное снаряжение',
    'Arcane Focus': 'Магический фокус',
    'Holy Symbol': 'Священный символ',
    'Druidic Focus': 'Друидический фокус',
    'Musical Instrument': 'Музыкальный инструмент',
    Tools: 'Инструменты',
    Light: 'Лёгкие',
    Medium: 'Средние',
    Heavy: 'Тяжёлые',
    Shield: 'Щиты',
    Melee: 'Ближний бой',
    Ranged: 'Дальний бой',
    Container: 'Контейнеры',
    Travel: 'Путевое',
    Food: 'Провизия',
    Medical: 'Медицина',
    Instrument: 'Инструменты барда',
    Spellcasting: 'Заклинания',
    'Artisan/Utility': 'Ремесло/Утилиты',
    beast: 'Зверь',
    humanoid: 'Гуманоид',
    undead: 'Нежить',
    monstrosity: 'Монстр',
    giant: 'Великан',
    dragon: 'Дракон',
    ooze: 'Слизь',
    elemental: 'Элементаль',
    unaligned: 'Без мировоззрения',
    'neutral good': 'Нейтрально-добрый',
    neutral: 'Нейтральный',
    'neutral evil': 'Нейтрально-злой',
    'chaotic evil': 'Хаотично-злой',
    'lawful evil': 'Законно-злой',
    slashing: 'рубящий',
    bludgeoning: 'дробящий',
    piercing: 'колющий',
    acid: 'кислотный',
    special: 'особый',
    gp: 'зм',
    sp: 'см',
    cp: 'мм',
    Tiny: 'Крошечный',
    Small: 'Маленький',
    Large: 'Большой',
    Huge: 'Огромный',
    Gargantuan: 'Громадный',
  }
  return map[value] ?? value
}

export function translateClassSlug(value?: string) {
  const map: Record<string, string> = {
    barbarian: 'Варвар',
    bard: 'Бард',
    cleric: 'Жрец',
    druid: 'Друид',
    fighter: 'Воин',
    monk: 'Монах',
    paladin: 'Паладин',
    ranger: 'Следопыт',
    rogue: 'Плут',
    sorcerer: 'Чародей',
    warlock: 'Колдун',
    wizard: 'Волшебник',
  }
  return map[(value ?? '').toLowerCase()] ?? value ?? ''
}
