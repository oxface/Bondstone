# Bondstone.Package.Tests Agent Index

This folder contains package artifact tests for produced Bondstone NuGet
packages.

Start with:

- [README.md](README.md) for test scope.
- [../../docs/testing.md](../../docs/testing.md) for categories and commands.
- [../../docs/packaging.md](../../docs/packaging.md) before changing package
  artifact expectations.

Use `Category=Package` only for tests that inspect freshly produced `.nupkg`
files from `pnpm backend:pack`.
