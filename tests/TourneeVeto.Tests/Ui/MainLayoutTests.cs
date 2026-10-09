using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TourneeVeto.Domain;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Data;
using TourneeVeto.Ui.Layout;
using TourneeVeto.Ui.Platform;

namespace TourneeVeto.Tests.Ui;

/// <summary>Coque des maquettes : barre du haut, barre latérale, navigation, recherche, contenu atteignable au clavier.</summary>
public class MainLayoutTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    public MainLayoutTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 7, 30, 0, TimeSpan.Zero)));
        Services.AddTourneeVeto();
        ShowcaseJs.Setup(this, Today);
        var module = JSInterop.SetupModule(AppStatusService.ModulePath);
        module.Setup<AppStatus>("watch", _ => true).SetResult(new AppStatus(Online: true, OfflineReady: true, UpdateAvailable: false));
        module.SetupVoid("unwatch").SetVoidResult();
    }

    // AppStatusService n'implémente que IAsyncDisposable (il libère le module JS) : libération asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<h1>Page</h1>"))));

    private IRenderedComponent<MainLayout> RenderLoadedLayout()
    {
        var cut = RenderLayout();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("option").Count));
        return cut;
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void Contient_l_en_tete_la_navigation_et_le_contenu()
    {
        var cut = RenderLayout();

        Assert.All(cut.FindAll(".brand-name"), name => Assert.Equal("TournéeVéto", name.TextContent));
        Assert.Equal("Navigation principale", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("Page", cut.Find("main#contenu h1").TextContent);
        cut.WaitForAssertion(() => Assert.Contains("En ligne · prêt hors ligne", cut.Find(".root--pill").TextContent));
    }

    [Fact]
    public void Garde_la_mention_donnees_fictives_et_la_banniere_de_mise_a_jour()
    {
        var cut = RenderLayout();

        Assert.Contains(cut.FindAll(".brand-tag"), tag => tag.TextContent == "Données fictives");
        Assert.NotNull(cut.Find(".banner"));
    }

    [Fact]
    public void La_barre_laterale_montre_la_synchronisation_reelle_et_le_praticien()
    {
        var cut = RenderLayout();

        cut.WaitForAssertion(() => Assert.Equal("Synchronisé", cut.Find(".root--card .eyebrow").TextContent));
        Assert.Contains("Élevages & cheptels à jour", cut.Find(".root--card").TextContent);
        Assert.Equal("Dre Camille Exemple", cut.Find(".profile-name").TextContent);
        Assert.Equal("Praticien ruminants", cut.Find(".profile-title").TextContent);
    }

    [Fact]
    public void La_barre_du_haut_montre_le_secteur_et_propose_la_recherche()
    {
        var cut = RenderLoadedLayout();

        Assert.StartsWith("Secteur : ", cut.Find(".chip--sector").TextContent);
        Assert.Contains("Mode poste clinique actif", cut.Find(".chip--mode").TextContent);
        Assert.Equal("search", cut.Find("form.search").GetAttribute("role"));
        Assert.Equal("global-search", cut.Find("label.sr").GetAttribute("for"));
    }

    [Fact]
    public void La_recherche_par_numero_de_vache_mene_a_la_grille_filtree_de_son_elevage()
    {
        var cut = RenderLoadedLayout();
        var cow = DemoData.Generate(Today, DemoDataSeeder.Seed).Cows.First(candidate => candidate.FarmId == "F002");

        cut.Find("#global-search").Input(cow.Id);
        cut.Find("form.search").Submit();

        cut.WaitForAssertion(() => Assert.EndsWith($"/regie/F002?q={cow.Id}", Navigation.Uri));
    }

    [Fact]
    public void La_recherche_par_nom_d_elevage_ouvre_sa_grille()
    {
        var cut = RenderLoadedLayout();

        cut.Find("#global-search").Input("érable");
        cut.Find("form.search").Submit();

        cut.WaitForAssertion(() => Assert.EndsWith("/regie/F003", Navigation.Uri));
    }

    [Fact]
    public void Le_bouton_de_deconnexion_est_simule()
    {
        var cut = RenderLayout();

        cut.Find("button[aria-label^='Se déconnecter']").Click();

        Assert.Contains("Démonstration", cut.Find("p.notice").TextContent);
    }

    [Fact]
    public void Le_lien_d_evitement_vise_le_contenu_de_la_page_courante()
    {
        var cut = RenderLayout();

        Assert.EndsWith("#contenu", cut.Find("a.skip").GetAttribute("href"));
        Assert.StartsWith("http", cut.Find("a.skip").GetAttribute("href"));
    }
}
