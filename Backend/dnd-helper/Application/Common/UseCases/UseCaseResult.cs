namespace dnd_helper.Application.Common.UseCases;

public enum UseCaseResultStatus
{
    Success,
    Created,
    ValidationFailed,
    NotFound,
    Forbidden
}

public sealed record UseCaseResult<T>(
    UseCaseResultStatus Status,
    T? Value = default,
    Dictionary<string, string[]>? Errors = null,
    string? Location = null)
{
    public static UseCaseResult<T> Success(T value) => new(UseCaseResultStatus.Success, value);

    public static UseCaseResult<T> Created(T value, string location) => new(UseCaseResultStatus.Created, value, Location: location);

    public static UseCaseResult<T> ValidationFailed(Dictionary<string, string[]> errors) =>
        new(UseCaseResultStatus.ValidationFailed, Errors: errors);

    public static UseCaseResult<T> NotFound() => new(UseCaseResultStatus.NotFound);

    public static UseCaseResult<T> Forbidden() => new(UseCaseResultStatus.Forbidden);
}
