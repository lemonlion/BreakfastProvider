using System.Reflection;
using InMemoryEmulator.ClickHouse.Core;

namespace BreakfastProvider.Tests.Unit.Emulator;

/// <summary>
/// <c>Core/</c> must never reference <c>System.Net.Http</c>, so that a native-TCP front-end could be
/// added later without a rewrite. Results cross the boundary as <see cref="ClickHouseResultSet"/>.
/// </summary>
public class CoreIsTransportAgnosticTests
{
    [Fact]
    public void Core_types_expose_nothing_from_System_Net_Http()
    {
        var coreNamespace = typeof(IClickHouseQueryEngine).Namespace!;
        var coreTypes = typeof(IClickHouseQueryEngine).Assembly.GetTypes()
            .Where(t => t.Namespace == coreNamespace)
            .ToList();

        coreTypes.Should().NotBeEmpty();

        var offenders = new List<string>();
        foreach (var type in coreTypes)
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            foreach (var field in type.GetFields(all))
                if (IsHttp(field.FieldType)) offenders.Add($"{type.Name}.{field.Name}");

            foreach (var property in type.GetProperties(all))
                if (IsHttp(property.PropertyType)) offenders.Add($"{type.Name}.{property.Name}");

            foreach (var method in type.GetMethods(all))
            {
                if (IsHttp(method.ReturnType)) offenders.Add($"{type.Name}.{method.Name}");
                foreach (var parameter in method.GetParameters())
                    if (IsHttp(parameter.ParameterType)) offenders.Add($"{type.Name}.{method.Name}({parameter.Name})");
            }
        }

        offenders.Should().BeEmpty();
    }

    private static bool IsHttp(Type type) =>
        type.Namespace?.StartsWith("System.Net.Http", StringComparison.Ordinal) == true ||
        type.Assembly.GetName().Name == "System.Net.Http";
}
