# Sprint 1: Product and fixed-price vertical slice

## Outcome

A tenant can create and retrieve a product with a fixed monthly price through `/api/v1`. Invalid money and product rules become stable Problem Details codes.

## Decisions

- Keep money and product invariants in a framework-free domain.
- Require tenant identity at the repository port instead of filtering after lookup.
- Map contracts explicitly so HTTP compatibility does not depend on domain shape.
- Use an in-memory adapter while the course establishes behavior; durability arrives only when its transaction lessons are in scope.

## Review exercise

Trace one invalid price from JSON to the domain rule and back to Problem Details. Then explain why adding a generic repository or mapping framework would not improve this slice.
