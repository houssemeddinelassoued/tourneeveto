using TourneeVeto.Domain.Biosecurity;

namespace TourneeVeto.Ui.Formatting;

/// <summary>Libellés français du bilan de biosécurité.</summary>
public static class BiosecurityLabels
{
    public static IReadOnlyList<Answer> Answers { get; } = [Domain.Biosecurity.Answer.Yes, Domain.Biosecurity.Answer.Partial, Domain.Biosecurity.Answer.No, Domain.Biosecurity.Answer.NotApplicable];

    public static string AnswerText(Answer answer) => answer switch
    {
        Domain.Biosecurity.Answer.Yes => "Oui",
        Domain.Biosecurity.Answer.Partial => "Partiel",
        Domain.Biosecurity.Answer.No => "Non",
        _ => "Sans objet",
    };

    /// <summary>Libellé court des boutons (maquette : OUI, PARTIEL, NON, S.O.).</summary>
    public static string ShortAnswerText(Answer answer) => answer == Domain.Biosecurity.Answer.NotApplicable ? "S.O." : AnswerText(answer).ToUpperInvariant();

    public static string Risk(RiskLevel level) => level switch
    {
        RiskLevel.Low => "Risque faible",
        RiskLevel.Moderate => "Risque modéré",
        RiskLevel.High => "Risque élevé",
        _ => "Non évaluée",
    };

    public static string Score(SectionResult section) =>
        section.Score is int score ? $"{Risk(section.Level)} · {score}/100" : Risk(section.Level);

    /// <summary>Teinte d'un niveau de risque (classes CSS ok, warning, urgent, neutral).</summary>
    public static string Tone(RiskLevel level) => level switch
    {
        RiskLevel.Low => "ok",
        RiskLevel.Moderate => "warning",
        RiskLevel.High => "urgent",
        _ => "neutral",
    };
}
