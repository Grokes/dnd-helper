import { apiRequest } from '../../../shared/api/http'
import type { EquipmentCatalogItem, MonsterCatalogItem, RuleConditionItem, RuleSpellItem } from '../../../types/character'

export function getEquipmentCatalog() {
  return apiRequest<EquipmentCatalogItem[]>('/api/equipment')
}

export function getMonstersCatalog() {
  return apiRequest<MonsterCatalogItem[]>('/api/monsters')
}

export function getRulesSpells() {
  return apiRequest<RuleSpellItem[]>('/api/rules/spells')
}

export function getRulesConditions() {
  return apiRequest<RuleConditionItem[]>('/api/rules/conditions')
}
