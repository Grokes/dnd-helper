import type { InfoModalState } from '../../../features/character-create/model/characterCreateModel'

type InfoModalTab = 'overview' | 'features' | 'proficiencies'

type CharacterOptionInfoModalProps = {
  modal: InfoModalState | null
  activeTab: InfoModalTab
  expandedFeature: string | null
  selectedFeatureLevel: number | null
  onClose: () => void
  onTabChange: (tab: InfoModalTab) => void
  onExpandedFeatureChange: (feature: string | null) => void
  onSelectedFeatureLevelChange: (level: number) => void
}

export function CharacterOptionInfoModal({
  modal,
  activeTab,
  expandedFeature,
  selectedFeatureLevel,
  onClose,
  onTabChange,
  onExpandedFeatureChange,
  onSelectedFeatureLevelChange,
}: CharacterOptionInfoModalProps) {
  if (!modal) {
    return null
  }

  return (
    <div className="modal-overlay" role="presentation" onClick={onClose}>
      <div className="modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3>{modal.title}</h3>
            <p className="section-text">{modal.subtitle}</p>
          </div>
          <button type="button" className="secondary-button button-reset" onClick={onClose}>
            Закрыть
          </button>
        </div>

        <div className="wizard-steps" aria-label="Разделы описания">
          <button type="button" className={`wizard-step button-reset ${activeTab === 'overview' ? 'active' : ''}`} onClick={() => onTabChange('overview')}>
            Обзор
          </button>
          <button type="button" className={`wizard-step button-reset ${activeTab === 'features' ? 'active' : ''}`} onClick={() => onTabChange('features')}>
            Особенности
          </button>
          <button type="button" className={`wizard-step button-reset ${activeTab === 'proficiencies' ? 'active' : ''}`} onClick={() => onTabChange('proficiencies')}>
            Владения
          </button>
        </div>

        {activeTab === 'overview' ? (
          <div className="modal-facts">
            {modal.overview.map((fact) => (
              <div key={fact.label} className="status-card">
                <span>{fact.label}</span>
                <strong>{fact.value}</strong>
              </div>
            ))}
          </div>
        ) : null}

        {activeTab === 'features' ? (
          <InfoModalFeatures
            modal={modal}
            expandedFeature={expandedFeature}
            selectedFeatureLevel={selectedFeatureLevel}
            onExpandedFeatureChange={onExpandedFeatureChange}
            onSelectedFeatureLevelChange={onSelectedFeatureLevelChange}
          />
        ) : null}

        {activeTab === 'proficiencies' ? (
          <div className="stack">
            {modal.proficiencies.length > 0 ? modal.proficiencies.map((entry) => (
              <div key={entry.category} className="surface-card modal-detail">
                <h4>{entry.category}</h4>
                <div className="skill-pick-grid">
                  {entry.items.length > 0 ? entry.items.map((item) => (
                    <span key={item} className="bonus-chip static">{item}</span>
                  )) : <span className="muted">Нет обязательных навыков для этого класса.</span>}
                </div>
              </div>
            )) : (
              <p className="muted">Нет отдельных владений в этом разделе.</p>
            )}
          </div>
        ) : null}
      </div>
    </div>
  )
}

type InfoModalFeaturesProps = {
  modal: InfoModalState
  expandedFeature: string | null
  selectedFeatureLevel: number | null
  onExpandedFeatureChange: (feature: string | null) => void
  onSelectedFeatureLevelChange: (level: number) => void
}

function InfoModalFeatures({
  modal,
  expandedFeature,
  selectedFeatureLevel,
  onExpandedFeatureChange,
  onSelectedFeatureLevelChange,
}: InfoModalFeaturesProps) {
  const classFeatureGroups = modal.features.reduce((acc, detail) => {
    const match = detail.title.match(/(\d+)\s*уровень\s*:\s*(.+)/i)
    if (!match) {
      return acc
    }
    const level = Number(match[1])
    const featureName = match[2].trim()
    const list = acc.get(level) ?? []
    if (!list.some((item) => item.title === featureName)) {
      list.push({ title: featureName, description: detail.description })
    }
    acc.set(level, list)
    return acc
  }, new Map<number, Array<{ title: string; description: string }>>())

  if (classFeatureGroups.size > 0) {
    const levels = Array.from(classFeatureGroups.keys()).sort((a, b) => a - b)
    const activeLevel = selectedFeatureLevel ?? levels[0]
    const features = classFeatureGroups.get(activeLevel) ?? []
    return (
      <div className="stack">
        <div className="skill-pick-grid">
          {levels.map((level) => (
            <button
              key={level}
              type="button"
              className={`bonus-chip ${activeLevel === level ? 'selected' : ''}`}
              onClick={() => onSelectedFeatureLevelChange(level)}
            >
              {level} уровень
            </button>
          ))}
        </div>
        <div className="stack">
          {features.map((detail) => (
            <div key={`${activeLevel}-${detail.title}`} className="surface-card modal-detail">
              <button
                type="button"
                className="disclosure-button"
                onClick={() => onExpandedFeatureChange(expandedFeature === `${activeLevel}-${detail.title}` ? null : `${activeLevel}-${detail.title}`)}
              >
                <span>{detail.title}</span>
                <strong>i</strong>
              </button>
              {expandedFeature === `${activeLevel}-${detail.title}` ? <p>{detail.description}</p> : null}
            </div>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="stack">
      {modal.features.map((detail) => (
        <div key={detail.title} className="surface-card modal-detail">
          <button
            type="button"
            className="disclosure-button"
            onClick={() => onExpandedFeatureChange(expandedFeature === detail.title ? null : detail.title)}
          >
            <span>{detail.title}</span>
            <strong>i</strong>
          </button>
          {expandedFeature === detail.title ? <p>{detail.description}</p> : null}
        </div>
      ))}
    </div>
  )
}
