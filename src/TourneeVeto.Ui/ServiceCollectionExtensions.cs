using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TourneeVeto.Ui.Data;

namespace TourneeVeto.Ui;

/// <summary>Enregistrement des services de TournéeVéto, commun à tous les hôtes (Web aujourd'hui, WPF et MAUI demain).</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Ajoute le stockage local (IndexedDB) et le chargement du jeu de démonstration.</summary>
    public static IServiceCollection AddTourneeVeto(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Un hôte ou un test peut fournir son propre TimeProvider avant cet appel.
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IVisitRepository, IndexedDbVisitRepository>();
        services.AddScoped<DemoDataSeeder>();
        return services;
    }
}
