# 02 — Identity, retries and atomic operations

[Course home](README.md) · [Previous: map](01-CODEBASE-MAP.md) · [Next: worked change](03-WORKED-CHANGE.md)

## 1. Entity identity is different from request identity

The stored event has an internal GUID. The caller supplies an external event ID so it can retry without knowing whether the earlier response arrived. The storage key is the pair `(TenantId, ExternalEventId)`. If tenant A and tenant B both send `evt-1`, those are separate namespaces and can create separate events.

Consider a client sending quantity 10, then losing the response. Generating a new external ID on the retry tells the server this is another fact, so two creations would be correct under the contract. The client must keep the same key until that logical action is resolved. Idempotency is a client-and-server contract, not a header that makes arbitrary duplicate requests safe.

```text
tenant-a + evt-1 + quantity 10 -> created E1
tenant-a + evt-1 + quantity 10 -> replay E1
tenant-a + evt-1 + quantity 11 -> conflict; E1 remains 10
tenant-b + evt-1 + quantity 10 -> created E2
tenant-a + evt-2 + quantity 10 -> created E3
```

Predict the result before reading each right-hand side. In the last row, the server cannot infer that the caller accidentally generated a new key. Product requirements must define key generation and retention.

## 2. Validation, normalization and equality are separate decisions

Validation asks whether the input is allowed: nonempty IDs, nonblank fields and a positive quantity. Normalization chooses a canonical representation: the current constructor trims meter/external-event strings and stores time in UTC. Equality decides whether an existing key carries the same business fact.

The service constructs the candidate event before selecting its key. Looking up the raw string `" evt-1 "` and then storing the trimmed string `"evt-1"` uses two different identities within one operation. That was a real defect in the old flow.

The tenant string is treated as an opaque identity and is not automatically lowercased or trimmed here. Do not normalize every string indiscriminately: two case-sensitive external identifiers can be different resources. Define each normalization rule at the appropriate contract boundary and test it.

Replay comparison covers subscription, meter, quantity and occurrence time. Internal GUID is intentionally excluded because each proposed insertion starts with a new GUID. Equal instants expressed using different timezone offsets compare as the same event after normalization. A different subscription under the same external key is a conflict even when the quantity happens to match.

## 3. A thread-safe collection does not make a sequence atomic

The old implementation effectively performed this sequence:

```csharp
var existing = await store.FindByExternalIdAsync(tenant, externalId, token);
if (existing is not null) return Replay(existing);
var candidate = CreateEvent();
await store.AddAsync(candidate, token);
return Created(candidate);
```

`FindByExternalIdAsync` is written in past tense on purpose: it no longer exists. It was removed from `IUsageEventStore` after the repair, because nothing in the production path called it any more and a read-by-natural-key method on the store is an invitation to write exactly the sequence above. Leaving it there would have been a small lie about the contract — see [01](01-CODEBASE-MAP.md).

Each dictionary call could be thread-safe while the sequence was still wrong:

| Step | Request A | Request B |
|---|---|---|
| 1 | Reads: missing | |
| 2 | | Reads: missing |
| 3 | Inserts E1 | |
| 4 | | Insert E2 loses because key exists |
| 5 | Returns created E1 | Returns created E2 even though E2 was never stored |

The old adapter ignored `TryAdd`'s result, so the caller could receive an identity that was not actually in storage. Counting stored rows alone would miss this bug. You must also check the identities and creation/replay flags returned to callers.

The new contract returns the stored winner from one `GetOrAdd` operation. There is one point where the key becomes associated with an event. Every caller then reasons about that same stored event. This is the atomicity guarantee for this one process; it is not a distributed exactly-once guarantee.

## 4. Immutability makes shared state easier to reason about

`UsageEvent` exposes getter-only properties assigned in its constructor. Readers cannot subsequently change quantity through that record. The concurrent dictionary coordinates the key-to-record mapping, and immutability protects the record after publication.

A thread-safe dictionary containing a mutable list would not automatically protect modifications to the list. For example, `dictionary[key].Items.Add(item)` is a separate mutation with its own synchronization needs. Always identify both the container and the contained data when reasoning about thread safety.

## 5. Error codes are part of the application contract

This change uses 201 for a new event, 200 for an identical replay, 409 for a reused key with changed business data, 422 for domain validation failures, and 404 for an unknown tenant-scoped event lookup. A frontend can respond differently to these outcomes: success, confirmation of a retry, a user-visible conflict, a field correction, or a missing resource.

Do not retry every non-200 response automatically. Repeating the same conflicting payload will not repair an idempotency conflict. A client should preserve its draft and show a useful action. Conversely, an uncertain network outcome may justify retrying the same logical action with the same event ID. That is exactly the scenario idempotency supports.

The exception handler maps a specific domain code to 409; other business-rule exceptions keep the existing 422 behavior. Mapping every domain error to 409 would erase the distinction between invalid input and an existing conflicting fact.

## 6. Cancellation has a boundary

The application and store check the cancellation token before their work. A request already cancelled before insertion should not write an event. Cancellation after a successful storage operation cannot retroactively erase that operation. If the response is lost, retrying with the same key is still necessary. “The client cancelled” and “the server definitely did not commit” are not equivalent statements.

## 7. Moving this design to SQL

`ConcurrentDictionary.GetOrAdd` is doing one specific job in this codebase: *insert this row if the key is free, otherwise hand me back the row that is already there* — as one indivisible act. Everything else in this chapter follows from that. So the first question for a durable adapter is which SQL construct has the same property, and the answer is not "a `SELECT` and then an `INSERT`".

### The schema

```sql
CREATE TABLE usage_events (
    id                  uuid        PRIMARY KEY,
    tenant_id           text        NOT NULL,
    subscription_id     uuid        NOT NULL,
    meter               text        NOT NULL,
    quantity            numeric     NOT NULL,
    occurred_at         timestamptz NOT NULL,
    external_event_id   text        NOT NULL,
    CONSTRAINT usage_events_idempotency UNIQUE (tenant_id, external_event_id)
);
```

The `UNIQUE (tenant_id, external_event_id)` constraint **is** the dictionary key. Note that it is a compound key including the tenant, exactly as `ConcurrentDictionary<(string TenantId, string ExternalEventId), UsageEvent>` is — `Ingest_SameExternalIdInDifferentTenants_AcceptsBoth` is the test that would catch you indexing on `external_event_id` alone, and it would fail as a *unique-violation error on a legitimate request*, which is a confusing symptom to debug. Put the tenant first in the constraint and in every query.

### The insert-or-return

```sql
WITH inserted AS (
    INSERT INTO usage_events (id, tenant_id, subscription_id, meter, quantity, occurred_at, external_event_id)
    VALUES (@id, @tenant, @subscription, @meter, @quantity, @occurredAt, @externalEventId)
    ON CONFLICT ON CONSTRAINT usage_events_idempotency DO NOTHING
    RETURNING *
)
SELECT * FROM inserted
UNION ALL
SELECT * FROM usage_events
WHERE tenant_id = @tenant AND external_event_id = @externalEventId
  AND NOT EXISTS (SELECT 1 FROM inserted);
```

That statement is the direct translation of `GetOrAddAsync`, and the shape is worth understanding rather than copying. `ON CONFLICT DO NOTHING RETURNING *` returns **zero rows** when the key was already taken — it does not return the existing row, which is the single most common surprise for people meeting this construct. The `UNION ALL` with the `NOT EXISTS` guard is what supplies the existing row in that case. Both branches run inside one statement, so there is no window between "did it exist?" and "then insert".

An alternative some teams prefer is `ON CONFLICT (tenant_id, external_event_id) DO UPDATE SET id = usage_events.id RETURNING *` — a deliberate no-op update purely to make `RETURNING` fire. It is shorter, and it costs you a write and a row lock on every replay, which on a hot idempotency key is exactly the traffic you least want to serialize. Know both; choose on purpose.

### What changes when the winner comes back from Postgres

| In this course | With a database | What you must now handle |
|---|---|---|
| `GetOrAdd` returns an object you already hold a reference to | The row comes back over the wire and is materialized fresh | Compare **values**, not references. The payload comparison in `UsageIngestionService` already does this, which is why it survives the move unchanged |
| The "winner" is visible to every thread the instant `GetOrAdd` returns | The winner is visible to *this* transaction immediately, and to others only after commit | **Read-your-write** holds inside the transaction; a concurrent reader in another transaction may still see nothing. If the caller's next request goes to another replica before your commit lands, it can legitimately observe a 404 for an id it was just given |
| Nothing can fail between the insert and the return | The statement can fail, the connection can drop, the commit can be uncertain | An error after `COMMIT` was sent but before the acknowledgement arrived is an **uncertain commit**. The safe response is to retry the same idempotent request — which works precisely because the key makes the retry safe. This is the payoff for the whole design |
| A conflict is not an error; `GetOrAdd` just returns the other object | A conflict raises `DbUpdateException` / `PostgresException` with `SqlState == "23505"` unless you handle it in SQL | If you use plain `INSERT` plus a catch, you must match on the **constraint name**, not on "any unique violation" — a second unique index would otherwise be reported as an idempotent replay. Prefer `ON CONFLICT`, which never raises |
| Restart forgets every key | The constraint survives restarts and covers every replica | This is the actual reason to move. The in-memory version cannot deduplicate a retry that arrives after the process has lost its state, and cannot coordinate two API instances at all |
| No side effects | Creation may need an outbox row, an audit row, a metering counter | All of it in **one** transaction with the insert, and none of it repeated on a replay. Getting this wrong is how a replayed request emits a duplicate billing event |

That last row deserves emphasis because it is the one the in-memory design cannot even express. A replay must be silent: same response, no new event, no new audit row. So the adapter cannot simply "insert the outbox row after calling `GetOrAdd`" — it has to know whether the insert actually created anything. With the `WITH inserted AS (...)` form above you already have that information (the `inserted` CTE is empty on a replay), which is another reason to prefer it over the no-op-update variant.

### The limits that do not go away

A unique database constraint coordinates all writers using that database, and an in-process lock does not — but neither one makes the payload comparison unnecessary, and neither one tells the caller *why* their second request was refused. `usage.idempotency_conflict` is still an application-level decision about what "the same event" means, and it is still the thing four of this course's tests defend. Exercise 6 asks you to write this adapter down properly, and Exercise 7 asks you to make the in-memory one better first.

## Explain it in your own words

Write a five-sentence explanation of why a retry can return 200 while the first attempt returned 201 without violating idempotency. Then explain why the existing tenant predicate does not authorize the caller: the route identifies the namespace, but no authenticated principal proves access to it. Keep those two concepts separate in every future CRUD app.
