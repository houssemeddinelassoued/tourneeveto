using TourneeVeto.Domain;
using TourneeVeto.Domain.Showcase;

namespace TourneeVeto.Tests.Domain.Showcase;

/// <summary>Profil fictif d'un élevage pour la grille de régie, y compris hors tournée du jour.</summary>
public class FarmProfileTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static readonly DemoDataSet Data = DemoData.Generate(Today, 2026);

    [Fact]
    public void Un_elevage_de_la_tournee_garde_le_meme_profil_que_dans_la_tournee()
    {
        var day = ShowcaseBuilder.Build(Data, Today);

        foreach (var expected in day.Farms)
        {
            var profile = ShowcaseBuilder.ProfileOf(Data.Farms, Data.Visits, expected.FarmId, Today);

            Assert.Equivalent(expected, profile, strict: true);
        }
    }

    [Fact]
    public void Un_elevage_hors_tournee_a_un_profil_fictif_complet()
    {
        var farm = Data.Farms[0];
        var profile = ShowcaseBuilder.ProfileOf(Data.Farms, [], farm.Id, Today);

        Assert.NotNull(profile);
        Assert.Equal(farm.Name, profile.FarmName);
        Assert.Equal(farm.CowCount, profile.HerdSize);
        Assert.StartsWith("555-01", profile.Phone);
        Assert.False(string.IsNullOrWhiteSpace(profile.Farmer));
    }

    [Fact]
    public void Un_elevage_inconnu_n_a_pas_de_profil()
    {
        Assert.Null(ShowcaseBuilder.ProfileOf(Data.Farms, Data.Visits, "F999", Today));
    }
}
