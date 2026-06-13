import test from 'node:test'
import assert from 'node:assert/strict'
import {
  humanizeSpellFallback,
  parseDiceExpression,
  parseEquipEntry,
  translateClassSlug,
  translateDamageType,
} from './characterDetailModel'

test('parseEquipEntry returns all equipped slots from inventory token', () => {
  assert.deepEqual(parseEquipEntry('equip:body=chain-mail;main=greatsword;off='), {
    body: 'chain-mail',
    mainHand: 'greatsword',
    offHand: null,
  })
})

test('parseEquipEntry ignores non equipment inventory entries', () => {
  assert.deepEqual(parseEquipEntry('item:rope'), {
    body: null,
    mainHand: null,
    offHand: null,
  })
})

test('parseDiceExpression supports dice and flat damage values', () => {
  assert.deepEqual(parseDiceExpression('2d6'), { count: 2, sides: 6, isFlat: false })
  assert.deepEqual(parseDiceExpression('7'), { count: 7, sides: 0, isFlat: true })
  assert.equal(parseDiceExpression('d6'), null)
  assert.equal(parseDiceExpression('-1'), null)
})

test('presentation helpers translate known catalog values and keep unknown values visible', () => {
  assert.equal(humanizeSpellFallback('burning-hands'), 'burning hands')
  assert.equal(translateClassSlug('fighter'), 'Воин')
  assert.equal(translateClassSlug('unknown-class'), 'unknown-class')
  assert.equal(translateDamageType('slashing'), 'рубящий')
  assert.equal(translateDamageType('unknown'), 'unknown')
})
