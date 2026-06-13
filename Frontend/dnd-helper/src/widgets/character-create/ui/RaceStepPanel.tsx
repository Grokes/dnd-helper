import { Fragment } from 'react'
import type { RaceOption } from '../../../types/character'
import { buildInfoIconLabel } from '../../../features/character-create/model/characterCreateModel'
import { translateAbility, translateSkill } from '../../../utils/characterPresentation'

type RaceStepPanelProps = {
  raceGroups: Array<[string, RaceOption[]]>
  selectedRaceId: string
  bonusAbilitySelections: string[]
  raceSkillSelections: string[]
  onSelectRace: (raceId: string) => void
  onOpenRaceInfo: (race: RaceOption) => void
  onToggleBonusSelection: (ability: string) => void
  onToggleRaceSkill: (skillId: string) => void
}

export function RaceStepPanel({
  raceGroups,
  selectedRaceId,
  bonusAbilitySelections,
  raceSkillSelections,
  onSelectRace,
  onOpenRaceInfo,
  onToggleBonusSelection,
  onToggleRaceSkill,
}: RaceStepPanelProps) {
  return (
    <article className="surface-card builder-section">
      <h3>Раса</h3>
      <div className="race-group-stack">
        {raceGroups.map(([group, races]) => (
          <section key={group} className="race-group">
            <div className="race-group__title">
              <h4>{group}</h4>
            </div>
            <div className="choice-grid compact">
              {races.map((race) => (
                <Fragment key={race.id}>
                  <article
                    className={`choice-card slim choice-card--static ${race.id === selectedRaceId ? 'selected' : ''}`}
                  >
                    <button
                      type="button"
                      className="info-icon-button"
                      aria-label={buildInfoIconLabel(race.name)}
                      onClick={() => onOpenRaceInfo(race)}
                    >
                      i
                    </button>
                    <button type="button" className="choice-card__main" onClick={() => onSelectRace(race.id)}>
                      <strong>{race.name}</strong>
                      {race.summary && race.summary !== race.name ? <small>{race.summary}</small> : null}
                      <small className="multiline-text">
                        Бонусы:{'\n'}
                        {(race.bonuses.map((bonus) => `${translateAbility(bonus.ability)} +${bonus.value}`).join('\n')) || 'нет'}
                      </small>
                      {race.skillChoiceRule ? (
                        <small>Выбор навыков: {race.skillChoiceRule.count}</small>
                      ) : null}
                    </button>
                  </article>
                  {race.id === selectedRaceId && race.bonusChoiceRule ? (
                    <div className="bonus-choice-panel full-width race-skill-selection-panel">
                      <p>{race.bonusChoiceRule.summary} (выбери: {race.bonusChoiceRule.count})</p>
                      <div className="bonus-choice-grid">
                        {race.bonusChoiceRule.allowedAbilities.map((ability) => (
                          <button
                            key={ability}
                            type="button"
                            className={`bonus-chip ${bonusAbilitySelections.includes(ability) ? 'selected' : ''}`}
                            onClick={() => onToggleBonusSelection(ability)}
                          >
                            {translateAbility(ability)}
                          </button>
                        ))}
                      </div>
                    </div>
                  ) : null}
                  {race.id === selectedRaceId && race.skillChoiceRule ? (
                    <div className="bonus-choice-panel full-width race-skill-selection-panel">
                      <p>{race.skillChoiceRule.summary} (выбери: {race.skillChoiceRule.count})</p>
                      <div className="skill-pick-grid">
                        {race.skillChoiceRule.availableSkills.map((skill) => (
                          <button
                            key={skill}
                            type="button"
                            className={`skill-toggle ${raceSkillSelections.includes(skill) ? 'selected' : ''}`}
                            onClick={() => onToggleRaceSkill(skill)}
                          >
                            {translateSkill(skill)}
                          </button>
                        ))}
                      </div>
                    </div>
                  ) : null}
                </Fragment>
              ))}
            </div>
          </section>
        ))}
      </div>
    </article>
  )
}
