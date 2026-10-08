using Bunit;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui.Data;

namespace TourneeVeto.Tests.Ui.Data;

/// <summary>Chargement du jeu de démonstration dans le stockage local (story 2.1), module JS simulé.</summary>
public class DemoDataSeederTests : BunitContext
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 10, 8, 7, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task Envoie_un_jeu_date_du_jour_et_le_charge_une_seule_fois()
    {
        var module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(true);
        await using var repository = new IndexedDbVisitRepository(JSInterop.JSRuntime);
        var seeder = new DemoDataSeeder(repository, timeProvider);

        await Task.WhenAll(seeder.EnsureSeededAsync(), seeder.EnsureSeededAsync());

        var invocation = Assert.Single(module.Invocations["seedIfEmpty"]);
        var visits = Assert.IsAssignableFrom<IReadOnlyList<Visit>>(invocation.Arguments[2]);
        Assert.All(visits, visit => Assert.Equal(Today, visit.Date));
    }

    [Fact]
    public async Task Un_echec_de_stockage_est_retente_au_prochain_appel()
    {
        var module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        var seedIfEmpty = module.Setup<bool>("seedIfEmpty", _ => true);
        seedIfEmpty.SetException(new JSException("TOURNEEVETO_STORAGE:Unavailable:BlockedError autre onglet"));
        await using var repository = new IndexedDbVisitRepository(JSInterop.JSRuntime);
        var seeder = new DemoDataSeeder(repository, timeProvider);

        await Assert.ThrowsAsync<StorageUnavailableException>(() => seeder.EnsureSeededAsync());
        seedIfEmpty.SetResult(false);
        var seeded = await seeder.EnsureSeededAsync();

        Assert.False(seeded);
        Assert.Equal(2, module.Invocations["seedIfEmpty"].Count);
    }
}
