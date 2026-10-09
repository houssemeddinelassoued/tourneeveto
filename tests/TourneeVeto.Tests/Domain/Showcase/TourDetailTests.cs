using TourneeVeto.Domain;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Showcase;

namespace TourneeVeto.Tests.Domain.Showcase;

/// <summary>Données fictives de la page Tournée : identifiants, protocole, historique, télémétrie, recherche et vaches du protocole.</summary>
public class TourDetailTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static readonly DemoDataSet Data = DemoData.Generate(Today, 2026);

    private static ShowcaseDay Build() => ShowcaseBuilder.Build(Data, Today);

    [Fact]
    public void Chaque_etape_porte_le_motif_reel_un_identifiant_et_un_protocole()
    {
        var farms = Build().Farms;

        Assert.Equal(Data.Visits.OrderBy(visit => visit.FarmId, StringComparer.Ordinal).Select(visit => visit.Reason), farms.Select(farm => farm.Reason));
        Assert.Equal(farms.Count, farms.Select(farm => farm.Identifier).Distinct().Count());
        Assert.All(farms, farm =>
        {
            Assert.StartsWith($"QC-{farm.FarmId}-", farm.Identifier);
            Assert.Contains(farm.HerdSize.ToString(), farm.Description);
            Assert.False(string.IsNullOrWhiteSpace(farm.MotiveDetail));
            Assert.False(string.IsNullOrWhiteSpace(farm.Footer));
        });

        Assert.Contains("PGF2α", farms[0].Protocol);
        Assert.Contains("PGF2α", farms[1].Protocol);   // « reproduction et biosécurité » : la reproduction l'emporte
        Assert.Contains("CCS", farms[2].Protocol);
    }

    [Fact]
    public void L_historique_est_date_avant_aujourd_hui_et_la_telemetrie_renseignee()
    {
        Assert.All(Build().Farms, farm =>
        {
            Assert.NotEmpty(farm.History);
            Assert.All(farm.History, entry =>
            {
                Assert.True(entry.Date < Today);
                Assert.False(string.IsNullOrWhiteSpace(entry.Title));
                Assert.False(string.IsNullOrWhiteSpace(entry.Badge));
            });
            Assert.True(farm.Telemetry.ProductionLitersPerDay > 0);
            Assert.True(farm.Telemetry.SccThousands > 0);
            Assert.False(string.IsNullOrWhiteSpace(farm.Telemetry.ActivityNote));
        });
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("  ", true)]
    [InlineData("érable", true)]
    [InlineData("ERABLE", true)]
    [InlineData("val-démonstration", true)]
    [InlineData("reproduction", true)]
    [InlineData("zzz", false)]
    public void La_recherche_ignore_la_casse_et_les_accents(string? query, bool expected)
    {
        var stops = Build().Farms;

        var matches = stops.Where(stop => StopSearch.Matches(stop, query)).Select(stop => stop.FarmName).ToList();

        Assert.Equal(expected, matches.Count > 0);
        if (query == "érable" || query == "ERABLE")
        {
            Assert.Equal(["Ferme de l'Érable Imaginaire"], matches);
        }
    }

    [Fact]
    public void La_recherche_porte_aussi_sur_l_eleveur_et_l_identifiant()
    {
        var stop = Build().Farms[0];

        Assert.True(StopSearch.Matches(stop, stop.Farmer));
        Assert.True(StopSearch.Matches(stop, stop.Identifier));
    }

    [Fact]
    public void Le_protocole_retient_les_vaches_a_diagnostic_de_gestation()
    {
        var farm = Data.Farms.First(candidate => DailyActions.Compute(Data.Cows.Where(cow => cow.FarmId == candidate.Id), Today)
            .Items.Any(item => item.Motives.Any(motive => motive.Action == RegieAction.PregnancyCheck)));
        var grid = DailyActions.Compute(Data.Cows.Where(cow => cow.FarmId == farm.Id), Today);

        var selected = FarmProtocol.Select(grid);

        Assert.NotEmpty(selected);
        Assert.All(selected, item => Assert.Contains(item.Motives, motive => motive.Action == RegieAction.PregnancyCheck));
    }

    [Fact]
    public void Sans_diagnostic_de_gestation_le_protocole_reprend_toute_la_grille()
    {
        var cow = Data.Cows[0] with { LastInsemination = null, Status = TourneeVeto.Domain.Herd.ReproStatus.Open, LastSccThousands = 900 };
        var grid = DailyActions.Compute([cow], Today);

        Assert.Equal(grid.Items, FarmProtocol.Select(grid));
    }
}
