using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using static TourneeVeto.Tests.Domain.Regie.RegieTestCows;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Vêlage prévu = insémination + 280 jours (PRODUCT.md), avec des dates calendaires précises dont l'année bissextile 2028.</summary>
public class CalvingDateBoundaryTests
{
    private static RegieMotive? CalvingSoon(DateOnly insemination, DateOnly visitDay)
    {
        var cow = Neutral() with { Status = ReproStatus.Dry, LastInsemination = insemination, LastCalving = null };
        var grid = DailyActions.Compute([cow], visitDay);
        return Motive(grid, RegieAction.CalvingSoon);
    }

    [Theory]
    [InlineData("2029-03-09", 0)]
    [InlineData("2029-03-08", 1)]
    [InlineData("2029-03-10", -1)]
    public void Insemination_du_2_juin_2028_prevoit_le_velage_le_9_mars_2029(string visitDay, int expectedDays)
    {
        var motive = CalvingSoon(new DateOnly(2028, 6, 2), DateOnly.Parse(visitDay));

        Assert.NotNull(motive);
        Assert.Equal(expectedDays, motive.Days);
    }

    [Fact]
    public void Le_9_mars_2029_est_bien_280_jours_apres_le_2_juin_2028()
    {
        Assert.Equal(280, new DateOnly(2029, 3, 9).DayNumber - new DateOnly(2028, 6, 2).DayNumber);
    }

    [Theory]
    [InlineData("2028-08-26", 0)]
    [InlineData("2028-08-25", 1)]
    public void Gestation_a_cheval_sur_le_29_fevrier_2028_compte_le_jour_bissextile(string visitDay, int expectedDays)
    {
        // 2027-11-20 + 280 j = 2028-08-26 : le 29 février 2028 est compté dans la gestation.
        var motive = CalvingSoon(new DateOnly(2027, 11, 20), DateOnly.Parse(visitDay));

        Assert.NotNull(motive);
        Assert.Equal(expectedDays, motive.Days);
    }

    [Fact]
    public void Velage_prevu_n_est_pas_signale_15_jours_avant_en_annee_bissextile()
    {
        Assert.Null(CalvingSoon(new DateOnly(2027, 11, 20), new DateOnly(2028, 8, 11)));
        Assert.NotNull(CalvingSoon(new DateOnly(2027, 11, 20), new DateOnly(2028, 8, 12)));
    }
}
