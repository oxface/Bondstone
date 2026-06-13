namespace Bondstone.Modules;

public interface IBondstoneModuleRegistry
{
    IReadOnlyCollection<BondstoneModuleRegistration> Modules { get; }

    BondstoneModuleRegistration GetModule(string moduleName);

    bool TryGetModule(
        string moduleName,
        out BondstoneModuleRegistration? registration);
}
