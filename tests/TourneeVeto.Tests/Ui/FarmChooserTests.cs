using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Components;
using TourneeVeto.Ui.Data;

namespace TourneeVeto.Tests.Ui;

/// <summary>Choix de la ferme du jour pour la grille de régie et la biosécurité (module visitStore.js simulé).</summary>
public class FarmChooserTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private readonly DemoDataSet data = DemoData.Generate(Today, DemoDataSeeder.Seed);
    private readonly BunitJSModuleInterop module;

    public FarmChooserTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 7, 30, 0, TimeSpan.Zero)));
        Services.AddTourneeVeto();
        module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult(data.Farms);
    }

    // Le repository n'implémente que IAsyncDisposable (il ferme le module JS) : libérer le contexte de façon asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    [Fact]
    public void Propose_uniquement_les_fermes_visitees_aujourd_hui_vers_l_ecran_demande()
    {
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult([data.Visits[2], data.Visits[0]]);

        var cut = Render<FarmChooser>(parameters => parameters.Add(p => p.BasePath, "biosecurite"));

        cut.WaitForAssertion(() => Assert.Equal(
            [$"biosecurite/{data.Farms[0].Id}", $"biosecurite/{data.Farms[2].Id}"],
            cut.FindAll("a.choice").Select(link => link.GetAttribute("href"))));
    }

    [Fact]
    public void Sans_visite_aujourd_hui_le_dit_clairement()
    {
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult([]);

        var cut = Render<FarmChooser>(parameters => parameters.Add(p => p.BasePath, "regie"));

        cut.WaitForAssertion(() => Assert.Equal("Aucune visite prévue aujourd'hui.", cut.Find(".status").TextContent));
    }
}
