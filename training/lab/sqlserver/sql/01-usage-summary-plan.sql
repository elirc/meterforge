-- J2 / M2: read an ACTUAL execution plan for the usage summary query.
-- Run in sqlcmd (-d MeterForge) or Azure Data Studio. With STATISTICS XML ON, each statement returns an extra
-- result set holding the actual plan (showplan XML: save it as .sqlplan and open it in SSMS/ADS to see the graphical plan).
-- The statements below are what UsageReadModel.SummarizeAsync sends (print it with: SqlLab summary tenant-0001 --show-sql).
-- EF.Constant(tenantId) inlines the tenant as a literal; the period stays parameterized.

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
SET STATISTICS XML ON;

-- Small tenant (about 400 rows in total, ~50 in the week).
EXEC sp_executesql N'
-- usage-summary
SELECT [u].[subscription_id] AS [SubscriptionId], [u].[meter] AS [Meter], COALESCE(SUM([u].[quantity]), 0.0) AS [TotalQuantity], COUNT(*) AS [EventCount]
FROM [billing].[usage_events] AS [u]
WHERE [u].[tenant_id] = ''tenant-0001'' AND [u].[occurred_at] >= @from AND [u].[occurred_at] < @to
GROUP BY [u].[subscription_id], [u].[meter]',
N'@from datetimeoffset(7), @to datetimeoffset(7)',
@from = '2026-09-01T00:00:00+00:00', @to = '2026-09-08T00:00:00+00:00';

-- Whale tenant (900k rows, ~115k in the week).
EXEC sp_executesql N'
-- usage-summary
SELECT [u].[subscription_id] AS [SubscriptionId], [u].[meter] AS [Meter], COALESCE(SUM([u].[quantity]), 0.0) AS [TotalQuantity], COUNT(*) AS [EventCount]
FROM [billing].[usage_events] AS [u]
WHERE [u].[tenant_id] = ''tenant-whale'' AND [u].[occurred_at] >= @from AND [u].[occurred_at] < @to
GROUP BY [u].[subscription_id], [u].[meter]',
N'@from datetimeoffset(7), @to datetimeoffset(7)',
@from = '2026-09-01T00:00:00+00:00', @to = '2026-09-08T00:00:00+00:00';

SET STATISTICS XML OFF;
SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;

-- In the Messages output, compare per query: "logical reads" on usage_events, "CPU time" and "elapsed time".
-- In each plan, find: the access operator (Index Seek vs Clustered Index Scan), whether there is a Key Lookup,
-- EstimateRows vs ActualRows on the seek/scan, and the ParameterList (ParameterCompiledValue vs ParameterRuntimeValue).
