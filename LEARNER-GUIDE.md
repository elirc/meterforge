# Learner guide

For every sprint:

1. Read the milestone and issues before the code.
2. Read commits in order and predict the next design pressure.
3. Start at a failing business-rule test, then trace the use case, adapter, endpoint, and contract.
4. Read review comments as design evidence, not style instructions.
5. Change one invariant and confirm the test fails for a useful reason.
6. Explain the transaction, tenant, retry, and compatibility boundaries in your own words.

Ask continuously: what business fact must remain true, who owns it, which failure can be retried, and what evidence would justify another abstraction?
