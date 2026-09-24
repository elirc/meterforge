# INC-005 — Reconciliation mismatches of one cent on plan changes

**Branch:** `training/incidents/005-proration-reconciliation-drift` (based on `training/lab/sqlserver`)

**Alert (daily reconciliation report, October 2)**
```
reconciliation/payments  amount_mismatch: 1,318 of 41,902 payments (3.1%)   baseline < 0.05%
all mismatches: difference = +0.01 USD (provider amount higher than ours)
```

**What finance saw**
- Every mismatched invoice has a **proration credit** line (a customer changed plan mid-month). Invoices without proration all match.
- Not every prorated invoice mismatches: roughly half do.
- The payment provider computes the same proration independently (unused days / days in period × old monthly price, rounded half-to-even to cents).
- Example from the report:
```
invoice      old plan   unused/days  our credit   provider credit  difference
inv_8812     49.00 USD   17/30         27.76          27.77          +0.01
inv_8840     49.00 USD   10/30         16.33          16.33           0.00
inv_9021     19.00 USD   13/31          7.96           7.97          +0.01
inv_9107    120.00 USD    1/30          4.00           4.00           0.00
```

**Code shipped in September**: `feat(billing): persist proration credits for mid-period plan changes`
```diff
+ Migrations/..._AddProrationCredits.cs   (creates billing.proration_credits)
+        credit.Property(c => c.Amount).HasColumnName("amount");
...
+            Amount = oldMonthlyPrice.Amount * unusedDays / daysInPeriod,
...
+        return row is null ? null : new Money(decimal.Round(row.Amount, 2, MidpointRounding.ToEven), row.Currency);
```
The PR description says: "Store the exact credit and round once, half-even, when the credit is read for invoicing."
Unit tests for the rounding (`decimal.Round(27.7666…, 2, ToEven) == 27.77`) pass.

**Your job.** Reproduce with `SqlLab migrate` + `SqlLab prorate tenant-0001 --monthly 49 --unused 17 --days 30` on this branch. Find exactly
which layer changed the value (C#, EF Core, SqlClient, SQL Server), prove it, fix it, and list the other places in MeterForge where the same
thing could happen. Write the postmortem, including how to correct the 1,318 invoices.

Sealed: [`_answers/incidents/005-proration-reconciliation-drift.md`](../_answers/incidents/005-proration-reconciliation-drift.md)
