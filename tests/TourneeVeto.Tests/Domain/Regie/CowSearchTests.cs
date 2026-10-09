using TourneeVeto.Domain.Regie;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Recherche dans la grille de régie par numéro de boucle ou par nom.</summary>
public class CowSearchTests
{
    private static RegieGrid Grid() => RegieTestCows.Grid(
        RegieTestCows.Neutral("4812") with { Name = "Bella", LastSccThousands = 900 },
        RegieTestCows.Neutral("3904") with { Name = "Marguerite", LastSccThousands = 900 },
        RegieTestCows.Neutral("5120") with { Name = "Étoile", LastSccThousands = 900 });

    [Theory]
    [InlineData(null, 3)]
    [InlineData("", 3)]
    [InlineData("   ", 3)]
    [InlineData("48", 1)]
    [InlineData("bell", 1)]
    [InlineData("BELLA", 1)]
    [InlineData("etoile", 1)]      // insensible aux accents
    [InlineData("ÉTOILE", 1)]
    [InlineData(" 3904 ", 1)]
    [InlineData("zzz", 0)]
    public void Filtre_par_numero_ou_nom(string? query, int expected) =>
        Assert.Equal(expected, CowSearch.Filter(Grid().Items, query).Count());

    [Fact]
    public void L_ordre_de_la_grille_est_conserve()
    {
        var items = Grid().Items;

        Assert.Equal(items.Select(item => item.Cow.Id), CowSearch.Filter(items, "").Select(item => item.Cow.Id));
    }
}
