create table if not exists invoices (
    tenant_id text not null,
    id uuid not null,
    subscription_id uuid not null,
    currency char(3) not null,
    period_start date not null,
    period_end date not null,
    finalized_at timestamptz not null,
    primary key (tenant_id, id),
    check (period_end > period_start)
);

create table if not exists invoice_lines (
    tenant_id text not null,
    invoice_id uuid not null,
    id uuid not null,
    description text not null,
    amount numeric(19, 2) not null check (amount >= 0),
    currency char(3) not null,
    price_version_id uuid null,
    source_reference text not null,
    primary key (tenant_id, invoice_id, id),
    foreign key (tenant_id, invoice_id) references invoices (tenant_id, id)
);
