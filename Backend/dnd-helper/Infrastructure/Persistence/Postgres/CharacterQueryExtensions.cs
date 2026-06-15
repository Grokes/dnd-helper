using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Infrastructure.Persistence.Postgres;

public static class CharacterQueryExtensions
{
    public static IQueryable<CharacterEntity> IncludeCharacterState(this IQueryable<CharacterEntity> query)
    {
        return query
            .AsSplitQuery()
            .Include(character => character.BaseAbilities)
            .Include(character => character.Abilities)
            .Include(character => character.SelectedOptions)
            .Include(character => character.SkillProficiencies)
            .Include(character => character.SavingThrowProficiencies)
            .Include(character => character.KnownSpells)
            .Include(character => character.SpellSlots)
            .Include(character => character.InventoryItems)
            .Include(character => character.CalculationTraceEntries);
    }
}
