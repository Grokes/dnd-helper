using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Characters.UseCases;

public sealed class UpdateCharacterUseCase
{
    private readonly AppDbContext dbContext;
    private readonly CharacterCreationService creationService;

    public UpdateCharacterUseCase(AppDbContext dbContext, CharacterCreationService creationService)
    {
        this.dbContext = dbContext;
        this.creationService = creationService;
    }

    public async Task<UseCaseResult<CharacterDto>> ExecuteAsync(
        Guid characterId,
        UpdateCharacterRequest request,
        string userId,
        bool isGameMaster,
        CancellationToken cancellationToken)
    {
        var character = await dbContext.Characters.FirstOrDefaultAsync(item => item.Id == characterId, cancellationToken);
        if (character is null)
        {
            return UseCaseResult<CharacterDto>.NotFound();
        }

        var isOwner = character.OwnerUserId == userId;
        if (!isGameMaster && !isOwner)
        {
            return UseCaseResult<CharacterDto>.Forbidden();
        }

        var normalizedRequest = new CreateCharacterRequest(
            request.Name,
            request.RaceId,
            request.ClassId,
            request.BackgroundId,
            request.Level,
            request.Alignment,
            request.Notes,
            request.BaseAbilities,
            request.BonusAbilitySelections,
            request.RaceSkillSelections,
            request.ClassSkillSelections,
            request.Spells,
            request.Inventory);

        var updateResult = await creationService.BuildCharacterAsync(normalizedRequest, character.OwnerUserId, cancellationToken);
        if (!updateResult.IsSuccess)
        {
            return UseCaseResult<CharacterDto>.ValidationFailed(updateResult.Errors!);
        }

        ApplyRebuiltCharacter(character, updateResult.Character!);
        await dbContext.SaveChangesAsync(cancellationToken);
        return UseCaseResult<CharacterDto>.Success(character.ToDto(canEdit: isGameMaster || isOwner));
    }

    private static void ApplyRebuiltCharacter(CharacterEntity character, CharacterEntity rebuilt)
    {
        var previousCurrentHitPoints = character.MaxHitPoints > 0
            ? character.CurrentHitPoints
            : character.HitPoints;

        character.RaceId = rebuilt.RaceId;
        character.ClassId = rebuilt.ClassId;
        character.BackgroundId = rebuilt.BackgroundId;
        character.Name = rebuilt.Name;
        character.Race = rebuilt.Race;
        character.ClassName = rebuilt.ClassName;
        character.Subclass = rebuilt.Subclass;
        character.Level = rebuilt.Level;
        character.Background = rebuilt.Background;
        character.Alignment = rebuilt.Alignment;
        character.ArmorClass = rebuilt.ArmorClass;
        character.WeaponDamage = rebuilt.WeaponDamage;
        character.HitPoints = rebuilt.HitPoints;
        character.MaxHitPoints = rebuilt.MaxHitPoints <= 0 ? rebuilt.HitPoints : rebuilt.MaxHitPoints;
        character.CurrentHitPoints = Math.Clamp(previousCurrentHitPoints, 0, character.MaxHitPoints);
        character.Speed = rebuilt.Speed;
        character.ProficiencyBonus = rebuilt.ProficiencyBonus;
        character.PassivePerception = rebuilt.PassivePerception;
        character.Notes = rebuilt.Notes;
        character.AbilitiesJson = rebuilt.AbilitiesJson;
        character.BaseAbilitiesJson = rebuilt.BaseAbilitiesJson;
        character.BonusAbilitySelectionsJson = rebuilt.BonusAbilitySelectionsJson;
        character.SkillsJson = rebuilt.SkillsJson;
        character.KnownSpellsJson = rebuilt.KnownSpellsJson;
        character.SpellSlotsJson = rebuilt.SpellSlotsJson;
        character.InventoryJson = rebuilt.InventoryJson;
        character.ComputedSnapshotJson = rebuilt.ComputedSnapshotJson;
        character.CalculationTraceJson = rebuilt.CalculationTraceJson;
        character.UpdatedAtUtc = DateTime.UtcNow;
    }
}
