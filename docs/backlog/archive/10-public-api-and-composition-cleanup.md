# Public API And Composition Cleanup

Archived: 2026-06-12

## Outcome

The active public API cleanup note is resolved as a documentation and planning
slice.

The first-pass public API inventory now covers all current package projects in
[../../public-api.md](../../public-api.md). The inventory remains the current
classification source for normal setup APIs, user application contracts,
advanced composition APIs, provider/runtime contracts, public implementation
details exposed for now, and cleanup candidates.

No public type was hidden, removed, renamed, or moved in this slice.

## Decisions

- `Bondstone.Utility.StringExtensions`, shipped from
  `Bondstone.Persistence`, remains public for now but is not documented as a
  user extension point or advanced composition API. It stays a cleanup
  candidate for future compatibility-planned internalization or replacement
  before stronger compatibility expectations.
- `BondstoneLocalServiceCollectionExtensions` remains public for now only
  because it is already exposed. Its current registration method is internal,
  so the type should be treated as accidental registration plumbing and remain
  a cleanup candidate requiring ADR 0046 compatibility planning and
  release-note treatment before hiding.
- No other public implementation details in the current inventory are clearly
  obsolete or misleading enough to promote to cleanup candidates now. Broad
  public implementation detail churn is deferred.
- A public API baseline/tool is not required for this doc-only decision slice.
  Add one before stronger compatibility promises or broad public-surface
  reduction.

ADR 0046 remains unchanged: the compatibility-first policy already covers
these decisions, and this slice does not change package boundaries or public
API shape.

## Deferred Work

Remaining compatibility work has returned to
[../00-plans.md](../00-plans.md):

- cleanup planning for the two current cleanup candidates;
- automated public API baseline/tooling before stronger compatibility promises
  or broad public-surface cleanup;
- package README quick-path cleanup where normal setup docs need to point away
  from advanced composition details.

## Verification

Doc-focused verification for the final decision slice:

- `pnpm format:check`
