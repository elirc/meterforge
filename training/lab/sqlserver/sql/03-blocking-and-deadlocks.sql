-- M3 / INC-002. Part A: who is blocking whom right now (run while `SqlLab soak` is running).
SELECT r.session_id, r.blocking_session_id, r.wait_type, r.wait_time AS wait_ms, r.wait_resource,
       DB_NAME(r.database_id) AS db, SUBSTRING(t.text, 1, 200) AS running_sql
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.session_id <> @@SPID AND (r.blocking_session_id <> 0 OR r.wait_type LIKE 'LCK%');

-- Part B: deadlock graphs. SQL Server records every deadlock in the always-on system_health Extended Events session.
-- Each row is one <deadlock> XML: the victim, each process's input buffer (SQL text), and the resource list
-- (which key/page each process OWNS and which it WAITS for, with lock modes).
SELECT
    xed.value('(@timestamp)[1]', 'datetime2') AS utc_time,
    xed.query('.') AS deadlock_graph
FROM (
    SELECT CAST(event_data AS xml) AS x
    FROM sys.fn_xe_file_target_read_file('system_health*.xel', NULL, NULL, NULL)
    WHERE object_name = 'xml_deadlock_report') AS f
CROSS APPLY f.x.nodes('/event') AS e(xed)
ORDER BY utc_time DESC;

-- Part C: isolation settings. READ_COMMITTED_SNAPSHOT is OFF on boxed SQL Server by default and ON by default in Azure SQL Database.
SELECT name, is_read_committed_snapshot_on, snapshot_isolation_state_desc, compatibility_level
FROM sys.databases WHERE name = DB_NAME();

-- To try RCSI in the lab (needs no other connections; ROLLBACK IMMEDIATE kicks them out):
-- ALTER DATABASE MeterForge SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
