# Bondstone.Package.Tests

Package artifact tests for produced Bondstone NuGet packages.

This project inspects `.nupkg` files created by `pnpm backend:pack`. It is not
part of the default fast test filter because package tests require freshly
produced artifacts in `artifacts/packages`.

See:

- [../../docs/packaging.md](../../docs/packaging.md) for package artifact and
  release policy.
- [../../docs/testing.md](../../docs/testing.md) for the `Package` category and
  verification entrypoints.
