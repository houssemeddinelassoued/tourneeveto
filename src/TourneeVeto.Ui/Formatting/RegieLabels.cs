using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;

namespace TourneeVeto.Ui.Formatting;

/// <summary>Libellés français de la grille de régie, communs à la grille, à la fiche d'une vache et à la tournée.</summary>
public static class RegieLabels
{
    public static string Action(RegieAction action) => action switch
    {
        RegieAction.CalvingSoon => "Vêlage prévu",
        RegieAction.PregnancyCheck => "DG",
        RegieAction.DryOff => "Tarissement",
        RegieAction.HighScc => "CCS élevé",
        RegieAction.PostCalvingCheck => "Post-vêlage",
        RegieAction.NotInseminated => "Vache vide",
        _ => action.ToString(),
    };

    /// <summary>Échéance d'un motif (« IA J+35 », « Vêlage J-3 »…), à partir des jours calculés par le domaine.</summary>
    public static string? Due(RegieMotive? motive) => motive switch
    {
        { Action: RegieAction.PregnancyCheck, Days: int days, IsOverdue: true } => $"IA J+{days} · DG en retard",
        { Action: RegieAction.PregnancyCheck, Days: int days } => $"IA J+{days}",
        { Action: RegieAction.DryOff or RegieAction.CalvingSoon, Days: int days } when days >= 0 => $"Vêlage J-{days}",
        { Action: RegieAction.DryOff or RegieAction.CalvingSoon, Days: int days } => $"Vêlage dépassé de {-days} j",
        { Action: RegieAction.PostCalvingCheck or RegieAction.NotInseminated, Days: int days } => $"Vêlage J+{days}",
        _ => null,
    };

    /// <summary>Pastille d'échéance d'une carte : l'échéance du motif, ou le dernier CCS pour un CCS élevé (« 850k sp/mL »).</summary>
    public static string? Pill(RegieMotive? motive, Cow cow) =>
        Due(motive) ?? (motive?.Action == RegieAction.HighScc && cow.LastSccThousands is int scc ? $"{scc}k sp/mL" : null);

    public static string Urgency(Urgency urgency) => urgency switch
    {
        Domain.Regie.Urgency.Urgent => "Urgent",
        Domain.Regie.Urgency.Warning => "À surveiller",
        Domain.Regie.Urgency.Ok => "Normal",
        _ => "À faire",
    };

    public static string? Anomaly(RegieAnomaly? anomaly) => anomaly switch
    {
        RegieAnomaly.InconsistentDate => "Date incohérente",
        RegieAnomaly.MissingInsemination => "Date d'IA manquante",
        _ => null,
    };
}
