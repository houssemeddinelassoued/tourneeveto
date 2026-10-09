using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Showcase;

namespace TourneeVeto.Tests.Domain.Showcase;

/// <summary>Fiche fictive d'une vache de la grille de régie : déterministe, cohérente avec les données réelles de la vache.</summary>
public class CowShowcaseTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static readonly DemoDataSet Data = DemoData.Generate(Today, 2026);

    private static readonly IReadOnlyList<RegieItem> Items = DailyActions.Compute(Data.Cows.Where(cow => cow.FarmId == "F001"), Today).Items;

    private static CowShowcase Of(RegieItem item) => CowShowcaseBuilder.Build(item, Today);

    [Fact]
    public void Est_deterministe()
    {
        Assert.Equivalent(Of(Items[0]), Of(Items[0]), strict: true);
    }

    [Fact]
    public void L_identifiant_national_est_derive_du_numero_de_boucle_et_de_l_elevage()
    {
        var item = Items[0];

        var national = Of(item).NationalId;

        Assert.StartsWith("CA ", national);
        Assert.EndsWith($" {item.Cow.Id}", national);
        Assert.NotEqual(Of(Items[0] with { Cow = item.Cow with { FarmId = "F002" } }).NationalId, national);
    }

    [Fact]
    public void Le_dernier_point_de_la_courbe_CCS_est_le_dernier_CCS_reel()
    {
        var item = Items.First(entry => entry.Cow.LastSccThousands is not null);

        var trend = Of(item).SccTrend;

        Assert.Equal(6, trend.Count);
        Assert.Equal(item.Cow.LastSccThousands, trend[^1].Thousands);
        Assert.Equal(new DateOnly(2026, 10, 1), trend[^1].Month);
        Assert.Equal(new DateOnly(2026, 5, 1), trend[0].Month);
        Assert.All(trend, point => Assert.True(point.Thousands > 0));
    }

    [Fact]
    public void Sans_controle_la_courbe_part_d_une_valeur_de_repli_sous_le_seuil()
    {
        var item = Items[0] with { Cow = Items[0].Cow with { LastSccThousands = null } };

        var show = Of(item);

        Assert.True(show.SccTrend[^1].Thousands < show.SccAlertThousands);
    }

    [Fact]
    public void Le_seuil_d_alerte_suit_les_seuils_de_regie()
    {
        var show = CowShowcaseBuilder.Build(Items[0], Today, RegieThresholds.Default with { HighSccThousands = 250 });

        Assert.Equal(250, show.SccAlertThousands);
    }

    [Fact]
    public void Trois_mesures_compactes_et_trois_interventions_dans_le_passe()
    {
        foreach (var item in Items)
        {
            var show = Of(item);

            Assert.Equal(3, show.Measures.Count);
            Assert.Equal(3, show.Interventions.Count);
            Assert.All(show.Interventions, intervention => Assert.True(intervention.Date < Today));
            Assert.Equal(3, show.Surveillance.Tiles.Count);
        }
    }

    [Fact]
    public void Le_bloc_de_surveillance_suit_le_motif_principal()
    {
        var calving = Items.First(entry => entry.Motives.FirstOrDefault()?.Action == RegieAction.CalvingSoon);
        var scc = Items.First(entry => entry.Motives.FirstOrDefault()?.Action == RegieAction.HighScc);

        Assert.Equal("Surveillance périnatale & pré-vêlage", Of(calving).Surveillance.Title);
        Assert.Equal("Surveillance de la santé mammaire", Of(scc).Surveillance.Title);
        Assert.Contains("sp/mL", Of(scc).Measures[0].Value);
    }

    [Fact]
    public void Une_vache_tarie_n_a_pas_de_production()
    {
        var dry = Items[0] with { Cow = Items[0].Cow with { Status = ReproStatus.Dry } };

        Assert.Null(Of(dry).ProductionKg);
    }

    [Fact]
    public void Une_vache_sans_motif_a_quand_meme_une_fiche()
    {
        var item = new RegieItem(Items[0].Cow, [], Urgency.Warning, RegieAnomaly.MissingInsemination);

        var show = Of(item);

        Assert.Equal(3, show.Measures.Count);
        Assert.Contains("corriger", show.Statement, StringComparison.OrdinalIgnoreCase);
    }
}
