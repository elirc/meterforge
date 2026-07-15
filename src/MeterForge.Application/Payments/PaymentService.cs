using MeterForge.Domain.Common;
using MeterForge.Domain.Payments;

namespace MeterForge.Application.Payments;

public sealed record ProviderChargeRequest(string IdempotencyKey, Money Amount, Guid InvoiceId);
public sealed record ProviderChargeResult(bool Succeeded, string? ProviderReference);

public interface IPaymentProvider
{
    Task<ProviderChargeResult> ChargeAsync(ProviderChargeRequest request, CancellationToken cancellationToken);
}

public sealed class PaymentService(IPaymentProvider provider)
{
    public async Task<PaymentAttempt> CollectAsync(string tenantId, Guid invoiceId, Money amount, string idempotencyKey, CancellationToken cancellationToken)
    {
        var attempt = new PaymentAttempt(Guid.NewGuid(), tenantId, invoiceId, amount, idempotencyKey);
        await SendAsync(attempt, cancellationToken).ConfigureAwait(false);
        return attempt;
    }

    public async Task RetryAsync(PaymentAttempt attempt, CancellationToken cancellationToken)
    {
        attempt.Retry();
        await SendAsync(attempt, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendAsync(PaymentAttempt attempt, CancellationToken cancellationToken)
    {
        var result = await provider.ChargeAsync(new ProviderChargeRequest(
            attempt.ProviderIdempotencyKey, attempt.Amount, attempt.InvoiceId), cancellationToken).ConfigureAwait(false);
        if (result.Succeeded) attempt.Succeed(result.ProviderReference!); else attempt.Fail();
    }
}
