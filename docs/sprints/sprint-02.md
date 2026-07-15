# Sprint 2: Customer subscriptions and trials

Subscriptions own explicit trial, active, paused, and cancelled transitions. Invalid repeated or out-of-order commands fail with stable business codes, and each successful decision advances the aggregate version.

The state machine is plain C# rather than a workflow framework: every legal transition is visible, testable, and easy to change when product policy changes.
