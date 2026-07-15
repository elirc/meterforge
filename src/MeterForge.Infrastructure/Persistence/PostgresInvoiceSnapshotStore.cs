using MeterForge.Application.Persistence;
using Npgsql;

namespace MeterForge.Infrastructure.Persistence;

public sealed class PostgresInvoiceSnapshotStore(NpgsqlDataSource dataSource) : IInvoiceSnapshotStore
{
    public async Task SaveFinalizedAsync(InvoiceSnapshot snapshot, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var invoiceCommand = connection.CreateCommand())
        {
            invoiceCommand.Transaction = transaction;
            invoiceCommand.CommandText = """
                insert into invoices (tenant_id, id, subscription_id, currency, period_start, period_end, finalized_at)
                values (@tenant_id, @id, @subscription_id, @currency, @period_start, @period_end, @finalized_at)
                on conflict (tenant_id, id) do nothing;
                """;
            invoiceCommand.Parameters.AddWithValue("tenant_id", snapshot.TenantId);
            invoiceCommand.Parameters.AddWithValue("id", snapshot.Id);
            invoiceCommand.Parameters.AddWithValue("subscription_id", snapshot.SubscriptionId);
            invoiceCommand.Parameters.AddWithValue("currency", snapshot.Currency);
            invoiceCommand.Parameters.AddWithValue("period_start", snapshot.PeriodStart);
            invoiceCommand.Parameters.AddWithValue("period_end", snapshot.PeriodEnd);
            invoiceCommand.Parameters.AddWithValue("finalized_at", snapshot.FinalizedAt);
            await invoiceCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var line in snapshot.Lines)
        {
            await using var lineCommand = connection.CreateCommand();
            lineCommand.Transaction = transaction;
            lineCommand.CommandText = """
                insert into invoice_lines (tenant_id, invoice_id, id, description, amount, currency, price_version_id, source_reference)
                values (@tenant_id, @invoice_id, @id, @description, @amount, @currency, @price_version_id, @source_reference)
                on conflict (tenant_id, invoice_id, id) do nothing;
                """;
            lineCommand.Parameters.AddWithValue("tenant_id", snapshot.TenantId);
            lineCommand.Parameters.AddWithValue("invoice_id", snapshot.Id);
            lineCommand.Parameters.AddWithValue("id", line.Id);
            lineCommand.Parameters.AddWithValue("description", line.Description);
            lineCommand.Parameters.AddWithValue("amount", line.Amount);
            lineCommand.Parameters.AddWithValue("currency", line.Currency);
            lineCommand.Parameters.AddWithValue("price_version_id", (object?)line.PriceVersionId ?? DBNull.Value);
            lineCommand.Parameters.AddWithValue("source_reference", line.SourceReference);
            await lineCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
