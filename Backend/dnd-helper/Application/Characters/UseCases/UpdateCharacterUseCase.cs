using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace dnd_helper.Application.Characters.UseCases;

public sealed class UpdateCharacterUseCase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
        var character = await dbContext.Characters
            .FirstOrDefaultAsync(item => item.Id == characterId, cancellationToken);
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

        var previousSpentSpellSlots = await ReadSpentSpellSlotsAsync(character, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await DeleteNormalizedCharacterStateAsync(character.Id, cancellationToken);

        ApplyRebuiltCharacter(character, updateResult.Character!, previousSpentSpellSlots);
        MarkNormalizedCharacterStateAsAdded(character);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var updatedCharacter = await dbContext.Characters
            .AsNoTracking()
            .IncludeCharacterState()
            .FirstAsync(item => item.Id == characterId, cancellationToken);
        return UseCaseResult<CharacterDto>.Success(updatedCharacter.ToDto(canEdit: isGameMaster || isOwner));
    }

    private void MarkNormalizedCharacterStateAsAdded(CharacterEntity character)
    {
        foreach (var item in character.BaseAbilities)
        {
            dbContext.Entry(item).State = EntityState.Added;
        }

        foreach (var item in character.Abilities)
        {
            dbContext.Entry(item).State = EntityState.Added;
        }

        foreach (var item in character.SelectedOptions)
        {
            dbContext.Entry(item).State = EntityState.Added;
        }

        foreach (var item in character.SkillProficiencies)
        {
            dbContext.Entry(item).State = EntityState.Added;
        }

        foreach (var item in character.SavingThrowProficiencies)
        {
            dbContext.Entry(item).State = EntityState.Added;
        }

        foreach (var item in character.KnownSpells)
        {
            dbContext.Entry(item).State = EntityState.Added;
        }

        foreach (var item in character.SpellSlots)
        {
            dbContext.Entry(item).State = EntityState.Added;
        }

        foreach (var item in character.InventoryItems)
        {
            dbContext.Entry(item).State = EntityState.Added;
        }

        foreach (var item in character.CalculationTraceEntries)
        {
            dbContext.Entry(item).State = EntityState.Added;
        }
    }

    private async Task DeleteNormalizedCharacterStateAsync(Guid characterId, CancellationToken cancellationToken)
    {
        await dbContext.CharacterBaseAbilities.Where(item => item.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.CharacterAbilities.Where(item => item.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.CharacterSelectedOptions.Where(item => item.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.CharacterSkillProficiencies.Where(item => item.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.CharacterSavingThrowProficiencies.Where(item => item.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.CharacterKnownSpells.Where(item => item.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.CharacterSpellSlots.Where(item => item.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.CharacterInventoryItems.Where(item => item.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.CharacterCalculationTraceEntries.Where(item => item.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
    }

    private static void ApplyRebuiltCharacter(
        CharacterEntity character,
        CharacterEntity rebuilt,
        IReadOnlyDictionary<int, int> previousSpentSpellSlots)
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
        character.HitDie = rebuilt.HitDie;
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
        character.ReplaceNormalizedStateFrom(rebuilt);
        foreach (var slot in character.SpellSlots)
        {
            slot.SpentSlots = Math.Clamp(previousSpentSpellSlots.GetValueOrDefault(slot.SpellLevel), 0, slot.MaxSlots);
        }

        character.SyncSpellSlotSnapshotFromRows();
        character.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task<Dictionary<int, int>> ReadSpentSpellSlotsAsync(CharacterEntity character, CancellationToken cancellationToken)
    {
        var spentSlots = await dbContext.CharacterSpellSlots
            .AsNoTracking()
            .Where(item => item.CharacterId == character.Id && item.SpentSlots > 0)
            .ToDictionaryAsync(item => item.SpellLevel, item => item.SpentSlots, cancellationToken);

        if (spentSlots.Count > 0)
        {
            return spentSlots;
        }

        if (string.IsNullOrWhiteSpace(character.SpentSpellSlotsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(character.SpentSpellSlotsJson, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
