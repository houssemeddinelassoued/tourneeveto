using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Showcase;

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

    /// <summary>Verdict de l'indice global (maquette : « Favorable »).</summary>
    public static string Verdict(RiskLevel level) => level switch
    {
        RiskLevel.Low => "Favorable",
        RiskLevel.Moderate => "À améliorer",
        RiskLevel.High => "Défavorable",
        _ => "Non évalué",
    };

    public static string Status(SectionStatus status) => status switch
    {
        SectionStatus.Compliant => "Conforme",
        SectionStatus.InProgress => "En cours",
        SectionStatus.Vigilance => "Point de vigilance",
        SectionStatus.NotEvaluated => "Non évaluée",
        _ => "À auditer",
    };

    /// <summary>Texte d'un onglet de rubrique : « 65 % · En cours », ou l'état seul sans score.</summary>
    public static string StatusScore(SectionResult section, SectionStatus status) =>
        section.Score is int score ? $"{score} % · {Status(status)}" : Status(status);

    public static string StatusTone(SectionStatus status) => status switch
    {
        SectionStatus.Compliant => "ok",
        SectionStatus.InProgress => "info",
        SectionStatus.Vigilance => "urgent",
        _ => "neutral",
    };

    /// <summary>Circonférence de l'anneau (rayon 42).</summary>
    public const double RingCircumference = 2 * Math.PI * 42;

    /// <summary>Longueur de l'arc de l'anneau pour un score sur 100 (attribut stroke-dasharray, point décimal).</summary>
    public static string RingDash(int? score) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{RingCircumference * Math.Clamp(score ?? 0, 0, 100) / 100:0.##} {RingCircumference:0.##}");

    /// <summary>Teinte d'un niveau de risque (classes CSS ok, warning, urgent, neutral).</summary>
    public static string Tone(RiskLevel level) => level switch
    {
        RiskLevel.Low => "ok",
        RiskLevel.Moderate => "warning",
        RiskLevel.High => "urgent",
        _ => "neutral",
    };
}
