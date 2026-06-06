using Bondstone.Utility;

namespace Bondstone.Modules;

public sealed class BondstoneModuleRegistration
{
    public BondstoneModuleRegistration(
        string name,
        bool usesDurableMessaging)
    {
        Name = name.NormalizeRequired(nameof(name), "Module name");
        UsesDurableMessaging = usesDurableMessaging;
    }

    public string Name { get; }

    public bool UsesDurableMessaging { get; }
}
