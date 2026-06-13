import test from 'node:test'
import assert from 'node:assert/strict'
import { catalogTabs, translateClassSlug, translateToken } from './catalogModel'

test('catalogTabs exposes all top-level reference sections', () => {
  assert.deepEqual(catalogTabs.map((tab) => tab.key), [
    'races',
    'classes',
    'backgrounds',
    'spells',
    'equipment',
    'monsters',
    'conditions',
  ])
})

test('translateToken localizes common catalog tokens and preserves unknown values', () => {
  assert.equal(translateToken('Armor'), 'Доспехи')
  assert.equal(translateToken('Martial Weapon'), 'Воинское оружие')
  assert.equal(translateToken('gp'), 'зм')
  assert.equal(translateToken('custom'), 'custom')
  assert.equal(translateToken(undefined), '')
})

test('translateClassSlug localizes class slugs case-insensitively', () => {
  assert.equal(translateClassSlug('FIGHTER'), 'Воин')
  assert.equal(translateClassSlug('wizard'), 'Волшебник')
  assert.equal(translateClassSlug('custom'), 'custom')
  assert.equal(translateClassSlug(undefined), '')
})

test('translateToken localizes monster sizes with original capitalization', () => {
  assert.equal(translateToken('Tiny'), 'Крошечный')
  assert.equal(translateToken('Small'), 'Маленький')
  assert.equal(translateToken('Gargantuan'), 'Громадный')
})
