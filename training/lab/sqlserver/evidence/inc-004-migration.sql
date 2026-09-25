BEGIN TRANSACTION;
DROP INDEX [ux_usage_events_tenant_external] ON [billing].[usage_events];
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[billing].[usage_events]') AND [c].[name] = N'external_event_id');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [billing].[usage_events] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [billing].[usage_events] ALTER COLUMN [external_event_id] varchar(128) NOT NULL;
CREATE UNIQUE INDEX [ux_usage_events_tenant_external] ON [billing].[usage_events] ([tenant_id], [external_event_id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260924221417_UseDatabaseDefaultCollation', N'10.0.12');

COMMIT;
GO

