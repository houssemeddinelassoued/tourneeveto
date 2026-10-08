using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using static TourneeVeto.Tests.Domain.Regie.RegieTestCows;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Story #26 : tarissement quand le vêlage prévu (insémination + 280 jours) est dans 60 jours ou moins.</summary>
public class DryOffRuleTests
{
    [Theory]
    [InlineData(61, false)]
    [InlineData(60, true)]
    [InlineData(30, true)]
    [InlineData(10, true)]
    public void Tarissement_est_propose_60_jours_avant_le_velage(int daysToCalving, bool expected)
    {
        var cow = Neutral() with { LastInsemination = InseminationForCalvingIn(daysToCalving) };

        var motive = Motive(Grid(cow), RegieAction.DryOff);

        Assert.Equal(expected, motive is not null);
        if (motive is not null)
        {
            Assert.Equal(Urgency.Ok, motive.Urgency);
            Assert.Equal(daysToCalving, motive.Days);
        }
    }

    [Fact]
    public void Vache_deja_tarie_n_a_pas_de_tarissement()
    {
        var cow = Neutral() with { Status = ReproStatus.Dry, LastInsemination = InseminationForCalvingIn(40) };

        Assert.Null(Motive(Grid(cow), RegieAction.DryOff));
    }

    [Fact]
    public void Generisse_gestante_n_a_pas_de_tarissement()
    {
        var heifer = Neutral() with { Lactation = 0, LastCalving = null, LastSccThousands = null, LastInsemination = InseminationForCalvingIn(40) };

        Assert.Null(Motive(Grid(heifer), RegieAction.DryOff));
    }

    [Fact]
    public void Gestante_sans_date_d_insemination_est_une_anomalie()
    {
        var cow = Neutral() with { LastInsemination = null };

        var item = Item(Grid(cow));

        Assert.NotNull(item);
        Assert.Equal(RegieAnomaly.MissingInsemination, item.Anomaly);
        Assert.Empty(item.Motives);
    }
}
