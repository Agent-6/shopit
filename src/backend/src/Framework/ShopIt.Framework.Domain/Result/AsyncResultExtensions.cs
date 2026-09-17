namespace ShopIt.Framework.Domain.Result;

public static class AsyncResultExtensions
{
    public static async Task<Result<TNext, TError>> BindAsync<T, TNext, TError>(
        this Result<T, TError> result,
        Func<T, Task<Result<TNext, TError>>> func)
    {
        return result.IsSuccess
            ? await func(result.Value)
            : Result<TNext, TError>.Failure(result.Error);
    }

    public static async Task<Result<TNext, TError>> BindAsync<T, TNext, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task<Result<TNext, TError>>> func)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? await func(result.Value)
            : Result<TNext, TError>.Failure(result.Error);
    }

    public static async Task<Result<TNext, TError>> BindAsync<T, TNext, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Result<TNext, TError>> func)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? func(result.Value)
            : Result<TNext, TError>.Failure(result.Error);
    }

    public static async Task<Result<TNext, TError>> MapAsync<T, TNext, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task<TNext>> func)
    {
        var result = await resultTask;
        if (result.IsFailure)
            return Result<TNext, TError>.Failure(result.Error);

        var nextValue = await func(result.Value);
        return Result<TNext, TError>.Success(nextValue);
    }

    public static async Task<Result<T, TError>> TapAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        Func<T, Task> func)
    {
        var result = await resultTask;
        if (result.IsSuccess)
            await func(result.Value);

        return result;
    }

    public static async Task<TOutput> MatchAsync<T, TError, TOutput>(
        this Task<Result<T, TError>> resultTask,
        Func<T, TOutput> onSuccess,
        Func<TError, TOutput> onFailure)
    {
        var result = await resultTask;
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
    }
}
