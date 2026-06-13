export type AbilityScore = {
  key: string
  score: number
  modifier: number
}

export type BaseAbilityScore = {
  key: string
  score: number
}

export type SkillLevel = {
  skillId: string
  level: number
}

export type SavingThrowBonus = {
  ability: string
  bonus: number
  isProficient: boolean
}

export type CharacterSummary = {
  id: string
  name: string
  race: string
  className: string
  subclass: string
  level: number
  armorClass: number
  weaponDamage?: string | null
  hitPoints: number
  maxHitPoints: number
  currentHitPoints: number
  spentHitDice: number
  availableHitDice: number
  passivePerception: number
  skills: SkillLevel[]
}

export type Character = CharacterSummary & {
  canEdit: boolean
  raceId: string
  classId: string
  backgroundId: string
  background: string
  alignment: string
  speed: number
  proficiencyBonus: number
  notes: string
  baseAbilities: BaseAbilityScore[]
  bonusAbilitySelections: string[]
  raceSkillSelections: string[]
  classSkillSelections: string[]
  skillProficiencies: string[]
  abilities: AbilityScore[]
  savingThrows: SavingThrowBonus[]
  spellSlots: Array<{ spellLevel: number; slots: number }>
  maxSpellSlots: Array<{ spellLevel: number; slots: number }>
  knownSpells: string[]
  calculationTrace: Array<{
    target: string
    source: string
    reason: string
    value: number
    operation: string
  }>
  inventory: string[]
  createdAtUtc: string
  updatedAtUtc: string
}

export type CharacterPayload = {
  name: string
  raceId: string
  classId: string
  backgroundId: string
  level: number
  alignment: string
  notes: string
  baseAbilities: BaseAbilityScore[]
  bonusAbilitySelections: string[]
  raceSkillSelections: string[]
  classSkillSelections: string[]
  spells: string[]
  inventory: string[]
}

export type CharacterRestPayload = {
  restType: 'short' | 'long' | 'full-heal'
  hitDiceToSpend?: number
}

export type CharacterRestResult = {
  restType: string
  previousCurrentHitPoints: number
  currentHitPoints: number
  maxHitPoints: number
  healed: number
  spentHitDice: number
  availableHitDice: number
  spellSlots: Array<{ spellLevel: number; slots: number }>
  maxSpellSlots: Array<{ spellLevel: number; slots: number }>
  details: string
}

export type CharacterCastSpellPayload = {
  spellSlug: string
  slotLevel?: number
}

export type CharacterCastSpellResult = {
  spellSlug: string
  spellName: string
  spellLevel: number
  slotLevel?: number | null
  consumedSlot: boolean
  spellSlots: Array<{ spellLevel: number; slots: number }>
  maxSpellSlots: Array<{ spellLevel: number; slots: number }>
  damageDice?: string | null
  damageType?: string | null
  damageRoll?: number | null
  damageTotal?: number | null
  message: string
}

export type AbilityBonus = {
  ability: string
  value: number
}

export type BonusChoiceRule = {
  count: number
  bonusValue: number
  allowedAbilities: string[]
  summary: string
}

export type SkillChoiceRule = {
  count: number
  availableSkills: string[]
  summary: string
}

export type FeatureDetail = {
  title: string
  description: string
}

export type RaceOption = {
  id: string
  parentRace: string
  name: string
  speed: number
  bonuses: AbilityBonus[]
  bonusChoiceRule: BonusChoiceRule | null
  grantedSkillProficiencies: string[]
  skillChoiceRule: SkillChoiceRule | null
  details: FeatureDetail[]
  summary: string
  grantedLanguages?: string[]
  description?: string
}

export type ClassOption = {
  id: string
  name: string
  hitDie: number
  primaryAbilities: string[]
  savingThrowProficiencies: string[]
  skillChoiceRule: SkillChoiceRule
  details: FeatureDetail[]
  summary: string
  description?: string
  proficiencyGroups?: Record<string, string[]>
}

export type BackgroundOption = {
  id: string
  name: string
  grantedSkillProficiencies: string[]
  details: FeatureDetail[]
  summary: string
  description?: string
}

export type CharacterOptions = {
  races: RaceOption[]
  classes: ClassOption[]
  backgrounds: BackgroundOption[]
  skillAbilityMap?: Record<string, string>
}

export type EquipmentCatalogItem = {
  slug: string
  name: string
  category?: string
  subcategory?: string
  costValue?: number
  costUnit?: string
  weightLb?: number
  damageDice?: string
  damageType?: string
  attackAbility?: string
  weaponProperties?: string[]
  isTwoHanded?: boolean
  isShield?: boolean
  armorClassBase?: number
  equipSlot?: string
}

export type MonsterCatalogItem = {
  slug: string
  name: string
  size?: string
  creatureType?: string
  alignment?: string
  challengeRating?: number
  armorClass?: number
  hitPoints?: number
  hitDice?: string
  speed?: number
  attackName?: string
  attackBonus?: number
  damageDice?: string
  damageBonus?: number
  damageType?: string
}

export type RuleSpellItem = {
  slug: string
  name: string
  classSlugs?: string[]
  spellLevel?: number
  minCharacterLevel?: number
  summary?: string
  description?: string
  damageDice?: string
  damageType?: string
}

export type RuleConditionItem = {
  slug: string
  name: string
  description: string
}

export type ApiValidationError = {
  message: string
  errors?: Record<string, string[]>
}

export type AuthUser = {
  id: string
  email: string
  displayName: string
  roles: string[]
}

export type RegisterPayload = {
  email: string
  password: string
  displayName: string
}

export type LoginPayload = {
  email: string
  password: string
  rememberMe: boolean
}

export type CreateRoomPayload = {
  name: string
}

export type JoinRoomPayload = {
  joinCode: string
}

export type JoinRoomByInvitePayload = {
  inviteToken: string
}

export type SelectRoomCharacterPayload = {
  characterId: string | null
}

export type UpdateRoomMemberRolePayload = {
  role: string
}

export type RoomMemberCharacter = {
  id: string
  name: string
  race: string
  className: string
  level: number
  armorClass: number
  maxHitPoints: number
  currentHitPoints: number
}

export type RoomMember = {
  userId: string
  displayName: string
  role: string
  isOwner: boolean
  isOnline: boolean
  joinedAtUtc: string
  characters: RoomMemberCharacter[]
  inventory: string[]
}

export type RoomSummary = {
  id: string
  name: string
  joinCode: string
  inviteToken: string
  ownerDisplayName: string
  memberCount: number
  connectedMemberCount: number
  currentUserRole: string
  isOwner: boolean
}

export type Room = {
  id: string
  name: string
  joinCode: string
  inviteToken: string
  ownerDisplayName: string
  currentUserRole: string
  canManageMembers: boolean
  connectedMembers: number
  members: RoomMember[]
}

export type RoomMonster = {
  id: string
  monsterSlug: string
  name: string
  challengeRating: number
  armorClass: number
  maxHitPoints: number
  currentHitPoints: number
  attackName: string
  attackBonus: number
  damageDice: string
  damageBonus: number
  damageType: string
}

export type MonsterDamageRoll = {
  monsterId: string
  monsterName: string
  attackName: string
  damageExpression: string
  diceResult: number
  damageBonus: number
  totalDamage: number
  rolledAtUtc: string
}

export type RoomMonsterDamageResult = {
  monsterId: string
  monsterName: string
  removed: boolean
  monster?: RoomMonster | null
}

export type MonsterAttackResult = {
  monsterId: string
  monsterName: string
  targetCharacterId: string
  targetCharacterName: string
  attackRoll: number
  attackBonus: number
  attackTotal: number
  targetArmorClass: number
  isCriticalHit: boolean
  isHit: boolean
  damageExpression: string
  damageDiceResult: number
  damageBonus: number
  damageTotal: number
  targetCurrentHitPoints: number
  targetMaxHitPoints: number
  rolledAtUtc: string
  message: string
}
