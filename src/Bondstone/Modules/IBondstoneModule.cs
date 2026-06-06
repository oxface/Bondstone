namespace Bondstone.Modules;

public interface IBondstoneModule
{
    string Name { get; }

    void Configure(BondstoneModuleBuilder module);
}
