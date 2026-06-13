using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Characters.UseCases;

public sealed class CastCharacterSpellUseCase
{
    private readonly AppDbContext dbContext;
    private readonly CharacterSpellService spellService;

    public CastCharacterSpellUseCase(AppDbContext dbContext, CharacterSpellService spellService)
    {
        this.dbContext = dbContext;
        this.spellService = spellService;
    }

    public async Task<UseCaseResult<CharacterCastSpellResultDto>> ExecuteAsync(
        Guid characterId,
        CharacterCastSpellRequest request,
        string userId,
        bool isGameMaster,
        CancellationToken cancellationToken)
    {
        var character = await dbContext.Characters.FirstOrDefaultAsync(item => item.Id == characterId, cancellationToken);
        if (character is null)
        {
            return UseCaseResult<CharacterCastSpellResultDto>.NotFound();
        }

        var isOwner = character.OwnerUserId == userId;
        if (!isGameMaster && !isOwner)
        {
            return UseCaseResult<CharacterCastSpellResultDto>.Forbidden();
        }

        var spellResult = await spellService.CastAsync(character, request, cancellationToken);
        if (spellResult.IsNotFound)
        {
            return UseCaseResult<CharacterCastSpellResultDto>.NotFound();
        }

        if (!spellResult.IsSuccess)
        {
            return UseCaseResult<CharacterCastSpellResultDto>.ValidationFailed(spellResult.Errors!);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return UseCaseResult<CharacterCastSpellResultDto>.Success(spellResult.Result!);
    }
}
