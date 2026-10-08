using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Ui.Formatting;

/// <summary>Libellés des résultats saisis, selon le motif (« Positif » pour un DG, « Tarie » pour un tarissement…).</summary>
public static class ResultLabels
{
    public static string For(RegieAction action, ResultOutcome outcome) => (action, outcome) switch
    {
        (RegieAction.DryOff, ResultOutcome.Done) => "Tarie",
        (RegieAction.CalvingSoon, ResultOutcome.Done) => "Vêlée",
        (RegieAction.CalvingSoon, ResultOutcome.Postponed) => "Pas encore",
        (RegieAction.HighScc, ResultOutcome.Positive) => "CMT positif",
        (RegieAction.HighScc, ResultOutcome.Negative) => "CMT négatif",
        (RegieAction.NotInseminated, ResultOutcome.Done) => "Vue",
        (_, ResultOutcome.Done) => "Fait",
        (_, ResultOutcome.Postponed) => "Reportée",
        (_, ResultOutcome.Positive) => "Positif",
        (_, ResultOutcome.Negative) => "Négatif",
        (_, ResultOutcome.Doubtful) => "Douteux",
        (_, ResultOutcome.Normal) => "Normal",
        (_, ResultOutcome.Abnormal) => "Anormal",
        _ => outcome.ToString(),
    };
}
