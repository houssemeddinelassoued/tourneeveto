using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Data;
using TourneeVeto.Ui.Pages;

namespace TourneeVeto.Tests.Ui;

/// <summary>Page d'accueil : la tournée du jour est lue dans le stockage local (module JS simulé, jamais la vraie base).</summary>
public class HomeTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private readonly BunitJSModuleInterop module;

    public HomeTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 7, 30, 0, TimeSpan.Zero)));
        Services.AddTourneeVeto();
        module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
    }

    // Le repository n'implémente que IAsyncDisposable (il ferme le module JS) : libérer le contexte de façon asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    [Fact]
    public void Affiche_la_tournee_du_jour_lue_dans_le_stockage()
    {
        var data = DemoData.Generate(Today, DemoDataSeeder.Seed);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult(data.Farms);
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult([.. data.Visits.Reverse()]);

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal(
            data.Farms.Select(farm => farm.Name),
            cut.FindAll(".farm").Select(farm => farm.TextContent)));
        Assert.Equal("false", cut.Find("section").GetAttribute("aria-busy"));
        Assert.Equal(data.Farms.Select(farm => $"regie/{farm.Id}"), cut.FindAll("a.visit").Select(link => link.GetAttribute("href")));
    }

    [Fact]
    public void Indique_l_absence_de_visite()
    {
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult([]);
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult([]);

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal("Aucune visite prévue aujourd'hui.", cut.Find(".status").TextContent));
    }

    [Fact]
    public void Affiche_l_erreur_de_stockage_sans_planter()
    {
        module.Setup<bool>("seedIfEmpty", _ => true)
            .SetException(new JSException("TOURNEEVETO_STORAGE:Unavailable:InvalidStateError navigation privée"));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal(
            "Stockage local indisponible : les données ne peuvent être ni lues ni enregistrées.",
            cut.Find("[role=alert]").TextContent));
    }
}
