# Tests Agent Index

This folder contains package and integration-boundary tests.

Start with:

- [README.md](README.md) for test project navigation.
- [../docs/testing.md](../docs/testing.md) for categories and verification
  entrypoints.

Use `Category=Unit` and `Category=Application` for fast tests that avoid
external infrastructure. Use `Category=Integration` for provider-backed
database, transport, sample, or infrastructure behavior. Use
`Category=Package` only for tests that inspect freshly produced package
artifacts from `pnpm backend:pack`.
