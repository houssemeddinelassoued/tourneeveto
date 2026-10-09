using TourneeVeto.Domain.Showcase;

namespace TourneeVeto.Ui.Formatting;

/// <summary>Libellés français du rapport de visite (constats, pratiques, heures).</summary>
public static class ReportLabels
{
    /// <summary>Mention du taux de gestation (maquette : « Réussite »).</summary>
    public static string RateVerdict(int? ratePercent) => ratePercent switch
    {
        null => "Non saisi",
        >= 70 => "Réussite",
        >= 50 => "Correct",
        _ => "À surveiller",
    };

    /// <summary>Teinte du taux de gestation (classes CSS ok, warning, urgent, neutral).</summary>
    public static string RateTone(int? ratePercent) => ratePercent switch
    {
        null => "neutral",
        >= 70 => "ok",
        >= 50 => "warning",
        _ => "urgent",
    };

    /// <summary>« Vache A », « Vaches A et B » ou « Vaches A, B et C ».</summary>
    public static string CowList(IReadOnlyList<string> ids) => ids.Count switch
    {
        0 => string.Empty,
        1 => $"Vache {ids[0]}",
        _ => $"Vaches {string.Join(", ", ids.Take(ids.Count - 1))} et {ids[^1]}",
    };

    /// <summary>Texte d'un constat, avec un repli lisible tant que rien n'est saisi.</summary>
    public static string FindingText(ReportFinding finding)
    {
        if (finding.Planned == 0)
        {
            return "Aucune vache concernée dans la grille de régie.";
        }

        if (!finding.HasData)
        {
            return $"Aucun résultat saisi : {finding.Planned} {(finding.Planned > 1 ? "vaches prévues" : "vache prévue")} dans la grille.";
        }

        return finding.Count == 0
            ? $"Aucun cas parmi les {finding.Entered} {(finding.Entered > 1 ? "résultats saisis" : "résultat saisi")}."
            : $"{CowList(finding.CowIds)}.";
    }

    /// <summary>Texte du bilan de reproduction.</summary>
    public static string ReproductionText(ReproductionSummary repro) => !repro.HasData
        ? repro.Planned == 0
            ? "Aucun diagnostic de gestation prévu dans la grille de régie."
            : $"Aucun diagnostic saisi : {repro.Planned} {(repro.Planned > 1 ? "vaches à contrôler" : "vache à contrôler")}."
        : $"{repro.Seen} {(repro.Seen > 1 ? "diagnostics saisis" : "diagnostic saisi")} sur {repro.Planned} prévu(s).";

    /// <summary>« 3 Pratiques prioritaires prescrites » ; sans pratique, le titre seul.</summary>
    public static string PracticesTitle(int count) => count switch
    {
        0 => "Pratiques prioritaires prescrites",
        1 => "1 Pratique prioritaire prescrite",
        _ => $"{count} Pratiques prioritaires prescrites",
    };

    /// <summary>Heure à deux chiffres : « 09:55 ».</summary>
    public static string Clock(TimeOnly time) => time.ToString("HH':'mm", System.Globalization.CultureInfo.InvariantCulture);
}
