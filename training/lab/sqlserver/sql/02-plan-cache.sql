-- M2 / INC-001: which cached plans exist for the usage summary, how often each was reused, and what they cost.
-- EF's TagWith("usage-summary") puts the comment "-- usage-summary" at the top of the SQL text, so we can find it.
SELECT TOP (20)
    qs.execution_count,
    qs.total_logical_reads / qs.execution_count AS avg_logical_reads,
    qs.total_worker_time / qs.execution_count / 1000 AS avg_cpu_ms,
    qs.total_elapsed_time / qs.execution_count / 1000 AS avg_elapsed_ms,
    qs.creation_time,
    SUBSTRING(st.text, 1, 400) AS query_text,
    qp.query_plan
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
WHERE st.text LIKE N'%usage-summary%' AND st.text NOT LIKE N'%dm_exec_query_stats%'
ORDER BY qs.total_worker_time DESC;

-- Single-use plans in the cache (the "plan cache bloat" a DBA complains about).
SELECT objtype, COUNT(*) AS plans, SUM(CAST(size_in_bytes AS bigint)) / 1024 / 1024 AS size_mb
FROM sys.dm_exec_cached_plans
WHERE usecounts = 1
GROUP BY objtype;

-- The sniffed value is in the plan XML: open query_plan and search for ParameterCompiledValue.
-- Query Store (on by default for new databases on SQL Server 2022 and Azure SQL) keeps plans across restarts:
-- SELECT * FROM sys.query_store_plan p JOIN sys.query_store_query q ON q.query_id = p.query_id
-- JOIN sys.query_store_query_text t ON t.query_text_id = q.query_text_id WHERE t.query_sql_text LIKE N'%usage-summary%';
