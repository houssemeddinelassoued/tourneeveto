using TourneeVeto.Domain;

namespace TourneeVeto.Ui.Data;

/// <summary>
/// Remplace les données en mémoire : au premier lancement, enregistre le jeu de démonstration dans le stockage local,
/// puis toutes les pages lisent leurs données via <see cref="IVisitRepository"/> (story 2.1).
/// </summary>
public sealed class DemoDataSeeder(IVisitRepository repository, TimeProvider timeProvider)
{
    /// <summary>Graine fixe : le même jour produit toujours le même troupeau.</summary>
    public const int Seed = 2026;

    private Task<bool>? seeding;

    /// <summary>
    /// À attendre avant toute lecture. Les appels concurrents partagent le même chargement ; un échec est retenté
    /// à l'appel suivant. Renvoie <c>true</c> si le jeu vient d'être enregistré.
    /// </summary>
    public Task<bool> EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (seeding is null || seeding.IsFaulted || seeding.IsCanceled)
        {
            var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
            seeding = repository.SeedIfEmptyAsync(DemoData.Generate(today, Seed), cancellationToken);
        }

        return seeding;
    }
}
