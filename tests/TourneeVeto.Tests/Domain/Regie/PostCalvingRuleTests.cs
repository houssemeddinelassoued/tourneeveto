using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using static TourneeVeto.Tests.Domain.Regie.RegieTestCows;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Story #27 : examen post-vêlage entre 21 et 35 jours après le vêlage.</summary>
public class PostCalvingRuleTests
{
    [Theory]
    [InlineData(20, false)]
    [InlineData(21, true)]
    [InlineData(35, true)]
    [InlineData(36, false)]
    public void Examen_post_velage_entre_21_et_35_jours(int daysSinceCalving, bool expected)
    {
        var cow = Neutral() with { Status = ReproStatus.Open, LastCalving = DaysAgo(daysSinceCalving), LastInsemination = null };

        var motive = Motive(Grid(cow), RegieAction.PostCalvingCheck);

        Assert.Equal(expected, motive is not null);
        if (motive is not null)
        {
            Assert.Equal(Urgency.Info, motive.Urgency);
            Assert.Equal(daysSinceCalving, motive.Days);
        }
    }
}
