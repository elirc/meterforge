namespace MeterForge.Contracts.V1.Payments;

public sealed record CollectPaymentRequest(Guid InvoiceId, decimal Amount, string Currency);
public sealed record PaymentResponse(Guid Id, Guid InvoiceId, string Status, int AttemptNumber, string ProviderReference);
