namespace MeterForge.Contracts.V1.Reconciliation;

public sealed record ReconcilePaymentRequest(decimal InternalAmount, decimal ProviderAmount, string Currency);
public sealed record ReconciliationResponse(bool Matches, decimal Difference, string Currency, string Reason);
