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
  hitPoints: number
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
  spells: string[]
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
}

export type BackgroundOption = {
  id: string
  name: string
  grantedSkillProficiencies: string[]
  details: FeatureDetail[]
  summary: string
}

export type CharacterOptions = {
  races: RaceOption[]
  classes: ClassOption[]
  backgrounds: BackgroundOption[]
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

export type UpdateRoomSessionPayload = {
  activeMemberUserId: string | null
}

export type RoomMemberCharacter = {
  id: string
  name: string
  race: string
  className: string
  level: number
}

export type RoomMember = {
  userId: string
  displayName: string
  role: string
  isOwner: boolean
  isOnline: boolean
  joinedAtUtc: string
  character: RoomMemberCharacter | null
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
  activeMemberDisplayName: string | null
  activeCharacterName: string | null
}

export type RoomSession = {
  activeMemberUserId: string | null
  activeMemberDisplayName: string | null
  activeCharacterName: string | null
  updatedAtUtc: string | null
  connectedMembers: number
}

export type Room = {
  id: string
  name: string
  joinCode: string
  inviteToken: string
  ownerDisplayName: string
  currentUserRole: string
  canManageMembers: boolean
  canManageSession: boolean
  session: RoomSession
  members: RoomMember[]
}
