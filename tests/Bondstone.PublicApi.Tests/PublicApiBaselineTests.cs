using System.Reflection;
using System.Runtime.CompilerServices;
using System.Globalization;
using Xunit;

namespace Bondstone.PublicApi.Tests;

public sealed class PublicApiBaselineTests
{
    private const string UpdateBaselineEnvironmentVariable = "BONDSTONE_UPDATE_PUBLIC_API_BASELINE";

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(PackageAssemblies))]
    public void PackagePublicApi_MatchesBaseline(string assemblyName)
    {
        string actual = PublicApiSnapshot.Generate(Assembly.Load(new AssemblyName(assemblyName)));
        string baselinePath = Path.Combine(AppContext.BaseDirectory, "Baselines", assemblyName + ".txt");

        if (ShouldUpdateBaselines())
        {
            string sourceBaselinePath = FindSourceBaselinePath(assemblyName);
            Directory.CreateDirectory(Path.GetDirectoryName(sourceBaselinePath)!);
            File.WriteAllText(sourceBaselinePath, actual);
            return;
        }

        Assert.True(
            File.Exists(baselinePath),
            $"Missing public API baseline for {assemblyName}. Run with {UpdateBaselineEnvironmentVariable}=1 to create it.");

        string expected = File.ReadAllText(baselinePath).ReplaceLineEndings("\n");

        Assert.Equal(expected, actual);
    }

    public static TheoryData<string> PackageAssemblies()
    {
        return
        [
            "Bondstone",
            "Bondstone.Capabilities.DomainEvents",
            "Bondstone.Capabilities.DomainEvents.EntityFrameworkCore",
            "Bondstone.Hosting",
            "Bondstone.Persistence",
            "Bondstone.Persistence.EntityFrameworkCore",
            "Bondstone.Persistence.EntityFrameworkCore.Postgres",
            "Bondstone.Persistence.Postgres",
            "Bondstone.Transport",
            "Bondstone.Transport.Local",
            "Bondstone.Transport.RabbitMq",
            "Bondstone.Transport.ServiceBus",
        ];
    }

    private static bool ShouldUpdateBaselines()
    {
        string? value = Environment.GetEnvironmentVariable(UpdateBaselineEnvironmentVariable);

        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindSourceBaselinePath(string assemblyName)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bondstone.slnx")))
            {
                return Path.Combine(
                    directory.FullName,
                    "tests",
                    "Bondstone.PublicApi.Tests",
                    "Baselines",
                    assemblyName + ".txt");
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }
}

internal static class PublicApiSnapshot
{
    private static readonly Dictionary<Type, string> TypeAliases = new()
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(char)] = "char",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(nint)] = "nint",
        [typeof(nuint)] = "nuint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(object)] = "object",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(string)] = "string",
        [typeof(void)] = "void",
    };

    public static string Generate(Assembly assembly)
    {
        List<string> lines = [];

        foreach (Type type in GetVisibleTypes(assembly))
        {
            lines.Add(FormatType(type));

            foreach (string member in GetVisibleMembers(type))
            {
                lines.Add("  " + member);
            }

            lines.Add(string.Empty);
        }

        return string.Join('\n', lines).TrimEnd() + "\n";
    }

    private static IEnumerable<Type> GetVisibleTypes(Assembly assembly)
    {
        return assembly
            .GetTypes()
            .Where(IsVisibleOutsideAssembly)
            .OrderBy(type => FormatTypeName(type), StringComparer.Ordinal);
    }

    private static bool IsVisibleOutsideAssembly(Type type)
    {
        if (type.IsNested)
        {
            return IsVisibleOutsideAssembly(type.DeclaringType!)
                && (type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem);
        }

        return type.IsPublic;
    }

    private static IEnumerable<string> GetVisibleMembers(Type type)
    {
        BindingFlags flags = BindingFlags.DeclaredOnly
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        List<string> members = [];

        members.AddRange(
            type.GetConstructors(flags)
                .Where(IsVisibleOutsideAssembly)
                .Select(FormatConstructor));

        members.AddRange(
            type.GetFields(flags)
                .Where(IsVisibleOutsideAssembly)
                .Where(field => !field.IsSpecialName)
                .Select(FormatField));

        members.AddRange(
            type.GetProperties(flags)
                .Where(IsVisibleOutsideAssembly)
                .Select(FormatProperty));

        members.AddRange(
            type.GetEvents(flags)
                .Where(IsVisibleOutsideAssembly)
                .Select(FormatEvent));

        members.AddRange(
            type.GetMethods(flags)
                .Where(IsVisibleOutsideAssembly)
                .Where(method => !IsAccessorMethod(method))
                .Select(FormatMethod));

        return members.Order(StringComparer.Ordinal);
    }

    private static bool IsVisibleOutsideAssembly(MethodBase method)
    {
        return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
    }

    private static bool IsVisibleOutsideAssembly(FieldInfo field)
    {
        return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
    }

    private static bool IsVisibleOutsideAssembly(PropertyInfo property)
    {
        return GetVisibleAccessors(property).Any();
    }

    private static bool IsVisibleOutsideAssembly(EventInfo eventInfo)
    {
        return GetVisibleAccessors(eventInfo).Any();
    }

    private static string FormatType(Type type)
    {
        List<string> parts = [FormatTypeVisibility(type)];

        if (type.IsAbstract && type.IsSealed)
        {
            parts.Add("static");
        }
        else
        {
            if (type.IsAbstract && !type.IsInterface)
            {
                parts.Add("abstract");
            }

            if (type.IsSealed && type.IsClass)
            {
                parts.Add("sealed");
            }
        }

        parts.Add(FormatTypeKind(type));
        parts.Add(FormatTypeName(type));

        List<string> inheritance = [];
        if (type.BaseType is not null
            && type.BaseType != typeof(object)
            && type.BaseType != typeof(ValueType)
            && type.BaseType != typeof(Enum))
        {
            inheritance.Add(FormatTypeName(type.BaseType));
        }

        inheritance.AddRange(type.GetInterfaces().Select(FormatTypeName));

        string line = "type " + string.Join(' ', parts);

        if (inheritance.Count > 0)
        {
            line += " : " + string.Join(", ", inheritance.Order(StringComparer.Ordinal));
        }

        return line;
    }

    private static string FormatTypeVisibility(Type type)
    {
        if (type.IsNestedFamily)
        {
            return "protected";
        }

        if (type.IsNestedFamORAssem)
        {
            return "protected internal";
        }

        return "public";
    }

    private static string FormatTypeKind(Type type)
    {
        if (type.IsInterface)
        {
            return "interface";
        }

        if (type.IsEnum)
        {
            return "enum";
        }

        return type.IsValueType ? "struct" : "class";
    }

    private static string FormatConstructor(ConstructorInfo constructor)
    {
        return $"ctor {FormatMethodVisibility(constructor)} {FormatTypeName(constructor.DeclaringType!)}({FormatParameters(constructor.GetParameters())})";
    }

    private static string FormatField(FieldInfo field)
    {
        List<string> parts = [FormatMethodVisibility(field)];

        if (field.IsLiteral)
        {
            parts.Add("const");
        }
        else
        {
            if (field.IsStatic)
            {
                parts.Add("static");
            }

            if (field.IsInitOnly)
            {
                parts.Add("readonly");
            }
        }

        parts.Add(FormatTypeName(field.FieldType));
        parts.Add(field.Name);

        return "field " + string.Join(' ', parts);
    }

    private static string FormatProperty(PropertyInfo property)
    {
        string parameters = property.GetIndexParameters().Length == 0
            ? string.Empty
            : $"[{FormatParameters(property.GetIndexParameters())}]";

        return $"property {FormatTypeName(property.PropertyType)} {property.Name}{parameters} {{ {FormatAccessors(property)} }}";
    }

    private static string FormatEvent(EventInfo eventInfo)
    {
        return $"event {FormatTypeName(eventInfo.EventHandlerType!)} {eventInfo.Name} {{ {FormatAccessors(eventInfo)} }}";
    }

    private static string FormatMethod(MethodInfo method)
    {
        List<string> parts = [FormatMethodVisibility(method)];

        if (method.IsStatic)
        {
            parts.Add("static");
        }
        else
        {
            if (method.IsAbstract)
            {
                parts.Add("abstract");
            }
            else if (method.IsVirtual && method.GetBaseDefinition() != method)
            {
                parts.Add("override");
            }
            else if (method.IsVirtual && !method.IsFinal)
            {
                parts.Add("virtual");
            }
        }

        if (method.IsDefined(typeof(ExtensionAttribute), inherit: false))
        {
            parts.Add("extension");
        }

        parts.Add(FormatTypeName(method.ReturnType));
        parts.Add(FormatMethodName(method));

        return $"method {string.Join(' ', parts)}({FormatParameters(method.GetParameters())})";
    }

    private static string FormatMethodName(MethodInfo method)
    {
        if (!method.IsGenericMethod)
        {
            return method.Name;
        }

        string genericArguments = string.Join(", ", method.GetGenericArguments().Select(FormatTypeName));

        return $"{method.Name}<{genericArguments}>";
    }

    private static string FormatMethodVisibility(MethodBase method)
    {
        if (method.IsFamily)
        {
            return "protected";
        }

        if (method.IsFamilyOrAssembly)
        {
            return "protected internal";
        }

        return "public";
    }

    private static string FormatMethodVisibility(FieldInfo field)
    {
        if (field.IsFamily)
        {
            return "protected";
        }

        if (field.IsFamilyOrAssembly)
        {
            return "protected internal";
        }

        return "public";
    }

    private static string FormatParameters(IEnumerable<ParameterInfo> parameters)
    {
        return string.Join(", ", parameters.Select(FormatParameter));
    }

    private static string FormatParameter(ParameterInfo parameter)
    {
        string prefix = string.Empty;
        Type parameterType = parameter.ParameterType;

        if (parameter.IsDefined(typeof(ParamArrayAttribute), inherit: false))
        {
            prefix = "params ";
        }

        if (parameterType.IsByRef)
        {
            prefix = parameter.IsOut ? "out " : parameter.IsIn ? "in " : "ref ";
            parameterType = parameterType.GetElementType()!;
        }

        string value = prefix + FormatTypeName(parameterType) + " " + parameter.Name;

        if (parameter.HasDefaultValue)
        {
            value += " = " + FormatDefaultValue(parameter.DefaultValue, parameterType);
        }

        return value;
    }

    private static string FormatDefaultValue(object? value, Type parameterType)
    {
        if (value is null)
        {
            return parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null
                ? "default"
                : "null";
        }

        return value switch
        {
            string text => "\"" + text.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"",
            char character => "'" + character + "'",
            bool boolean => boolean ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "null",
        };
    }

    private static string FormatAccessors(PropertyInfo property)
    {
        return string.Join(
            " ",
            GetVisibleAccessors(property)
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .Select(method => method.Name.StartsWith("get_", StringComparison.Ordinal)
                    ? "get;"
                    : "set;"));
    }

    private static string FormatAccessors(EventInfo eventInfo)
    {
        return string.Join(
            " ",
            GetVisibleAccessors(eventInfo)
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .Select(method => method.Name.StartsWith("add_", StringComparison.Ordinal)
                    ? "add;"
                    : "remove;"));
    }

    private static IEnumerable<MethodInfo> GetVisibleAccessors(PropertyInfo property)
    {
        return property.GetAccessors(nonPublic: true).Where(IsVisibleOutsideAssembly);
    }

    private static IEnumerable<MethodInfo> GetVisibleAccessors(EventInfo eventInfo)
    {
        MethodInfo? addMethod = eventInfo.GetAddMethod(nonPublic: true);
        MethodInfo? removeMethod = eventInfo.GetRemoveMethod(nonPublic: true);

        return (addMethod, removeMethod) switch
        {
            ({ }, { }) => [addMethod, removeMethod],
            ({ }, null) => [addMethod],
            (null, { }) => [removeMethod],
            _ => [],
        };
    }

    private static bool IsAccessorMethod(MethodInfo method)
    {
        return method.IsSpecialName
            && (method.Name.StartsWith("get_", StringComparison.Ordinal)
                || method.Name.StartsWith("set_", StringComparison.Ordinal)
                || method.Name.StartsWith("add_", StringComparison.Ordinal)
                || method.Name.StartsWith("remove_", StringComparison.Ordinal));
    }

    private static string FormatTypeName(Type type)
    {
        if (TypeAliases.TryGetValue(type, out string? alias))
        {
            return alias;
        }

        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (type.IsArray)
        {
            return FormatTypeName(type.GetElementType()!) + "[]";
        }

        if (type.IsByRef)
        {
            return FormatTypeName(type.GetElementType()!);
        }

        Type? nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType is not null)
        {
            return FormatTypeName(nullableType) + "?";
        }

        if (type.IsGenericType)
        {
            string genericTypeName = type.GetGenericTypeDefinition().FullName ?? type.Name;
            int tickIndex = genericTypeName.IndexOf('`', StringComparison.Ordinal);
            if (tickIndex >= 0)
            {
                genericTypeName = genericTypeName[..tickIndex];
            }

            string genericArguments = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
            return $"{genericTypeName.Replace("+", ".", StringComparison.Ordinal)}<{genericArguments}>";
        }

        return (type.FullName ?? type.Name).Replace("+", ".", StringComparison.Ordinal);
    }
}
