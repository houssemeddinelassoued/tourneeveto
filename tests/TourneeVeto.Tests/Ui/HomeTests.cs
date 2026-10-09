using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Pages;

namespace TourneeVeto.Tests.Ui;

/// <summary>Accueil : tableau de bord du praticien (mobile et poste), chiffres tirés de la régie, boutons externes simulés.</summary>
public class HomeTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private readonly DemoDataSet data;

    public HomeTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 7, 30, 0, TimeSpan.Zero)));
        Services.AddTourneeVeto();
        data = ShowcaseJs.Setup(this, Today);
    }

    // Le repository n'implémente que IAsyncDisposable (il ferme le module JS) : libérer le contexte de façon asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    private IRenderedComponent<Home> RenderLoaded()
    {
        var cut = Render<Home>();
        cut.WaitForAssertion(() => Assert.Equal("false", cut.Find("section.root").GetAttribute("aria-busy")));
        return cut;
    }

    private int CountOf(RegieAction action) =>
        data.Farms
            .SelectMany(farm => DailyActions.Compute(data.Cows.Where(cow => cow.FarmId == farm.Id), Today).Items)
            .Count(item => item.Motives.Any(motive => motive.Action == action));

    [Fact]
    public void Salue_la_veterinaire_avec_la_date_du_jour()
    {
        var cut = RenderLoaded();

        Assert.Equal("Jeudi 8 octobre 2026", cut.Find(".eyebrow").TextContent);
        Assert.Contains("Bonjour, Dre Camille Exemple", cut.Find("h1").TextContent);
        Assert.Single(cut.FindAll("h1"));
    }

    [Fact]
    public void Annonce_la_base_locale_et_le_nombre_d_elevages_reel()
    {
        var cut = RenderLoaded();

        Assert.Contains("Base locale prête · 3 élevages synchronisés", cut.Find(".ready").TextContent);
    }

    [Fact]
    public void La_prochaine_etape_mene_a_la_grille_de_regie_de_l_elevage()
    {
        var cut = RenderLoaded();

        var next = cut.Find("section.next");
        Assert.Contains("Étape 1 · 08h30", next.TextContent);
        Assert.Contains(data.Farms[0].Name, next.QuerySelector("h2")!.TextContent);
        Assert.Contains("Lancer le guidage (18 min)", next.TextContent);
        var link = next.QuerySelector("a")!;
        Assert.Equal("Ouvrir la fiche cheptel", link.TextContent.Trim());
        Assert.Equal($"regie/{data.Farms[0].Id}", link.GetAttribute("href"));
    }

    [Fact]
    public void Les_indicateurs_viennent_de_la_regie()
    {
        var cut = RenderLoaded();

        var values = cut.FindAll("ul.tiles .tile-value").Take(4).Select(tile => tile.TextContent);
        Assert.Equal(
            ["3", CountOf(RegieAction.PregnancyCheck).ToString(), CountOf(RegieAction.CalvingSoon).ToString(), CountOf(RegieAction.HighScc).ToString()],
            values);
    }

    [Fact]
    public void Les_outils_terrain_menent_aux_quatre_ecrans()
    {
        var cut = RenderLoaded();

        Assert.Equal(["tournee", "regie", "biosecurite", "rapport"], cut.FindAll("a.tool").Select(link => link.GetAttribute("href")));
    }

    [Fact]
    public void Le_materiel_de_bord_est_liste()
    {
        var cut = RenderLoaded();

        Assert.Contains(cut.FindAll(".equipment-item"), item => item.TextContent.Contains("Échographe portable") && item.TextContent.Contains("100 % prêt"));
    }

    [Fact]
    public void Le_tableau_de_bord_liste_le_plan_les_sujets_et_les_modules()
    {
        var cut = RenderLoaded();

        var steps = cut.FindAll("ol.plan > li");
        Assert.Equal(data.Farms.Count + 1, steps.Count);
        Assert.Contains("08h30", steps[0].TextContent);
        Assert.Contains("Urgence inscrite", steps[^1].TextContent);

        var subjects = cut.FindAll("li.subject");
        Assert.Equal(3, subjects.Count);
        Assert.All(cut.FindAll("li.subject a.link"), link => Assert.StartsWith("regie/F00", link.GetAttribute("href")));
        Assert.Contains("Ouvrir fiche animal", subjects[0].TextContent);

        Assert.Equal(["tournee", "regie", "biosecurite", "rapport"], cut.FindAll("a.module").Select(link => link.GetAttribute("href")));
        Assert.Equal(4, cut.FindAll("progress.bar").Count);
        Assert.NotNull(cut.Find("svg.map"));
    }

    [Fact]
    public void Un_bouton_externe_affiche_un_message_de_demonstration_sans_navigation()
    {
        var cut = RenderLoaded();

        Assert.Equal(string.Empty, cut.Find("p.demo").TextContent);
        cut.FindAll("button").First(button => button.TextContent.Contains("Lancer le guidage")).Click();

        Assert.Contains("Démonstration : fonction simulée, aucune donnée envoyée", cut.Find("p.demo").TextContent);
    }

    [Theory]
    [InlineData("+ Urgence d'élevage")]
    [InlineData("Préparer tournée J+1")]
    [InlineData("Export Hebdo")]
    [InlineData("Rappeler")]
    [InlineData("Naviguer")]
    public void Les_boutons_simules_existent(string label) =>
        Assert.Contains(RenderLoaded().FindAll("button"), button => button.TextContent.Trim() == label);
}
