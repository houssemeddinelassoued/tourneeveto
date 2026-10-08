using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TourneeVeto.Ui.Components;

namespace TourneeVeto.Tests.Ui;

/// <summary>Navigation principale d'après les maquettes : un lien par écran disponible, l'écran courant mis en avant.</summary>
public class AppNavTests : BunitContext
{
    [Fact]
    public void Propose_la_tournee_et_la_grille_de_regie()
    {
        var cut = Render<AppNav>();

        var links = cut.FindAll("a.item");
        Assert.Equal(["Tournée du jour", "Grille de régie"], links.Select(link => link.TextContent.Trim()));
        Assert.Equal(["", "regie"], links.Select(link => link.GetAttribute("href")));
    }

    [Fact]
    public void La_grille_d_une_ferme_met_en_avant_l_onglet_grille_de_regie()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("regie/F001");

        var cut = Render<AppNav>();

        Assert.Equal(["Grille de régie"], cut.FindAll("a.item.active").Select(link => link.TextContent.Trim()));
    }

    [Fact]
    public void Chaque_onglet_a_un_libelle_visible_et_une_icone_decorative()
    {
        var cut = Render<AppNav>();

        Assert.All(cut.FindAll("a.item"), link =>
        {
            Assert.NotEmpty(link.QuerySelector(".label")!.TextContent.Trim());
            Assert.Equal("true", link.QuerySelector("svg")!.GetAttribute("aria-hidden"));
        });
    }
}
