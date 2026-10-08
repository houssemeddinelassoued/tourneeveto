using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Vaches d'essai pour les règles de régie, dérivées du jeu de démonstration (seed fixe) et datées par rapport à <see cref="Today"/>.</summary>
internal static class RegieTestCows
{
    public static readonly DateOnly Today = new(2026, 10, 8);

    private static readonly Cow Template = DemoData.Generate(Today, seed: 42).Cows[0];

    /// <summary>Vache sans aucun motif : gestante confirmée, vêlage prévu dans 180 jours, vêlée il y a 400 jours, CCS normal.</summary>
    public static Cow Neutral(string id = "9001") => Template with
    {
        Id = id,
        Lactation = 2,
        Status = ReproStatus.Pregnant,
        LastInsemination = DaysAgo(100),
        LastCalving = DaysAgo(400),
        LastSccThousands = 100,
    };

    public static DateOnly DaysAgo(int days) => Today.AddDays(-days);

    /// <summary>Insémination qui place le vêlage prévu dans <paramref name="daysToCalving"/> jours.</summary>
    public static DateOnly InseminationForCalvingIn(int daysToCalving) => Today.AddDays(daysToCalving - RegieThresholds.Default.GestationDays);

    public static RegieGrid Grid(params Cow[] cows) => DailyActions.Compute(cows, Today);

    public static RegieItem? Item(RegieGrid grid, string id = "9001") => grid.Items.SingleOrDefault(item => item.Cow.Id == id);

    public static RegieMotive? Motive(RegieGrid grid, RegieAction action, string id = "9001") =>
        Item(grid, id)?.Motives.SingleOrDefault(motive => motive.Action == action);
}
