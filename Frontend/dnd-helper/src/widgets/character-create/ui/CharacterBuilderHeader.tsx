import { Link } from 'react-router-dom'
import { getStepTitle, steps, type StepKey } from '../../../features/character-create/model/characterCreateModel'

type CharacterBuilderHeaderProps = {
  isEditMode: boolean
  characterId?: string
  currentStep: StepKey
}

export function CharacterBuilderHeader({ isEditMode, characterId, currentStep }: CharacterBuilderHeaderProps) {
  const basePath = isEditMode && characterId ? `/characters/${characterId}/edit` : '/characters/new'

  return (
    <section className="builder-header">
      <div>
        <h2>{isEditMode ? 'Редактирование персонажа' : 'Создание персонажа'}</h2>
      </div>
      <div className="wizard-steps" aria-label="Шаги конструктора">
        {steps.map((stepKey, index) => (
          <Link
            key={stepKey}
            to={`${basePath}/${stepKey}`}
            className={`wizard-step ${stepKey === currentStep ? 'active' : ''}`}
          >
            <span>{index + 1}</span>
            {getStepTitle(stepKey)}
          </Link>
        ))}
      </div>
    </section>
  )
}
