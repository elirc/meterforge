# 06 — Hints, solution reasoning and review criteria

[Course home](README.md) · [Practice first](05-PRACTICE.md) · [Trace lab](07-TRACE-LAB.md)

Use this after attempting the exercises. These are design guides, not a claim that the proposed features have been implemented. More than one design can be correct if its behavior, tradeoffs and evidence are explicit.

## Solution reasoning: identity and replay

An identical replay returns the first stored GUID and sets `IsReplay` on that ingestion response. A subsequent GET uses the same response shape but sets that operation flag to false. You could propose separate DTOs for ingestion outcomes and plain resource reads; that would make the distinction clearer, but it would be a public-contract change requiring client and test review.

For the existing implementation, the key must be chosen from the normalized candidate. Equality must exclude the generated internal GUID and include every business field that determines the meaning of the fact. If a future field changes billing behavior, adding it to the DTO alone is insufficient: decide how it affects validation, replay equivalence and compatibility with earlier stored events.

## Solution reasoning: bounded identifiers

A good solution adds a named policy close to `UsageEvent`, checks it after the documented normalization step, and uses a stable domain code. Test the boundary values without hiding them behind a random-data generator. A test called `AcceptsMaximumLengthExternalId` should show what the chosen maximum means.

Be careful when adding a limit to an existing system. Old stored IDs may exceed it. Decide whether reads remain available and whether old identical retries are accepted. The memory lab makes it easy to reset fixtures, but a production migration cannot pretend historical data never existed. Include a compatibility note even if you choose a simpler policy for the exercise.

## Solution reasoning: client state

A useful client has at least these conceptual states:

```text
editing -> submitting -> accepted
                  \-> validation/conflict: retain draft, explain correction
                  \-> uncertain network result: retry same logical action/key

accepted -> deliberate new action -> new key
```

Disabling a button prevents some duplicate clicks, but it does not coordinate multiple tabs, clients or network retries. The server's idempotency policy remains necessary. Likewise, a server returning 409 is not permission for the client to silently invent a new key and resubmit changed data. Ask the user to confirm a new action or correct the conflicting intent.

For tests, make the first simulated request commit successfully and then throw before its response reaches the client. This is stronger than a mock that throws before doing any work: it exercises the uncertainty that motivates the design.

## Solution reasoning: deterministic lists

Choose an explicit ordering such as occurrence time plus internal GUID. Timestamp alone is not unique; dictionary enumeration is not a pagination contract. If you expose offset pagination for a small demo, document that concurrent inserts can move page boundaries. If you expose cursors, encode the complete ordering position and validate decoded values rather than trusting arbitrary client input.

Read the tenant predicate before the sort. The correct result is the ordered page of the tenant's records, not the tenant's records from a globally selected page. The latter can leak global activity through empty pages/counts and produces incorrect navigation even without exposing another tenant's full record.

## Solution reasoning: parent ownership

A subscription GUID being syntactically valid does not prove the subscription exists or belongs to the tenant. Resolve the relationship through the application/repository boundary before creating a new event. Write a failing test using a subscription actually owned by tenant B and a request routed through tenant A.

Replay policy needs separate thought. If an event was valid when created but its subscription is later cancelled, must an identical retry still return the original? Frequently yes, because the retry is asking about a historical action, but your product specification must decide. Do not silently change that policy while adding a parent lookup.

## Solution reasoning: a durable adapter

The following is proposed SQL structure, not an existing MeterForge migration:

```sql
CREATE TABLE usage_events (
  id uuid PRIMARY KEY,
  tenant_id text NOT NULL,
  external_event_id text NOT NULL,
  subscription_id uuid NOT NULL,
  meter text NOT NULL,
  quantity numeric NOT NULL CHECK (quantity > 0),
  occurred_at timestamptz NOT NULL,
  UNIQUE (tenant_id, external_event_id)
);
```

The unique constraint is part of the correctness mechanism. An insert that loses the conflict must return/read the winner rather than report its unused candidate ID. If using `INSERT ... ON CONFLICT DO NOTHING RETURNING ...`, the no-row case requires careful handling of visibility and concurrent commits. Do not assume an arbitrary combined query or isolation level automatically makes the winner visible; test two real connections.

If creation also produces a billing event in an outbox, both writes must commit in one transaction and a replay must not append another outbox item. Define how the adapter reports insertion versus replay so the application cannot guess from an unreliable side effect. A lock in one API process is not a replacement for the database constraint.

## Solution reasoning: corrections

A correction should point back to the original fact and preserve an explanation, actor and time. Decide whether it is a replacement fact, a signed adjustment or a cancellation paired with a new event. Those models have different reporting semantics. Do not use a negative quantity with the current constructor without redesigning the domain: the current positive-quantity rule intentionally rejects it.

Your acceptance evidence should show a duplicate correction request, an unauthorized actor, an already-finalized downstream invoice, and a failed operation that leaves the original history intact. You do not need to invent a distributed architecture to discuss these cases; a modular monolith still has consistency boundaries.

## Review rubric

| Area | Developing | Ready for independent small-feature ownership |
|---|---|---|
| Navigation | Names files after being told | Traces an unfamiliar request and explains each owner |
| Business rules | Adds checks where convenient | Chooses one authoritative boundary and tests edge cases |
| Concurrency | Says “the dictionary is thread-safe” | Identifies the atomic operation and its process/database limits |
| Contracts | Checks only happy-path JSON | Tests statuses, stable codes, links and failure recovery |
| Testing | Counts tests | Explains which failure each assertion would catch |
| Communication | Claims the feature is “production-ready” | Describes observed behavior, evidence and remaining limits |

Ask a reviewer to challenge one design choice. Respond with evidence or revise the design. Defending every first choice is not the goal; making a maintainable, understandable change is.
