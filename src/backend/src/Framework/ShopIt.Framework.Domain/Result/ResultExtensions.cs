namespace ShopIt.Framework.Domain.Result;

public static class ResultExtensions
{
    // Chain operations that return another Result
    public static Result<TNext, TError> Bind<T, TNext, TError>(
        this Result<T, TError> result,
        Func<T, Result<TNext, TError>> func)
    {
        return result.IsSuccess ? func(result.Value) : result.Error;
    }

    public static Result<TNext, TError> Bind<TNext, TError>(
        this Result<Unit, TError> result,
        Func<Result<TNext, TError>> func)
    {
        new Unit();
        return result.IsSuccess ? func() : result.Error;
    }

    // Transform Success value directly
    public static Result<TNext, TError> Map<T, TNext, TError>(
        this Result<T, TError> result,
        Func<T, TNext> func)
    {
        return result.IsSuccess ? func(result.Value) : result.Error;
    }

    // Execute side-effect on Success without altering track value
    public static Result<T, TError> Tap<T, TError>(
        this Result<T, TError> result,
        Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);

        return result;
    }

    public static Result<Unit, TError> Tap<TError>(
        this Result<Unit, TError> result,
        Action action)
    {
        if (result.IsSuccess)
            action();

        return result;
    }

    // Unwrap final output
    public static TOutput Match<T, TError, TOutput>(
        this Result<T, TError> result,
        Func<T, TOutput> onSuccess,
        Func<TError, TOutput> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
    }

    public static TOutput Match<TError, TOutput>(
        this Result<Unit, TError> result,
        Func<TOutput> onSuccess,
        Func<TError, TOutput> onFailure)
    {
        return result.IsSuccess ? onSuccess() : onFailure(result.Error);
    }
}
