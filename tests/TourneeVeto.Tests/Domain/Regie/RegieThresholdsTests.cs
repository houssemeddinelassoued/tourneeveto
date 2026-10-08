using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using static TourneeVeto.Tests.Domain.Regie.RegieTestCows;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Story #28 : seuils configurables, valeurs par défaut de PRODUCT.md, repli sur ces valeurs si un seuil est invalide.</summary>
public class RegieThresholdsTests
{
    [Fact]
    public void Valeurs_par_defaut_sont_celles_de_PRODUCT()
    {
        var thresholds = RegieThresholds.Default;

        Assert.Equal(
            [280, 14, 60, 30, 45, 21, 35, 60, 200],
            [thresholds.GestationDays, thresholds.CalvingSoonDays, thresholds.DryOffDaysBeforeCalving,
             thresholds.PregnancyCheckFromDays, thresholds.PregnancyCheckToDays, thresholds.PostCalvingFromDays,
             thresholds.PostCalvingToDays, thresholds.OpenAfterCalvingDays, thresholds.HighSccThousands]);
        Assert.True(thresholds.IsValid);
    }

    [Theory]
    [InlineData(34, false)]
    [InlineData(35, true)]
    public void Seuil_modifie_change_la_grille(int daysSinceInsemination, bool expected)
    {
        var cow = Neutral() with { Status = ReproStatus.Bred, LastInsemination = DaysAgo(daysSinceInsemination), LastCalving = DaysAgo(120) };
        var thresholds = RegieThresholds.Default with { PregnancyCheckFromDays = 35 };

        var grid = DailyActions.Compute([cow], Today, thresholds);

        Assert.Equal(expected, grid.Items.SelectMany(item => item.Motives).Any(motive => motive.Action == RegieAction.PregnancyCheck));
        Assert.False(grid.UsesDefaultThresholds);
    }

    public static TheoryData<RegieThresholds> InvalidThresholds =>
    [
        RegieThresholds.Default with { PregnancyCheckFromDays = -1 },
        RegieThresholds.Default with { HighSccThousands = 0 },
        RegieThresholds.Default with { PregnancyCheckFromDays = 50, PregnancyCheckToDays = 45 },
        RegieThresholds.Default with { PostCalvingFromDays = 40, PostCalvingToDays = 35 },
        RegieThresholds.Default with { DryOffDaysBeforeCalving = 300 },
    ];

    [Theory]
    [MemberData(nameof(InvalidThresholds))]
    public void Seuil_invalide_ramene_les_valeurs_par_defaut(RegieThresholds invalid)
    {
        var cow = Neutral() with { Status = ReproStatus.Bred, LastInsemination = DaysAgo(30), LastCalving = DaysAgo(120) };

        var grid = DailyActions.Compute([cow], Today, invalid);

        Assert.False(invalid.IsValid);
        Assert.True(grid.UsesDefaultThresholds);
        Assert.Equal(Urgency.Info, Assert.Single(grid.Items).Urgency);
    }
}
