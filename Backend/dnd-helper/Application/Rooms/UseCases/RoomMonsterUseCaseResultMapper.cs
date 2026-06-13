using dnd_helper.Application.Common.UseCases;

namespace dnd_helper.Application.Rooms.UseCases;

internal static class RoomMonsterUseCaseResultMapper
{
    public static UseCaseResult<T> ToUseCaseResult<T>(this RoomMonsterServiceOutcome<T> outcome)
    {
        if (outcome.IsNotFound)
        {
            return UseCaseResult<T>.NotFound();
        }

        if (!outcome.IsSuccess)
        {
            return UseCaseResult<T>.ValidationFailed(outcome.Errors ?? []);
        }

        return UseCaseResult<T>.Success(outcome.Result!);
    }
}
