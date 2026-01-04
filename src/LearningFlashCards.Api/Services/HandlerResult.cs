using Microsoft.AspNetCore.Http;

namespace LearningFlashCards.Api.Services;

public record HandlerResult<T>(bool IsSuccess, int StatusCode, T? Value, string? Error)
{
    public static HandlerResult<T> Success(T value, int statusCode = StatusCodes.Status200OK) =>
        new(true, statusCode, value, null);

    public static HandlerResult<T> Failure(int statusCode, string error) =>
        new(false, statusCode, default, error);

    public static HandlerResult<T> NotFound(string? error = null) =>
        Failure(StatusCodes.Status404NotFound, error ?? "Not found.");

    public static HandlerResult<T> Forbidden(string? error = null) =>
        Failure(StatusCodes.Status403Forbidden, error ?? "Forbidden.");

    public static HandlerResult<T> BadRequest(string error) =>
        Failure(StatusCodes.Status400BadRequest, error);
}
