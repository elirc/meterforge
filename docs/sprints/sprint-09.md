# Sprint 9: Discounts, tax boundary, and credits

Discounts calculate an explicit reduction without mutating the source amount. Tax is a replaceable application port whose quote retains provider, jurisdiction, and calculation-version evidence. Corrections use immutable positive credit notes linked to the original invoice.

The model avoids a generic rules engine: percentage policy, external tax calculation, and corrective financial records have different owners and failure modes.
