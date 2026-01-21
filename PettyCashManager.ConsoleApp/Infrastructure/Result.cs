namespace PettyCashManager.Infrastructure;

public sealed class Result<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public List<string> Errors { get; init; } = new();

    public static Result<T> Ok(T data, string message = "OK")
        => new() { Success = true, Data = data, Message = message };

    public static Result<T> Fail(string message, params string[] errors)
        => new() { Success = false, Message = message, Errors = errors.ToList() };
}
