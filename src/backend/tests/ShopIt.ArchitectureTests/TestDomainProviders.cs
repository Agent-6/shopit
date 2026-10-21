using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Domain;
using ShopIt.Framework.Domain.Providers;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Populates <see cref="DomainProviders"/> once, before any test in this assembly runs.
/// </summary>
/// <remarks>
/// <para>
/// Domain entities read <c>DomainProviders.Guid</c>/<c>Date</c> when they raise a domain event, and those
/// statics are <c>null</c> until <c>UseDomainServices</c> sets them — which in a service happens at
/// startup. Without this, constructing any aggregate in a test throws a <see cref="NullReferenceException"/>.
/// </para>
/// <para>
/// Deliberately a <see cref="ModuleInitializerAttribute"/> and not a fixture's static constructor: xunit
/// runs test classes in parallel, so "some other class initialised it first" is an ordering assumption
/// that holds until it does not. It was already like this — <c>TenancyResultTests</c> and the catalog's
/// result tests both construct aggregates, and the suite went from green to red on a rebuild purely
/// because of scheduling. This runs when the assembly loads, so nothing can observe the state
/// half-initialised.
/// </para>
/// <para>
/// The providers are stand-ins: the shipped implementations live in
/// <c>ShopIt.Framework.Infrastructure</c>, and these interfaces exist precisely so tests can supply their
/// own.
/// </para>
/// </remarks>
internal static class TestDomainProviders
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGuidProvider, TestGuidProvider>();
        services.AddSingleton<IDateProvider, TestDateProvider>();
        services.BuildServiceProvider().UseDomainServices();
    }

    private sealed class TestGuidProvider : IGuidProvider
    {
        public Guid NewGuid() => Guid.NewGuid();
    }

    private sealed class TestDateProvider : IDateProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime Today => DateTime.UtcNow.Date;
    }
}
