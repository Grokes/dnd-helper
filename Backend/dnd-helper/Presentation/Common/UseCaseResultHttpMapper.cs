using dnd_helper.Application.Common.UseCases;

namespace dnd_helper.Presentation.Common;

public static class UseCaseResultHttpMapper
{
    public static IResult ToHttpResult<T>(this UseCaseResult<T> result)
    {
        return result.Status switch
        {
            UseCaseResultStatus.Success => Results.Ok(result.Value),
            UseCaseResultStatus.Created => Results.Created(result.Location ?? string.Empty, result.Value),
            UseCaseResultStatus.ValidationFailed => Results.ValidationProblem(result.Errors ?? []),
            UseCaseResultStatus.NotFound => Results.NotFound(),
            UseCaseResultStatus.Forbidden => Results.Forbid(),
            UseCaseResultStatus.NoContent => Results.NoContent(),
            _ => Results.Problem("Unexpected use case result.")
        };
    }
}
