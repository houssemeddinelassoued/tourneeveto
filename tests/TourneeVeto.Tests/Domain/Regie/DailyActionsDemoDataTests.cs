using TourneeVeto.Domain;
using TourneeVeto.Domain.Regie;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Story 2.2 : le jeu de démonstration fournit au moins une vache par motif, en quantités exactes, quel que soit le jour.</summary>
public class DailyActionsDemoDataTests
{
    public static TheoryData<DateOnly, int> DaysAndSeeds => new()
    {
        { new DateOnly(2026, 10, 8), 42 },
        { new DateOnly(2028, 2, 29), 2026 },
        { new DateOnly(2026, 12, 31), 7 },
        { new DateOnly(2027, 1, 1), 123456 },
    };

    [Theory]
    [MemberData(nameof(DaysAndSeeds))]
    public void Chaque_motif_a_le_nombre_de_vaches_annonce_par_DemoData(DateOnly today, int seed)
    {
        var grid = DailyActions.Compute(DemoData.Generate(today, seed).Cows, today);
        var motives = grid.Items.SelectMany(item => item.Motives).ToList();

        int Count(RegieAction action, bool overdue = false) => motives.Count(motive => motive.Action == action && motive.IsOverdue == overdue);

        Assert.Equal(DemoData.CalvingsDueWithin14Days, Count(RegieAction.CalvingSoon));
        Assert.Equal(DemoData.PregnancyChecksDue, Count(RegieAction.PregnancyCheck));
        Assert.Equal(DemoData.PregnancyChecksOverdue, Count(RegieAction.PregnancyCheck, overdue: true));
        Assert.Equal(DemoData.DryOffsDue, Count(RegieAction.DryOff));
        Assert.Equal(DemoData.HighSccCows, Count(RegieAction.HighScc));
        Assert.Equal(DemoData.PostCalvingChecksDue, Count(RegieAction.PostCalvingCheck));
        Assert.Equal(DemoData.OpenCowsOverdue, Count(RegieAction.NotInseminated));
        Assert.All(grid.Items, item => Assert.Null(item.Anomaly));
    }
}
