using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Domain.Reports;

/// <summary>Résultat d'un motif de régie ; <paramref name="Outcome"/> est <c>null</c> si rien n'a été saisi.</summary>
public sealed record ReportResult(RegieAction Action, ResultOutcome? Outcome);

/// <summary>Ligne du rapport : une vache de la grille, ses motifs avec leur résultat et sa note.</summary>
public sealed record ReportLine(Cow Cow, Urgency Urgency, IReadOnlyList<ReportResult> Results, string Note)
{
    /// <summary>Vue : au moins un résultat saisi. Sinon la vache est « Non vue ».</summary>
    public bool IsSeen => Results.Any(result => result.Outcome is not null);
}

/// <summary>Rapport d'une visite (story 10.1).</summary>
/// <param name="Biosecurity">Bilan de biosécurité ; <c>null</c> s'il n'a pas été réalisé.</param>
public sealed record VisitReport(string FarmName, DateOnly Date, IReadOnlyList<ReportLine> Lines, BiosecurityResult? Biosecurity)
{
    public const string Disclaimer = "Données fictives — règles simplifiées";

    public bool HasBiosecurity => Biosecurity is not null;

    public int SeenCount => Lines.Count(line => line.IsSeen);

    public int NotSeenCount => Lines.Count - SeenCount;
}

/// <summary>Construit le rapport de visite à partir de la grille, des saisies et du bilan (fonction pure).</summary>
public static class VisitReports
{
    public static VisitReport Build(
        string farmName,
        DateOnly date,
        RegieGrid grid,
        IEnumerable<CowVisitRecord> records,
        BiosecurityResult? biosecurity)
    {
        ArgumentNullException.ThrowIfNull(farmName);
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(records);

        var byCow = records.GroupBy(record => record.CowId).ToDictionary(group => group.Key, group => group.Last());
        var lines = grid.Items.Select(item =>
        {
            var record = byCow.GetValueOrDefault(item.Cow.Id);
            var results = item.Motives.Select(motive => new ReportResult(motive.Action, record?.ResultFor(motive.Action))).ToList();
            return new ReportLine(item.Cow, item.Urgency, results, record?.Note ?? string.Empty);
        }).ToList();

        // Un bilan sans aucune réponse équivaut à un bilan non réalisé.
        return new VisitReport(farmName, date, lines, biosecurity is { AnsweredCount: > 0 } ? biosecurity : null);
    }
}
