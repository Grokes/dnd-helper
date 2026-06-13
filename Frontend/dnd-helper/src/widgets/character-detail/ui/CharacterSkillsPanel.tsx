import type { SkillLevel } from '../../../types/character'
import { translateSkill } from '../../../utils/characterPresentation'

type CharacterSkillsPanelProps = {
  skills: SkillLevel[]
  onRoll: (label: string, dieSides: number, modifier?: number) => void
}

export function CharacterSkillsPanel({ skills, onRoll }: CharacterSkillsPanelProps) {
  return (
    <article className="surface-card">
      <h3>Навыки</h3>
      <ul className="plain-list sheet-list">
        {skills.map((skill) => (
          <li key={skill.skillId}>
            <button
              type="button"
              className="button-reset rollable-list-button sheet-list-button"
              onClick={() => onRoll(`Навык: ${translateSkill(skill.skillId)}`, 20, skill.level)}
            >
              {translateSkill(skill.skillId)} {skill.level >= 0 ? `+${skill.level}` : skill.level}
            </button>
          </li>
        ))}
      </ul>
    </article>
  )
}
