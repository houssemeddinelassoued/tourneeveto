using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TourneeVeto.Domain;
using TourneeVeto.Ui.Components;

namespace TourneeVeto.Tests.Ui;

/// <summary>Sélecteur d'élevage compact de la barre du haut.</summary>
public class FarmSelectorTests : BunitContext
{
    private readonly DemoDataSet data = DemoData.Generate(new DateOnly(2026, 10, 8), 2026);

    [Fact]
    public void Propose_les_elevages_et_nomme_le_champ()
    {
        var cut = Render<FarmSelector>(parameters => parameters.Add(p => p.Farms, data.Farms));

        Assert.Equal("Élevage", cut.Find("select").GetAttribute("aria-label"));
        Assert.Equal(data.Farms.Select(farm => farm.Name), cut.FindAll("option").Select(option => option.TextContent));
    }

    [Fact]
    public void Changer_d_elevage_ouvre_sa_grille_de_regie_depuis_l_accueil()
    {
        var cut = Render<FarmSelector>(parameters => parameters.Add(p => p.Farms, data.Farms));

        cut.Find("select").Change(data.Farms[1].Id);

        Assert.EndsWith($"/regie/{data.Farms[1].Id}", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Changer_d_elevage_garde_l_ecran_courant()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"biosecurite/{data.Farms[0].Id}");
        var cut = Render<FarmSelector>(parameters => parameters.Add(p => p.Farms, data.Farms));

        cut.Find("select").Change(data.Farms[2].Id);

        Assert.EndsWith($"/biosecurite/{data.Farms[2].Id}", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Sans_elevage_rien_n_est_affiche()
    {
        var cut = Render<FarmSelector>();

        Assert.Empty(cut.FindAll("select"));
    }
}
