using MeterForge.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MeterForge.Api.Errors;

public sealed class BusinessRuleExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException businessRule) return false;

        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Business rule violation",
                Detail = businessRule.Message,
                Extensions = { ["code"] = businessRule.Code, ["traceId"] = context.TraceIdentifier },
            },
            Exception = exception,
        }).ConfigureAwait(false);
    }
}
