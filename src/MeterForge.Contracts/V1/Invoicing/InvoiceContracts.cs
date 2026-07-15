namespace MeterForge.Contracts.V1.Invoicing;

public sealed record InvoiceLineInput(string Description, decimal Amount, Guid? PriceVersionId, string SourceReference);
public sealed record FinalizeInvoiceRequest(Guid SubscriptionId, string Currency, DateOnly PeriodStart, DateOnly PeriodEnd, IReadOnlyCollection<InvoiceLineInput> Lines);
public sealed record FinalizedInvoiceResponse(Guid Id, string Status, decimal Total, string Currency, DateTimeOffset FinalizedAt, int LineCount);
