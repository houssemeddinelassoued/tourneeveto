using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using static TourneeVeto.Tests.Domain.Regie.RegieTestCows;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Story #25 : diagnostic de gestation entre 30 et 45 jours après l'insémination, en retard au-delà.</summary>
public class PregnancyCheckRuleTests
{
    [Theory]
    [InlineData(29, null, false)]
    [InlineData(30, Urgency.Info, false)]
    [InlineData(45, Urgency.Info, false)]
    [InlineData(46, Urgency.Warning, true)]
    public void DG_est_propose_de_30_a_45_jours_puis_signale_en_retard(int daysSinceInsemination, Urgency? expected, bool overdue)
    {
        var cow = Neutral() with { Status = ReproStatus.Bred, LastInsemination = DaysAgo(daysSinceInsemination), LastCalving = DaysAgo(120) };

        var motive = Motive(Grid(cow), RegieAction.PregnancyCheck);

        Assert.Equal(expected, motive?.Urgency);
        if (motive is not null)
        {
            Assert.Equal(daysSinceInsemination, motive.Days);
            Assert.Equal(overdue, motive.IsOverdue);
        }
    }

    [Fact]
    public void Vache_deja_confirmee_gestante_n_a_pas_de_DG()
    {
        var cow = Neutral() with { Status = ReproStatus.Pregnant, LastInsemination = DaysAgo(35) };

        Assert.Null(Motive(Grid(cow), RegieAction.PregnancyCheck));
    }

    [Fact]
    public void Generisse_inseminee_a_aussi_son_DG()
    {
        var heifer = Neutral() with { Lactation = 0, LastCalving = null, Status = ReproStatus.Bred, LastInsemination = DaysAgo(32), LastSccThousands = null };

        Assert.Equal(Urgency.Info, Motive(Grid(heifer), RegieAction.PregnancyCheck)?.Urgency);
    }

    [Fact]
    public void Insemination_posterieure_a_la_visite_est_une_anomalie_sans_DG()
    {
        var cow = Neutral() with { Status = ReproStatus.Bred, LastInsemination = Today.AddDays(5), LastCalving = DaysAgo(120) };

        var item = Item(Grid(cow));

        Assert.NotNull(item);
        Assert.Equal(RegieAnomaly.InconsistentDate, item.Anomaly);
        Assert.DoesNotContain(item.Motives, motive => motive.Action == RegieAction.PregnancyCheck);
    }
}
