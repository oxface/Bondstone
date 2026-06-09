using Bondstone.Configuration;

namespace Bondstone.Modules;

public static class BondstoneModuleBuilderExtensions
{
    public static BondstoneBuilder Module(
        this BondstoneBuilder builder,
        string name,
        Action<BondstoneModuleBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        BondstoneModuleBuilder module = builder.CreateModuleBuilder(name);
        configure(module);

        return builder;
    }

    public static BondstoneBuilder AddModule<TModule>(
        this BondstoneBuilder builder)
        where TModule : IBondstoneModule, new()
    {
        ArgumentNullException.ThrowIfNull(builder);

        var module = new TModule();
        return builder.AddModule(module);
    }

    public static BondstoneBuilder AddModule(
        this BondstoneBuilder builder,
        IBondstoneModule module)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(module);

        BondstoneModuleBuilder moduleBuilder = builder.CreateModuleBuilder(module.Name);
        module.Configure(moduleBuilder);

        return builder;
    }
}
