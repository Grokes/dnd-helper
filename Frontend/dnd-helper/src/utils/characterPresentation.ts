import type { SkillLevel } from '../types/character'

const abilityNames: Record<string, string> = {
  STR: 'Сила',
  DEX: 'Ловкость',
  CON: 'Телосложение',
  INT: 'Интеллект',
  WIS: 'Мудрость',
  CHA: 'Харизма',
}

const skillNames: Record<string, string> = {
  Acrobatics: 'Акробатика',
  AnimalHandling: 'Уход за животными',
  Arcana: 'Магия',
  Athletics: 'Атлетика',
  Deception: 'Обман',
  History: 'История',
  Insight: 'Проницательность',
  Intimidation: 'Запугивание',
  Investigation: 'Анализ',
  Medicine: 'Медицина',
  Nature: 'Природа',
  Perception: 'Внимательность',
  Performance: 'Выступление',
  Persuasion: 'Убеждение',
  Religion: 'Религия',
  SleightOfHand: 'Ловкость рук',
  Stealth: 'Скрытность',
  Survival: 'Выживание',
  Acro: 'Акробатика',
  Anim: 'Уход за животными',
  Arca: 'Магия',
  Athl: 'Атлетика',
  Dece: 'Обман',
  Hist: 'История',
  Insi: 'Проницательность',
  Inti: 'Запугивание',
  Inve: 'Анализ',
  Medi: 'Медицина',
  Natu: 'Природа',
  Perc: 'Внимательность',
  Perf: 'Выступление',
  Pers: 'Убеждение',
  Reli: 'Религия',
  Slei: 'Ловкость рук',
  Stea: 'Скрытность',
  Surv: 'Выживание',
}

function hashString(value: string) {
  let hash = 0

  for (let index = 0; index < value.length; index += 1) {
    hash = (hash << 5) - hash + value.charCodeAt(index)
    hash |= 0
  }

  return Math.abs(hash)
}

export function translateSkill(skill: string) {
  return skillNames[skill] ?? skill
}

export function translateAbility(ability: string) {
  return abilityNames[ability] ?? ability
}

export function formatSkillLevel(skill: SkillLevel) {
  const prefix = skill.level >= 0 ? `+${skill.level}` : `${skill.level}`
  return `${translateSkill(skill.skillId)} ${prefix}`
}

export function getCharacterPortrait(name: string, race: string, className: string) {
  const seed = hashString(`${name}-${race}-${className}`)
  const hueA = seed % 360
  const hueB = (seed + 60) % 360
  const initials = name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')

  const svg = `
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96">
      <defs>
        <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stop-color="hsl(${hueA} 70% 55%)" />
          <stop offset="100%" stop-color="hsl(${hueB} 60% 38%)" />
        </linearGradient>
      </defs>
      <rect width="96" height="96" rx="24" fill="url(#bg)" />
      <circle cx="48" cy="34" r="16" fill="rgba(255,255,255,0.18)" />
      <path d="M24 78c4-14 16-22 24-22s20 8 24 22" fill="rgba(255,255,255,0.18)" />
      <text x="48" y="53" font-size="18" text-anchor="middle" fill="white" font-family="Arial, sans-serif" font-weight="700">${initials}</text>
    </svg>
  `.trim()

  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`
}

export const availableSkills = [
  { id: 'Acrobatics', label: 'Акробатика' },
  { id: 'AnimalHandling', label: 'Уход за животными' },
  { id: 'Arcana', label: 'Магия' },
  { id: 'Athletics', label: 'Атлетика' },
  { id: 'Deception', label: 'Обман' },
  { id: 'History', label: 'История' },
  { id: 'Insight', label: 'Проницательность' },
  { id: 'Intimidation', label: 'Запугивание' },
  { id: 'Investigation', label: 'Анализ' },
  { id: 'Medicine', label: 'Медицина' },
  { id: 'Nature', label: 'Природа' },
  { id: 'Perception', label: 'Внимательность' },
  { id: 'Performance', label: 'Выступление' },
  { id: 'Persuasion', label: 'Убеждение' },
  { id: 'Religion', label: 'Религия' },
  { id: 'SleightOfHand', label: 'Ловкость рук' },
  { id: 'Stealth', label: 'Скрытность' },
  { id: 'Survival', label: 'Выживание' },
] as const
