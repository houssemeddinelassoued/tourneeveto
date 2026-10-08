using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using static TourneeVeto.Tests.Domain.Regie.RegieTestCows;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Story #26 : vêlage prévu signalé dans les 14 jours qui précèdent, date dépassée comprise.</summary>
public class CalvingSoonRuleTests
{
    [Theory]
    [InlineData(15, false)]
    [InlineData(14, true)]
    [InlineData(0, true)]
    [InlineData(-3, true)]
    public void Velage_prevu_est_signale_14_jours_avant(int daysToCalving, bool expected)
    {
        var cow = Neutral() with { Status = ReproStatus.Dry, LastInsemination = InseminationForCalvingIn(daysToCalving) };

        var motive = Motive(Grid(cow), RegieAction.CalvingSoon);

        Assert.Equal(expected, motive is not null);
        if (motive is not null)
        {
            Assert.Equal(Urgency.Warning, motive.Urgency);
            Assert.Equal(daysToCalving, motive.Days);
        }
    }

    [Fact]
    public void Gestante_non_tarie_proche_du_velage_a_les_deux_motifs()
    {
        var cow = Neutral() with { Status = ReproStatus.Pregnant, LastInsemination = InseminationForCalvingIn(10) };

        var item = Item(Grid(cow));

        Assert.NotNull(item);
        Assert.Equal([RegieAction.CalvingSoon, RegieAction.DryOff], item.Motives.Select(motive => motive.Action));
        Assert.Equal(Urgency.Warning, item.Urgency);
    }

    [Fact]
    public void Tarie_sans_date_d_insemination_est_une_anomalie()
    {
        var cow = Neutral() with { Status = ReproStatus.Dry, LastInsemination = null };

        Assert.Equal(RegieAnomaly.MissingInsemination, Item(Grid(cow))?.Anomaly);
    }
}
