import test from 'node:test'
import assert from 'node:assert/strict'
import {
  formatSkillLevel,
  getCharacterPortrait,
  translateAbility,
  translateSkill,
} from './characterPresentation'

test('translates abilities and skills while preserving unknown values', () => {
  assert.equal(translateAbility('STR'), 'Сила')
  assert.equal(translateAbility('LUCK'), 'LUCK')
  assert.equal(translateSkill('Perception'), 'Внимательность')
  assert.equal(translateSkill('Perc'), 'Внимательность')
  assert.equal(translateSkill('UnknownSkill'), 'UnknownSkill')
})

test('formatSkillLevel formats positive and negative bonuses', () => {
  assert.equal(formatSkillLevel({ skillId: 'Athletics', level: 4 }), 'Атлетика +4')
  assert.equal(formatSkillLevel({ skillId: 'Stealth', level: -1 }), 'Скрытность -1')
})

test('getCharacterPortrait is deterministic for race and class and keeps initials from name', () => {
  const portraitA = getCharacterPortrait('Ада Лавлейс', 'Эльф', 'Волшебник')
  const portraitB = getCharacterPortrait('Другое Имя', 'Эльф', 'Волшебник')
  const decodedA = decodeURIComponent(portraitA)
  const decodedB = decodeURIComponent(portraitB)

  assert.ok(portraitA.startsWith('data:image/svg+xml;utf8,'))
  assert.match(decodedA, /hsl\(\d+ 70% 55%\)/)
  assert.match(decodedB, /hsl\(\d+ 70% 55%\)/)
  assert.match(decodedA, />АЛ</)
  assert.match(decodedB, />ДИ</)
  assert.equal(decodedA.match(/stop-color="hsl\(\d+ 70% 55%\)"/)?.[0], decodedB.match(/stop-color="hsl\(\d+ 70% 55%\)"/)?.[0])
})

test('getCharacterPortrait supports empty character names without changing palette seed', () => {
  const portrait = getCharacterPortrait('', 'Гном', 'Плут')
  const decoded = decodeURIComponent(portrait)

  assert.ok(portrait.startsWith('data:image/svg+xml;utf8,'))
  assert.match(decoded, /<text[^>]*><\/text>/)
  assert.match(decoded, /hsl\(\d+ 70% 55%\)/)
})
