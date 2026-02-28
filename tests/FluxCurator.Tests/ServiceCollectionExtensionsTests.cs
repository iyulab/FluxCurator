namespace FluxCurator.Tests;

using global::FluxCurator.Core;
using global::FluxCurator.Core.Core;
using global::FluxCurator.Infrastructure.Chunking;
using Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFluxCurator_RegistersIFluxCurator()
    {
        var services = new ServiceCollection();
        services.AddFluxCurator();

        using var provider = services.BuildServiceProvider();
        var curator = provider.GetService<IFluxCurator>();

        Assert.NotNull(curator);
    }

    [Fact]
    public void AddFluxCurator_RegistersIChunkerFactory()
    {
        var services = new ServiceCollection();
        services.AddFluxCurator();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IChunkerFactory>();

        Assert.NotNull(factory);
    }

    [Fact]
    public void AddFluxCurator_IFluxCurator_IsTransient()
    {
        var services = new ServiceCollection();
        services.AddFluxCurator();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetService<IFluxCurator>();
        var second = provider.GetService<IFluxCurator>();

        Assert.NotSame(first, second);
    }
}
