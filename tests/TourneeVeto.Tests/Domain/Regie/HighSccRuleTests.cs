using TourneeVeto.Domain.Regie;
using static TourneeVeto.Tests.Domain.Regie.RegieTestCows;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Story #28 : CCS élevé au-delà de 200 milliers de cellules/mL, urgent.</summary>
public class HighSccRuleTests
{
    [Theory]
    [InlineData(200, false)]
    [InlineData(201, true)]
    [InlineData(850, true)]
    public void CCS_superieur_a_200_est_urgent(int scc, bool expected)
    {
        var cow = Neutral() with { LastSccThousands = scc };

        var motive = Motive(Grid(cow), RegieAction.HighScc);

        Assert.Equal(expected, motive is not null);
        if (motive is not null)
        {
            Assert.Equal(Urgency.Urgent, motive.Urgency);
            Assert.Null(motive.Days);
        }
    }

    [Fact]
    public void Absence_de_controle_n_est_pas_un_CCS_eleve()
    {
        var cow = Neutral() with { LastSccThousands = null };

        Assert.Null(Item(Grid(cow)));
    }

    [Fact]
    public void CCS_eleve_reste_signale_malgre_une_date_incoherente()
    {
        var cow = Neutral() with { LastSccThousands = 400, LastInsemination = Today.AddDays(2) };

        var item = Item(Grid(cow));

        Assert.NotNull(item);
        Assert.Equal(RegieAnomaly.InconsistentDate, item.Anomaly);
        Assert.Equal(Urgency.Urgent, item.Urgency);
    }
}
