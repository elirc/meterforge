# Sprint 6: Invoice calculation and finalization

Invoices preserve itemized amount, price-version identity, and source reference. Finalization records a UTC fact and rejects empty or repeated finalization. Totals derive from line money rather than a separately mutable scalar.

Review question: which operations must become impossible after finalization, and should a correction rewrite this document or issue a new financial record?
