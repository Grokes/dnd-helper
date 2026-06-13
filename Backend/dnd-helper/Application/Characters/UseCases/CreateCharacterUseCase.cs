using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;

namespace dnd_helper.Application.Characters.UseCases;

public sealed class CreateCharacterUseCase
{
    private readonly AppDbContext dbContext;
    private readonly CharacterCreationService creationService;

    public CreateCharacterUseCase(AppDbContext dbContext, CharacterCreationService creationService)
    {
        this.dbContext = dbContext;
        this.creationService = creationService;
    }

    public async Task<UseCaseResult<CharacterDto>> ExecuteAsync(
        CreateCharacterRequest request,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        var createResult = await creationService.BuildCharacterAsync(request, ownerUserId, cancellationToken);
        if (!createResult.IsSuccess)
        {
            return UseCaseResult<CharacterDto>.ValidationFailed(createResult.Errors!);
        }

        var character = createResult.Character!;
        dbContext.Characters.Add(character);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = character.ToDto();
        return UseCaseResult<CharacterDto>.Created(dto, $"/api/characters/{character.Id}");
    }
}
