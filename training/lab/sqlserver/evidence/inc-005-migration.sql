BEGIN TRANSACTION;
CREATE TABLE [billing].[proration_credits] (
    [tenant_id] varchar(64) NOT NULL,
    [id] uniqueidentifier NOT NULL,
    [subscription_id] uniqueidentifier NOT NULL,
    [amount] decimal(18,2) NOT NULL,
    [currency] char(3) NOT NULL,
    [unused_days] int NOT NULL,
    [days_in_period] int NOT NULL,
    [created_at] datetimeoffset NOT NULL,
    CONSTRAINT [PK_proration_credits] PRIMARY KEY ([tenant_id], [id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260924222443_AddProrationCredits', N'10.0.12');

COMMIT;
GO

