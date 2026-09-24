-- M2 / S2: indexes on billing.usage_events, their size, how they are used, and how old their statistics are.
SELECT i.name AS index_name, i.type_desc, i.is_unique, i.has_filter, i.filter_definition,
       ps.row_count, ps.used_page_count * 8 / 1024 AS used_mb,
       us.user_seeks, us.user_scans, us.user_lookups, us.user_updates
FROM sys.indexes i
JOIN sys.dm_db_partition_stats ps ON ps.object_id = i.object_id AND ps.index_id = i.index_id
LEFT JOIN sys.dm_db_index_usage_stats us ON us.object_id = i.object_id AND us.index_id = i.index_id AND us.database_id = DB_ID()
WHERE i.object_id = OBJECT_ID('billing.usage_events')
ORDER BY i.index_id;

SELECT s.name AS stats_name, sp.last_updated, sp.rows, sp.rows_sampled, sp.modification_counter
FROM sys.stats s
CROSS APPLY sys.dm_db_stats_properties(s.object_id, s.stats_id) sp
WHERE s.object_id = OBJECT_ID('billing.usage_events');

-- Fragmentation (LIMITED mode is cheap; do not run DETAILED on a 100M-row table in business hours).
SELECT i.name, ips.avg_fragmentation_in_percent, ips.page_count
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('billing.usage_events'), NULL, NULL, 'LIMITED') ips
JOIN sys.indexes i ON i.object_id = ips.object_id AND i.index_id = ips.index_id;

-- Histogram for the tenant column: how skewed is tenant_id? (the whale is one step with ~900k rows)
DBCC SHOW_STATISTICS ('billing.usage_events', 'ix_usage_events_tenant_occurred') WITH HISTOGRAM;
