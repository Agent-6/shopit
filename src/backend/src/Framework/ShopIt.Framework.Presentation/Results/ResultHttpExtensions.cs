using Microsoft.AspNetCore.Http;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Framework.Presentation.Results;

/// <summary>
/// Provides extension methods for converting domain errors and Results into HTTP IResult responses.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Converts a domain Error into an appropriate HTTP Problem Details response.
    /// </summary>
    public static IResult ToProblem(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };

        var title = error.Type switch
        {
            ErrorType.NotFound => "Not Found",
            ErrorType.Validation => "Validation Failure",
            ErrorType.Conflict => "Conflict",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.Unexpected => "An unexpected error occurred",
            _ => "Bad Request"
        };

        var extensions = new Dictionary<string, object?>
        {
            ["code"] = error.Code
        };

        if (error is ValidationError validationError && validationError.Errors.Count > 0)
        {
            extensions["errors"] = validationError.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray());
        }

        if (error.Metadata is not null)
        {
            foreach (var (key, value) in error.Metadata)
            {
                extensions[key] = value;
            }
        }

        return Microsoft.AspNetCore.Http.Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Description,
            extensions: extensions);
    }

    /// <summary>
    /// Converts a void Result (Result&lt;Unit, TError&gt;) to an HTTP IResult (200 OK by default, or Problem Details on failure).
    /// </summary>
    public static IResult ToHttpResult<TError>(
        this Result<Unit, TError> result,
        int successStatusCode = StatusCodes.Status200OK,
        Func<TError, IResult>? onFailure = null)
    {
        if (result.IsFailure)
        {
            if (onFailure is not null)
                return onFailure(result.Error);

            return result.Error is Error domainError
                ? domainError.ToProblem()
                : Microsoft.AspNetCore.Http.Results.Problem(detail: result.Error?.ToString(), statusCode: StatusCodes.Status400BadRequest);
        }

        return successStatusCode switch
        {
            StatusCodes.Status204NoContent => Microsoft.AspNetCore.Http.Results.NoContent(),
            _ => Microsoft.AspNetCore.Http.Results.Ok()
        };
    }

    /// <summary>
    /// Converts a generic Result&lt;TValue, TError&gt; to an HTTP IResult (200 OK by default, or Problem Details on failure).
    /// </summary>
    public static IResult ToHttpResult<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, IResult>? onSuccess = null,
        Func<TError, IResult>? onFailure = null)
    {
        if (result.IsFailure)
        {
            if (onFailure is not null)
                return onFailure(result.Error);

            return result.Error is Error domainError
                ? domainError.ToProblem()
                : Microsoft.AspNetCore.Http.Results.Problem(detail: result.Error?.ToString(), statusCode: StatusCodes.Status400BadRequest);
        }

        return onSuccess is not null
            ? onSuccess(result.Value)
            : Microsoft.AspNetCore.Http.Results.Ok(result.Value);
    }
}
