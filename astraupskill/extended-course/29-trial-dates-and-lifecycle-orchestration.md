# 29. Trial dates and lifecycle orchestration

Read [Subscription](../../src/MeterForge.Domain/Subscriptions/Subscription.cs) and [SubscriptionService](../../src/MeterForge.Application/Subscriptions/SubscriptionService.cs). Earlier lessons examined version checks and reference ownership. This chapter asks a different question: who decides when a valid transition should happen? The domain object permits or rejects transitions from a state, but it does not run a clock-driven lifecycle scheduler.

The constructor rejects a trial end before the subscription start. If TrialEndsOn is non-null, it initializes Trialing; otherwise it initializes Active. It does not compare those dates with today's date. A subscription with a trial end in the past can still begin in Trialing when constructed through this method. That is an observable source fact, not a recommendation for a production onboarding policy.

## State permission is not temporal eligibility

Activate requires Trialing and then changes state to Active while incrementing Version. It does not receive a date or check whether the trial has ended. Pause requires Active, Resume requires Paused, and Cancel rejects only an already Cancelled state. These methods express local state permission. A scheduler or application workflow would need to establish temporal eligibility and caller authority before invoking them.

Work a subscription starting on a synthetic January first with trial end January fifteenth. Construct it on paper as though today were January twentieth. The object still starts Trialing because non-null trial end selects that state. Calling Activate is permitted because the state is Trialing, not because the method evaluated January twentieth. A test that uses current wall time can obscure this distinction; fixed dates make it explicit.

Now construct a future-start subscription with no trial end. It starts Active under the constructor's branch. If the product wants a Scheduled state before StartsOn, that requires a proposed workflow or model extension. Do not silently reinterpret Active as "active only after its start date" unless every consumer applies that additional predicate consistently.

## Define a proposed lifecycle policy

For a teaching scheduler, choose a business date supplied by an injected calendar service. A trial is eligible for activation when the business date reaches its declared end under the selected inclusive or exclusive rule. A future-start subscription remains scheduled in a separate proposed state until its start date. These are deliberate product choices beyond the current enum.

The scheduler should discover eligible work from authoritative persisted state. A timer callback alone is not durable work identity. If the process stops at the intended transition time, a later sweep must discover the missed transition. Store enough evidence to determine eligibility after restart rather than relying on a single in-memory notification.

Decide how payment readiness affects activation. A free trial may transition to paid service only after a valid collection arrangement, or the product may allow a grace period. The current Activate method knows nothing about payment evidence. A proposed coordinator should keep that decision explicit and avoid smuggling an external payment call into a domain method that currently has no such dependency.

## Repeated sweeps and competing actors

Two scheduler instances can both discover the same eligible Trialing subscription. The acceptance boundary must ensure one transition or one accepted lifecycle operation. The current repository's expected-version mutation is a useful local model, but a durable adapter must enforce it across independent processes. The scheduler's discovery query is not itself a claim.

Work both actors reading Version one. Actor A activates successfully to Version two. Actor B attempts with expected one and should receive the chosen conflict behavior under the repository protocol. B can reload and recognize that the desired state already exists, but it should not blindly issue another unrelated transition. A stable lifecycle operation identity helps distinguish replay from a different actor's manual action.

Manual cancellation can race with activation. If cancellation wins, the scheduler must not revive the subscription by reconstructing a stale Trialing object and overwriting it. If activation wins first, cancellation may still be valid afterward under product policy. The final state depends on accepted ordering, which should be recorded rather than inferred from request arrival timestamps.

## Calendar date selection is a policy boundary

DateOnly contains no timezone. A scheduler that derives a business date from UTC must decide whether UTC is the intended calendar or merely a transport convention. Tenants may use one global calendar or configured billing calendars. This workbook does not select a universal answer; it requires the answer to be explicit and tested.

Use a fixed instant near a date boundary and two hypothetical calendars to demonstrate the difference. The same instant can correspond to different business dates. A transition rule based on the tenant's local date must record which calendar version was used if that setting can change. Otherwise a later replay may evaluate the same instant under a different policy.

Avoid tests that sleep until midnight or manipulate the operating-system clock. A supplied clock and calendar conversion make the boundary deterministic. The domain methods can remain focused on state permission while a pure eligibility function computes whether a transition should be proposed for a given date.

## Version increments are evidence of accepted mutation

Every successful lifecycle transition shown increments Version. A rejected transition should leave state and version unchanged. A scheduler test should assert both, because an implementation that increments before validation can make a failed operation look like an accepted change. The current methods validate before their assignments.

Version is not a timestamp or a count of every event in the customer's history. It tracks mutations through this object under its current API. External payment attempts or notifications do not automatically increment it. A proposed audit log can reference both subscription version and separate operation identities without treating one integer as a universal sequence for all services.

## Exercise MF-29A: separate permission and eligibility

Create traces for an expired trial, a future-start nontrial subscription, and activation before trial end. State current object behavior and a proposed lifecycle policy separately. Identify the inputs missing from Activate that prevent it from enforcing your temporal rule alone.

## Exercise MF-29B: design a recoverable scheduler

Define discovery, claim, expected version, operation identity, and restart behavior. Work two schedulers plus a manual cancellation. Specify how the loser reloads accepted state without reviving a canceled subscription.

## Exercise MF-29C: test a calendar boundary

Choose a fixed instant near midnight and a declared business calendar. Write a pure eligibility test and a state-transition test. Explain why neither a DateOnly value nor a current wall-clock call alone establishes the product's calendar policy.

## Review checkpoint

Lifecycle correctness requires both a valid state transition and a valid reason to perform it now. The current domain object provides the first part. A strong orchestration design supplies time, authority, recovery, and concurrency evidence without pretending those concerns are already hidden inside Activate.
