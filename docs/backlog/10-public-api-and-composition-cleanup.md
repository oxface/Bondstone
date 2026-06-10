# Public API And Composition Cleanup

Priority: High

Goal: turn the architecture decision work from items 05 through 08 into
focused code cleanup without changing durable behavior by accident.

## Scope

- Classify public implementation types as:
  - stable user-facing API;
  - advanced composition API;
  - accidental public implementation detail.
- Clarify or reduce resolver duplication around module-owned persistence
  registrations.
- Decide whether fallback non-module persistence services stay documented
  advanced composition or move toward retirement.
- Review naming around provider-neutral receive entrypoints versus handler
  pipeline behaviors.
- Review public API naming after the terminal-failure rename.

## Related ADRs

- [0042 Module Persistence Capability Metadata](../adr/0042-module-persistence-capability-metadata.md)
- [0045 Module Execution Context Semantics](../adr/0045-module-execution-context-semantics.md)
- [0046 Public API Surface Policy](../adr/0046-public-api-surface-policy.md)

## Intake From Item 08

ADR 0046 accepted the compatibility-first policy but did not authorize broad
surface reduction. The next cleanup slice must produce an inventory before
hiding, renaming, or removing public types.

## Candidate Work Items

- Add a public API inventory or baseline artifact so accidental expansion is
  visible in review.
- Classify public concrete implementation types by package as normal API,
  advanced composition API, or public implementation detail exposed for now.
- Mark or document advanced composition APIs before changing visibility,
  especially low-level persistence services, dispatcher contracts, provider
  diagnostics, and transport receive helpers.
- Add docs that distinguish normal setup APIs from advanced composition APIs.
- Centralize common module persistence resolver diagnostics if it can stay
  internal and simple.
- Identify public concrete types that should remain callable but stop growing
  as extension points.
- Produce a compatibility plan before removing, hiding, or renaming public
  types.
- Decide whether an automated API baseline is needed before stronger
  compatibility promises or package cleanup.

## Proposed Slices

1. Inventory slice: list public namespaces and concrete public types by
   package, including whether each is normal API, advanced composition, or
   implementation detail exposed for now. Include public constructors and
   parameter names where source compatibility matters.
2. Documentation slice: update stable docs and package guidance so users know
   which APIs are expected for normal setup.
3. Resolver cleanup slice: remove obvious duplicated persistence resolution or
   diagnostic text only where behavior stays identical.
4. Compatibility slice: if public surface needs trimming, use ADR 0046's
   compatibility policy and add an amendment or follow-up ADR before broad
   implementation.

## Verification

- `pnpm backend:build`
- `pnpm backend:test:fast`
- `pnpm backend:pack`
