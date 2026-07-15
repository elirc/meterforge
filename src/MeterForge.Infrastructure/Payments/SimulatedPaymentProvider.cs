using MeterForge.Application.Payments;

namespace MeterForge.Infrastructure.Payments;

public sealed class SimulatedPaymentProvider : IPaymentProvider
{
    public Task<ProviderChargeResult> ChargeAsync(ProviderChargeRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ProviderChargeResult(true, $"sim_{request.InvoiceId:N}"));
    }
}
