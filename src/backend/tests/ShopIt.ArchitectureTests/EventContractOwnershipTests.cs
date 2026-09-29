using System.Reflection;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Encodes the dependency rule that <c>plans/event-contract-ownership.md</c> establishes, so the
/// inversion it fixed cannot quietly come back.
/// </summary>
/// <remarks>
/// <para>
/// The rule: <strong>the service that owns the meaning of the message owns the contract</strong> —
/// the publisher for a domain event, the receiving authority for a directed message.
/// </para>
/// <para>
/// Without this test the per-service contract assemblies buy almost nothing at build time in a
/// single-solution monorepo, because any event edit rebuilds everything anyway. What they buy is
/// legible, enforceable dependency direction — and that only holds while something checks it.
/// </para>
/// <para>
/// <strong>Limitation.</strong> This reads compiled assembly references, so it sees assemblies whose
/// types are actually <em>used</em>. Roslyn emits no <c>AssemblyRef</c> for a <c>ProjectReference</c>
/// that is declared but never used — verified by adding one and watching the test still pass — so a
/// purely decorative cross-service reference is invisible here. That is an acceptable trade: every
/// inversion this test exists to prevent was a real usage. Checking declared references too would
/// mean parsing every <c>.csproj</c>, which is a different and more fragile check.
/// </para>
/// </remarks>
public class EventContractOwnershipTests
{
    private static readonly string[] Services = ["Authentication", "Identity", "Notifications", "Tenancy"];

    /// <summary>Contract assembly suffixes a service is allowed to reference across a boundary.</summary>
    private static readonly string[] AllowedContractSuffixes = [".Events", ".Client"];

    [Fact]
    public void No_service_references_another_service_except_through_a_contract_assembly()
    {
        var violations = new List<string>();

        foreach (var assembly in ServiceAssemblies())
        {
            var ownService = ServiceOf(assembly.GetName())!;

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                var targetService = ServiceOf(reference);
                if (targetService is null || targetService == ownService)
                {
                    continue;
                }

                var name = reference.Name!;
                if (AllowedContractSuffixes.Any(name.EndsWith))
                {
                    continue;
                }

                violations.Add($"{assembly.GetName().Name} -> {name}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "A service referenced another service outside a contract assembly:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations) +
            Environment.NewLine +
            "Publish or consume a contract instead — see plans/event-contract-ownership.md.");
    }

    [Fact]
    public void Events_assemblies_reference_only_the_framework()
    {
        var violations = new List<string>();

        foreach (var assembly in ServiceAssemblies().Where(a => a.GetName().Name!.EndsWith(".Events")))
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                var name = reference.Name!;
                if (name.StartsWith("ShopIt.") && !name.StartsWith("ShopIt.Framework."))
                {
                    violations.Add($"{assembly.GetName().Name} -> {name}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "An .Events assembly referenced something other than the framework:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations) +
            Environment.NewLine +
            "An .Events assembly carries message contracts only — no handlers, no transport, no peers.");
    }

    [Fact]
    public void Every_service_has_an_events_assembly_it_can_publish_from()
    {
        // Guards the rule's usefulness: if a service has no .Events assembly, its contracts are
        // hiding in an implementation assembly again.
        var servicesWithEvents = ServiceAssemblies()
            .Where(a => a.GetName().Name!.EndsWith(".Events"))
            .Select(a => ServiceOf(a.GetName())!)
            .ToHashSet();

        Assert.Equal(Services.Order(), servicesWithEvents.Order());
    }

    /// <summary>All ShopIt service assemblies that landed in this test's output directory.</summary>
    private static IEnumerable<Assembly> ServiceAssemblies() =>
        Directory
            .GetFiles(AppContext.BaseDirectory, "ShopIt.*.dll")
            .Select(path => Assembly.LoadFrom(path))
            .Where(a => ServiceOf(a.GetName()) is not null)
            .DistinctBy(a => a.GetName().Name);

    /// <summary>"ShopIt.Tenancy.Application" → "Tenancy"; framework and non-service assemblies → null.</summary>
    private static string? ServiceOf(AssemblyName name)
    {
        var parts = name.Name?.Split('.') ?? [];
        return parts.Length >= 2 && parts[0] == "ShopIt" && Services.Contains(parts[1])
            ? parts[1]
            : null;
    }
}
