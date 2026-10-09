using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TourneeVeto.Ui.Components;

namespace TourneeVeto.Tests.Ui;

/// <summary>Navigation principale d'après les maquettes : un lien par écran disponible, l'écran courant mis en avant.</summary>
public class AppNavTests : BunitContext
{
    [Fact]
    public void Propose_l_accueil_la_tournee_la_grille_de_regie_la_biosecurite_et_le_rapport()
    {
        var cut = Render<AppNav>();

        var links = cut.FindAll("a.item");
        Assert.Equal(["", "tournee", "regie", "biosecurite", "rapport"], links.Select(link => link.GetAttribute("href")));
        Assert.Equal(
            ["Accueil", "Tournée", "Grille Régie", "Biosécurité", "Rapport"],
            links.Select(link => link.QuerySelector(".label--short")!.TextContent.Trim()));
        Assert.Equal(
            ["Tableau de bord", "Tournée du jour", "Grille de régie", "Bilan biosécurité", "Rapports de visite"],
            links.Select(link => link.QuerySelector(".label--long")!.TextContent.Trim()));
    }

    [Fact]
    public void La_page_d_accueil_est_la_seule_mise_en_avant_a_la_racine()
    {
        var cut = Render<AppNav>();

        Assert.Equal(["Accueil"], cut.FindAll("a.item.active").Select(link => link.QuerySelector(".label--short")!.TextContent.Trim()));
    }

    [Fact]
    public void La_tournee_met_en_avant_son_onglet_et_pas_l_accueil()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("tournee");

        var cut = Render<AppNav>();

        Assert.Equal(["Tournée"], cut.FindAll("a.item.active").Select(link => link.QuerySelector(".label--short")!.TextContent.Trim()));
    }

    [Fact]
    public void La_grille_d_une_ferme_met_en_avant_l_onglet_grille_de_regie()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("regie/F001");

        var cut = Render<AppNav>();

        Assert.Equal(["Grille Régie"], cut.FindAll("a.item.active").Select(link => link.QuerySelector(".label--short")!.TextContent.Trim()));
    }

    [Fact]
    public void Le_rapport_d_une_ferme_met_en_avant_l_onglet_rapport()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("rapport/F001");

        var cut = Render<AppNav>();

        Assert.Equal(["Rapport"], cut.FindAll("a.item.active").Select(link => link.QuerySelector(".label--short")!.TextContent.Trim()));
    }

    [Fact]
    public void Chaque_onglet_a_un_libelle_visible_et_une_icone_decorative()
    {
        var cut = Render<AppNav>();

        Assert.All(cut.FindAll("a.item"), link =>
        {
            Assert.All(link.QuerySelectorAll(".label"), label => Assert.NotEmpty(label.TextContent.Trim()));
            Assert.Equal("true", link.QuerySelector("svg")!.GetAttribute("aria-hidden"));
        });
    }
}
