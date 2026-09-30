using MeterForge.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MeterForge.Api.Errors;

public sealed class BusinessRuleExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException businessRule) return false;

        var status = businessRule.Code == "usage.idempotency_conflict"
            ? StatusCodes.Status409Conflict : StatusCodes.Status422UnprocessableEntity;
        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = "Business rule violation",
                Detail = businessRule.Message,
                Extensions = { ["code"] = businessRule.Code, ["traceId"] = context.TraceIdentifier },
            },
            Exception = exception,
        }).ConfigureAwait(false);
    }
}
