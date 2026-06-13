import test from 'node:test'
import assert from 'node:assert/strict'
import { getRoomRoleLabel, translateDamageType, translateMonsterMeta } from './roomModel'

test('getRoomRoleLabel translates supported room roles', () => {
  assert.equal(getRoomRoleLabel('GameMaster'), 'Ведущий')
  assert.equal(getRoomRoleLabel('Player'), 'Игрок')
  assert.equal(getRoomRoleLabel('Unknown'), 'Игрок')
})

test('room model translates damage and monster metadata', () => {
  assert.equal(translateDamageType('slashing'), 'рубящий')
  assert.equal(translateDamageType('unknown'), 'unknown')
  assert.equal(translateMonsterMeta('huge'), 'Огромный')
  assert.equal(translateMonsterMeta('chaotic evil'), 'Хаотично-злой')
  assert.equal(translateMonsterMeta('custom'), 'custom')
})
