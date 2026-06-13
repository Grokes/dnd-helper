export type RoomNoticeKind = 'success' | 'error' | 'info'

export type RoomNotice = {
  id: number
  kind: RoomNoticeKind
  text: string
}

export function getRoomRoleLabel(role: string) {
  return role === 'GameMaster' ? 'Ведущий' : 'Игрок'
}

export function translateDamageType(value?: string) {
  const normalized = (value ?? '').trim().toLowerCase()
  const map: Record<string, string> = {
    slashing: 'рубящий',
    piercing: 'колющий',
    bludgeoning: 'дробящий',
    fire: 'огненный',
    cold: 'холод',
    poison: 'яд',
    acid: 'кислотный',
  }
  return map[normalized] ?? value ?? ''
}

export function translateMonsterMeta(value?: string) {
  const normalized = (value ?? '').trim().toLowerCase()
  const map: Record<string, string> = {
    tiny: 'Крошечный',
    small: 'Маленький',
    medium: 'Средний',
    large: 'Большой',
    huge: 'Огромный',
    gargantuan: 'Громадный',
    beast: 'Зверь',
    humanoid: 'Гуманоид',
    undead: 'Нежить',
    monstrosity: 'Монстр',
    giant: 'Великан',
    dragon: 'Дракон',
    ooze: 'Слизь',
    elemental: 'Элементаль',
    unaligned: 'Без мировоззрения',
    neutral: 'Нейтральный',
    'neutral good': 'Нейтрально-добрый',
    'neutral evil': 'Нейтрально-злой',
    'chaotic evil': 'Хаотично-злой',
    'lawful evil': 'Законно-злой',
  }
  return map[normalized] ?? value ?? ''
}
