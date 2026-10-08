using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Data;

namespace TourneeVeto.Tests.Ui;

/// <summary>AddTourneeVeto : enregistrement commun aux hôtes Web, WPF et MAUI.</summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Enregistre_le_stockage_IndexedDB_en_Scoped()
    {
        var services = new ServiceCollection().AddTourneeVeto();

        var repository = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IVisitRepository));
        Assert.Equal(ServiceLifetime.Scoped, repository.Lifetime);
        Assert.Equal(typeof(IndexedDbVisitRepository), repository.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(DemoDataSeeder)).Lifetime);
    }

    [Fact]
    public void Garde_le_TimeProvider_fourni_par_l_hote()
    {
        var hostTime = new FakeTimeProvider();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(hostTime);

        services.AddTourneeVeto();

        Assert.Same(hostTime, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TimeProvider)).ImplementationInstance);
    }
}
