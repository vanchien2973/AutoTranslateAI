using Shared.Results;

namespace Shared.Extensions;

public static class ResultExtensions
{
    // ─────────────────────────────────────────────────────────────
    // BIND — nối bước có thể fail. Fail giữa chuỗi -> short-circuit.
    // ─────────────────────────────────────────────────────────────

    // sync result, sync func
    public static Result<TOut> Bind<TIn, TOut>(
        this Result<TIn> result, Func<TIn, Result<TOut>> next) =>
        result.IsSuccess ? next(result.Value) : Result.Failure<TOut>(result.Error);

    // sync result, async func
    public static async Task<Result<TOut>> Bind<TIn, TOut>(
        this Result<TIn> result, Func<TIn, Task<Result<TOut>>> next) =>
        result.IsSuccess ? await next(result.Value) : Result.Failure<TOut>(result.Error);

    // async result, sync func
    public static async Task<Result<TOut>> Bind<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, Result<TOut>> next)
    {
        var result = await resultTask;
        return result.IsSuccess ? next(result.Value) : Result.Failure<TOut>(result.Error);
    }

    // async result, async func
    public static async Task<Result<TOut>> Bind<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> next)
    {
        var result = await resultTask;
        return result.IsSuccess ? await next(result.Value) : Result.Failure<TOut>(result.Error);
    }

    // ─────────────────────────────────────────────────────────────
    // MAP — biến đổi value thuần (không fail). Fail -> giữ nguyên Error.
    // ─────────────────────────────────────────────────────────────

    // sync result, sync func
    public static Result<TOut> Map<TIn, TOut>(
        this Result<TIn> result, Func<TIn, TOut> mapper) =>
        result.IsSuccess ? Result.Success(mapper(result.Value)) : Result.Failure<TOut>(result.Error);

    // async result, sync func
    public static async Task<Result<TOut>> Map<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, TOut> mapper)
    {
        var result = await resultTask;
        return result.IsSuccess ? Result.Success(mapper(result.Value)) : Result.Failure<TOut>(result.Error);
    }

    // async result, async func
    public static async Task<Result<TOut>> Map<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, Task<TOut>> mapper)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? Result.Success(await mapper(result.Value))
            : Result.Failure<TOut>(result.Error);
    }

    // ─────────────────────────────────────────────────────────────
    // TAP — chạy side-effect (log, lưu DB, update progress) rồi trả
    // nguyên result. Chỉ chạy khi success.
    // ─────────────────────────────────────────────────────────────

    // sync result, sync action
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess) action(result.Value);
        return result;
    }

    // sync result, async action
    public static async Task<Result<T>> Tap<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess) await action(result.Value);
        return result;
    }

    // async result, sync action
    public static async Task<Result<T>> Tap<T>(this Task<Result<T>> resultTask, Action<T> action)
    {
        var result = await resultTask;
        if (result.IsSuccess) action(result.Value);
        return result;
    }

    // async result, async action
    public static async Task<Result<T>> Tap<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
    {
        var result = await resultTask;
        if (result.IsSuccess) await action(result.Value);
        return result;
    }

    // ─────────────────────────────────────────────────────────────
    // TapError — side-effect khi FAIL (log lỗi, ghi JobSteps.ErrorMessage)
    // ─────────────────────────────────────────────────────────────

    public static async Task<Result<T>> TapError<T>(
        this Task<Result<T>> resultTask, Func<Error, Task> action)
    {
        var result = await resultTask;
        if (result.IsFailure) await action(result.Error);
        return result;
    }

    // ─────────────────────────────────────────────────────────────
    // Ensure — kiểm tra điều kiện trên value, fail nếu không thỏa
    // ─────────────────────────────────────────────────────────────

    public static Result<T> Ensure<T>(
        this Result<T> result, Func<T, bool> predicate, Error error) =>
        result.IsFailure ? result
        : predicate(result.Value) ? result
        : Result.Failure<T>(error);
}
