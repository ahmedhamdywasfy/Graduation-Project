using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace SmartHorse.API.Common;

/// <summary>
/// Wraps every successful controller action's JSON payload in <see cref="ApiResponse{T}"/>
/// (Sprint 2 §8). Runs as a global result filter so individual controllers/actions
/// don't need to remember to wrap their own results. Skips non-JSON results
/// (files, redirects) and results that are already an <see cref="ApiResponse{T}"/>
/// (defensive — none currently return one directly, but avoids double-wrapping if
/// a future action does).
/// </summary>
public class ApiResponseWrapperFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult
            && objectResult.Value is not null
            && !IsAlreadyWrapped(objectResult.Value)
            && !IsProblemDetails(objectResult.Value))
        {
            var wrapperType = typeof(ApiResponse<>).MakeGenericType(objectResult.Value.GetType());
            var factory = wrapperType.GetMethod(nameof(ApiResponse<object>.Ok))
                ?? throw new InvalidOperationException("ApiResponse<T>.Ok factory method not found.");

            objectResult.Value = factory.Invoke(null, new[] { objectResult.Value });
        }

        await next();
    }

    private static bool IsAlreadyWrapped(object value) =>
        value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>);

    private static bool IsProblemDetails(object value) =>
        value is Microsoft.AspNetCore.Mvc.ProblemDetails;
}
