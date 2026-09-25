IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF SCHEMA_ID(N'billing') IS NULL EXEC(N'CREATE SCHEMA [billing];');

CREATE TABLE [billing].[invoices] (
    [tenant_id] varchar(64) NOT NULL,
    [id] uniqueidentifier NOT NULL,
    [subscription_id] uniqueidentifier NOT NULL,
    [currency] char(3) NOT NULL,
    [period_start] date NOT NULL,
    [period_end] date NOT NULL,
    [finalized_at] datetimeoffset NOT NULL,
    CONSTRAINT [PK_invoices] PRIMARY KEY ([tenant_id], [id]),
    CONSTRAINT [ck_invoices_period] CHECK ([period_end] > [period_start])
);

CREATE TABLE [billing].[outbox_messages] (
    [tenant_id] varchar(64) NOT NULL,
    [id] uniqueidentifier NOT NULL,
    [event_type] varchar(100) NOT NULL,
    [schema_version] int NOT NULL,
    [subject] nvarchar(200) NOT NULL,
    [occurred_at] datetimeoffset NOT NULL,
    [payload] nvarchar(max) NOT NULL,
    [published_at] datetimeoffset NULL,
    CONSTRAINT [PK_outbox_messages] PRIMARY KEY ([tenant_id], [id]),
    CONSTRAINT [ck_outbox_payload_json] CHECK (ISJSON([payload]) = 1)
);

CREATE TABLE [billing].[usage_daily_rollups] (
    [tenant_id] varchar(64) NOT NULL,
    [subscription_id] uniqueidentifier NOT NULL,
    [meter] varchar(64) NOT NULL,
    [usage_date] date NOT NULL,
    [total_quantity] decimal(19,4) NOT NULL,
    [event_count] int NOT NULL,
    [updated_at] datetimeoffset NOT NULL,
    CONSTRAINT [PK_usage_daily_rollups] PRIMARY KEY ([tenant_id], [subscription_id], [meter], [usage_date])
);

CREATE TABLE [billing].[usage_events] (
    [seq] bigint NOT NULL IDENTITY,
    [id] uniqueidentifier NOT NULL,
    [tenant_id] varchar(64) NOT NULL,
    [subscription_id] uniqueidentifier NOT NULL,
    [meter] varchar(64) NOT NULL,
    [quantity] decimal(19,4) NOT NULL,
    [occurred_at] datetimeoffset NOT NULL,
    [external_event_id] varchar(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
    [ingested_at] datetimeoffset NOT NULL,
    [rolled_up] bit NOT NULL,
    CONSTRAINT [PK_usage_events] PRIMARY KEY CLUSTERED ([seq])
);

CREATE TABLE [billing].[invoice_lines] (
    [tenant_id] varchar(64) NOT NULL,
    [invoice_id] uniqueidentifier NOT NULL,
    [id] uniqueidentifier NOT NULL,
    [description] nvarchar(400) NOT NULL,
    [amount] decimal(19,2) NOT NULL,
    [currency] char(3) NOT NULL,
    [price_version_id] uniqueidentifier NULL,
    [source_reference] varchar(200) NOT NULL,
    CONSTRAINT [PK_invoice_lines] PRIMARY KEY ([tenant_id], [invoice_id], [id]),
    CONSTRAINT [ck_invoice_lines_amount] CHECK ([amount] >= 0),
    CONSTRAINT [FK_invoice_lines_invoices_tenant_id_invoice_id] FOREIGN KEY ([tenant_id], [invoice_id]) REFERENCES [billing].[invoices] ([tenant_id], [id]) ON DELETE CASCADE
);

CREATE INDEX [ix_outbox_unpublished] ON [billing].[outbox_messages] ([occurred_at]) WHERE [published_at] IS NULL;

CREATE INDEX [ix_usage_events_tenant_occurred] ON [billing].[usage_events] ([tenant_id], [occurred_at]);

CREATE INDEX [ix_usage_events_unrolled] ON [billing].[usage_events] ([occurred_at]) WHERE [rolled_up] = 0;

CREATE UNIQUE INDEX [ux_usage_events_id] ON [billing].[usage_events] ([id]);

CREATE UNIQUE INDEX [ux_usage_events_tenant_external] ON [billing].[usage_events] ([tenant_id], [external_event_id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260924200100_InitialSqlServer', N'10.0.12');

COMMIT;
GO

