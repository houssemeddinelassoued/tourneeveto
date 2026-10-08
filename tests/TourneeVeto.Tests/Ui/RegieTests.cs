using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Data;
using TourneeVeto.Ui.Pages;

namespace TourneeVeto.Tests.Ui;

/// <summary>Page de la grille de régie, avec le module visitStore.js simulé (jamais la vraie base).</summary>
public class RegieTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private readonly DemoDataSet data = DemoData.Generate(Today, DemoDataSeeder.Seed);
    private readonly BunitJSModuleInterop module;

    public RegieTests()
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
    public void Affiche_la_ferme_et_ses_vaches_a_voir()
    {
        var farm = data.Farms[0];
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", farm.Id).SetResult([.. data.Cows.Where(cow => cow.FarmId == farm.Id)]);

        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, farm.Id));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("article.cow-card")));
        Assert.Equal($"{farm.Name} · {farm.Municipality}", cut.Find(".farm").TextContent);
    }

    [Fact]
    public void Filtre_DG_n_affiche_que_les_vaches_a_diagnostiquer()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult(data.Cows);
        var expected = DailyActions.Compute(data.Cows, Today).Items
            .Where(item => item.Motives.Any(motive => motive.Action == RegieAction.PregnancyCheck))
            .Select(item => item.Cow.Id)
            .ToList();

        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));
        var filterDg = cut.WaitForElement("button.filter:contains('DG')");
        filterDg.Click();

        Assert.Equal(DemoData.PregnancyChecksDue + DemoData.PregnancyChecksOverdue, expected.Count);
        Assert.Equal($"DG ({expected.Count})", cut.Find("button[aria-pressed=true]").TextContent.Trim());
        Assert.Equal(expected, cut.FindAll(".cow-card__number-value").Select(number => number.TextContent));
    }

    [Fact]
    public void Filtre_Tous_reaffiche_toute_la_grille()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult(data.Cows);
        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));
        cut.WaitForElement("button.filter:contains('DG')").Click();

        cut.Find("button.filter:contains('Tous')").Click();

        Assert.Equal(DailyActions.Compute(data.Cows, Today).Items.Count, cut.FindAll("article.cow-card").Count);
    }

    [Fact]
    public void Etat_vide_quand_aucune_vache_n_a_de_motif()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult([]);

        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));

        cut.WaitForAssertion(() => Assert.Equal("Aucune action de régie pour cette visite.", cut.Find(".status").TextContent));
        Assert.Empty(cut.FindAll("button.filter"));
        Assert.Equal("false", cut.Find("section").GetAttribute("aria-busy"));
    }

    [Fact]
    public void Ferme_inconnue_affiche_ferme_introuvable_sans_lire_de_vaches()
    {
        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F999"));

        cut.WaitForAssertion(() => Assert.Equal("Ferme introuvable.", cut.Find("[role=alert]").TextContent));
        Assert.Empty(module.Invocations["getCowsByFarm"]);
    }

    [Fact]
    public void Erreur_de_stockage_est_affichee_sans_planter()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001")
            .SetException(new JSException("TOURNEEVETO_STORAGE:Unavailable:InvalidStateError navigation privée"));

        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));

        cut.WaitForAssertion(() => Assert.Equal(
            "Stockage local indisponible : les données ne peuvent être ni lues ni enregistrées.",
            cut.Find("[role=alert]").TextContent));
    }

    [Fact]
    public void Seuils_invalides_affichent_un_avertissement()
    {
        Services.AddSingleton(RegieThresholds.Default with { HighSccThousands = 0 });
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult(data.Cows);

        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));

        cut.WaitForAssertion(() => Assert.Equal(
            "Configuration des seuils invalide : valeurs par défaut utilisées.",
            cut.Find("[role=status]").TextContent));
    }
}
