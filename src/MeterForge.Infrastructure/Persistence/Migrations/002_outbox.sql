create table if not exists outbox_messages (
    tenant_id text not null,
    id uuid not null,
    event_type text not null,
    schema_version integer not null check (schema_version > 0),
    subject text not null,
    occurred_at timestamptz not null,
    payload jsonb not null,
    published_at timestamptz null,
    primary key (tenant_id, id)
);

create index if not exists ix_outbox_unpublished on outbox_messages (occurred_at) where published_at is null;
