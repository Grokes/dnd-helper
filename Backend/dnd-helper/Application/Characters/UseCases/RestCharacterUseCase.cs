using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Characters.UseCases;

public sealed class RestCharacterUseCase
{
    private readonly AppDbContext dbContext;
    private readonly CharacterRestService restService;

    public RestCharacterUseCase(AppDbContext dbContext, CharacterRestService restService)
    {
        this.dbContext = dbContext;
        this.restService = restService;
    }

    public async Task<UseCaseResult<CharacterRestResultDto>> ExecuteAsync(
        Guid characterId,
        CharacterRestRequest request,
        string userId,
        bool isGameMaster,
        CancellationToken cancellationToken)
    {
        var character = await dbContext.Characters
            .IncludeCharacterState()
            .FirstOrDefaultAsync(item => item.Id == characterId, cancellationToken);
        if (character is null)
        {
            return UseCaseResult<CharacterRestResultDto>.NotFound();
        }

        var isOwner = character.OwnerUserId == userId;
        if (!isGameMaster && !isOwner)
        {
            return UseCaseResult<CharacterRestResultDto>.Forbidden();
        }

        var restResult = restService.ApplyRest(character, request, canEdit: isOwner || isGameMaster);
        if (!restResult.IsSuccess)
        {
            return UseCaseResult<CharacterRestResultDto>.ValidationFailed(restResult.Errors!);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return UseCaseResult<CharacterRestResultDto>.Success(restResult.Result!);
    }
}
