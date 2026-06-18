# Bondstone.PublicApi.Tests

This test project protects the checked-in public API baseline for packable
Bondstone packages.

The baseline test uses `PublicApiGenerator` for each package assembly and
compares the generated public/protected surface to files under `Baselines/`.

To intentionally refresh the baselines after an approved public API change:

```bash
BONDSTONE_UPDATE_PUBLIC_API_BASELINE=1 dotnet test tests/Bondstone.PublicApi.Tests/Bondstone.PublicApi.Tests.csproj --configuration Release
```

Review the resulting baseline diff before merging. Do not update baselines as
a substitute for BMAD architecture review or release-note treatment when a public API change
is compatibility-sensitive.
