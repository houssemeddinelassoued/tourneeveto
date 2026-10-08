using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using static TourneeVeto.Tests.Domain.Regie.RegieTestCows;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Story #27 : vache vide (non inséminée) plus de 60 jours après le vêlage.</summary>
public class OpenCowRuleTests
{
    [Theory]
    [InlineData(60, false)]
    [InlineData(61, true)]
    [InlineData(120, true)]
    public void Vache_vide_est_signalee_apres_60_jours(int daysSinceCalving, bool expected)
    {
        var cow = Neutral() with { Status = ReproStatus.Open, LastCalving = DaysAgo(daysSinceCalving), LastInsemination = null };

        var motive = Motive(Grid(cow), RegieAction.NotInseminated);

        Assert.Equal(expected, motive is not null);
        if (motive is not null)
        {
            Assert.Equal(Urgency.Warning, motive.Urgency);
            Assert.Equal(daysSinceCalving, motive.Days);
        }
    }

    [Fact]
    public void Vache_inseminee_depuis_le_velage_n_est_pas_vide()
    {
        var cow = Neutral() with { Status = ReproStatus.Bred, LastCalving = DaysAgo(90), LastInsemination = DaysAgo(10) };

        Assert.Null(Motive(Grid(cow), RegieAction.NotInseminated));
    }

    [Fact]
    public void Generisse_vide_n_est_pas_signalee()
    {
        var heifer = Neutral() with { Lactation = 0, Status = ReproStatus.Open, LastCalving = null, LastInsemination = null, LastSccThousands = null };

        Assert.Null(Item(Grid(heifer)));
    }

    [Fact]
    public void Velage_posterieur_a_la_visite_est_une_anomalie_sans_motif_de_velage()
    {
        var cow = Neutral() with { Status = ReproStatus.Open, LastCalving = Today.AddDays(3), LastInsemination = null };

        var item = Item(Grid(cow));

        Assert.NotNull(item);
        Assert.Equal(RegieAnomaly.InconsistentDate, item.Anomaly);
        Assert.DoesNotContain(item.Motives, motive => motive.Action is RegieAction.NotInseminated or RegieAction.PostCalvingCheck);
    }
}
