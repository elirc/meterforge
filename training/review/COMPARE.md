# Compare: my review vs the reference

| # | Reference finding (title) | Severity | Did I find it? (yes / partly / no) | If partly or no: what I looked at instead |
|---|---|---|---|---|
| 1 | | | | |
| 2 | | | | |
| … | | | | |

Findings I had that the reference doesn't (keep them if you can defend them with `path:line`):

| My finding | Evidence | Severity | Is it real? (checked how) |
|---|---|---|---|

## The pattern behind my misses
Tick what applies and write one sentence of evidence for each tick.
- [ ] I read files top to bottom instead of following data (request → storage → response).
- [ ] I trusted names: `*Service`, `*Store`, `ready`, "preview", "idempotent".
- [ ] I didn't ask "what if this runs twice / concurrently / after a restart".
- [ ] I missed money precision / rounding / truncation.
- [ ] I missed the gap between docs/health output and code.
- [ ] I didn't look at CI or tests at all.
- [ ] I found the bug but ranked it wrong (billing impact vs style).

**One habit I will use in the next review:** …
