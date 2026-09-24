namespace Application.Common.Results;

public class Result
{
    protected bool _success;
    protected string? _error;

    public bool Success => _success;
    public bool Failed => !_success;
    public string? ErrorMessage => _error;

    protected Result(bool success, string? error)
    {
        _success = success;
        _error = error;
    }

    public static Result Ok() => new Result(true, null);

    public static Result Fail(string error) => new Result(false, error);

    public static Result<T> Ok<T>(T data)
        => new Result<T>(true, data, null);
}

public class Result<T> : Result
{
    protected T? _data;

    public T? Data => _success ? _data : throw new Exception("Result is failed");

    public Result(bool success, T? data, string? error) : base(success, error)
    {
        _data = data;
    }

    public static Result<T> Ok(T data)
        => new Result<T>(true, data, null);

    public new static Result<T> Fail(string error)
        => new Result<T>(false, default, error);
}