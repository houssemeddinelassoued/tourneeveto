using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Showcase;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Ui.Data;

/// <summary>Données lues dans le stockage local avec la journée de démonstration qui en est dérivée.</summary>
/// <param name="Day">Journée de démonstration (profil, étapes, matériel, indicateurs).</param>
/// <param name="Farms">Tous les élevages du stockage.</param>
/// <param name="Cows">Les vaches des élevages visités aujourd'hui.</param>
/// <param name="Today">Date du jour (TimeProvider, heure locale).</param>
public sealed record ShowcaseSnapshot(ShowcaseDay Day, IReadOnlyList<Farm> Farms, IReadOnlyList<Cow> Cows, DateOnly Today);

/// <summary>Charge les données du jour dans <see cref="IVisitRepository"/> et construit la journée de démonstration (aucune logique métier ici).</summary>
public sealed class ShowcaseProvider(DemoDataSeeder seeder, IVisitRepository repository, TimeProvider timeProvider, RegieThresholds thresholds)
{
    /// <summary>Lit les élevages, les visites du jour et les vaches ; lève <see cref="StorageUnavailableException"/> si le stockage est inaccessible.</summary>
    public async Task<ShowcaseSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        await seeder.EnsureSeededAsync(cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var farms = await repository.GetFarmsAsync(cancellationToken);
        var visits = await repository.GetVisitsByDateAsync(today, cancellationToken);
        var cows = new List<Cow>();
        foreach (var farmId in visits.Select(visit => visit.FarmId).Distinct().Where(id => farms.Any(farm => farm.Id == id)).Order(StringComparer.Ordinal))
        {
            cows.AddRange(await repository.GetCowsByFarmAsync(farmId, cancellationToken));
        }

        return new ShowcaseSnapshot(ShowcaseBuilder.Build(farms, visits, cows, today, thresholds), farms, cows, today);
    }
}
