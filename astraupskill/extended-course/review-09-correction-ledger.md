# Review 09. Correction ledger

## MF-28A

A fully paid 100 invoice with a 20 credit has corrected receivable 80 and net collected cash 100 until the refund confirms. The pending return obligation is 20. The unpaid version has corrected receivable 80 and cash 0, so no cash refund is implied. An invoice 100 with collected 110 can refund 10 while leaving corrected receivable 100. The same positive amount can represent different event roles, so event category and correspondence must remain explicit.

The review should require intermediate states, not only final balanced examples. Those intermediate states explain customer questions during processing. A pending provider outcome must not be presented as completed cash movement. The projection can report an obligation without claiming the external effect has already occurred.

## MF-28B

The shared authority accepts one 70 reservation and leaves 30 eligible. An explicit rejection before effect can release that reservation. A timeout after a possible effect keeps it unresolved until authoritative evidence settles the outcome. Later confirmation consumes the reservation into a confirmed refund without subtracting the same capacity twice.

Both local Refund records can pass construction, which is why the cumulative rule belongs around them. A process-local model demonstrates the protocol but not cross-process enforcement. The provider-backed acceptance test must coordinate independent callers and assert the accepted ledger, remaining capacity, and both responses. Silent partial fulfillment is outside the chosen request contract.

## MF-28C

The packet should preserve distinct correction, refund, reservation, and reversal identities. Exact replay returns accepted evidence; conflicting replay rejects changed meaning. A reversal references its original event and is itself replay-safe. A correction without a refund demonstrates the separation between receivable and cash histories.

Grade the model on conservation under its declared policy and on visible unresolved obligations. Deduct credit for releasing capacity on every exception, editing finalized invoice history, or treating a file-based restart demonstration as a transactional database guarantee. The best submission identifies the smallest provider-backed experiment needed after the pure protocol is sound.
