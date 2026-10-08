using TourneeVeto.Domain.Herd;

namespace TourneeVeto.Domain.Regie;

/// <summary>Un motif de régie pour une vache.</summary>
/// <param name="Action">Motif.</param>
/// <param name="Urgency">Urgence propre à ce motif.</param>
/// <param name="Days">
/// Jours depuis l'insémination (DG), jusqu'au vêlage prévu (tarissement, vêlage prévu ; négatif si dépassé),
/// depuis le vêlage (post-vêlage, vache vide) ; <c>null</c> pour le CCS.
/// </param>
/// <param name="IsOverdue">DG en retard (au-delà de la fenêtre).</param>
public sealed record RegieMotive(RegieAction Action, Urgency Urgency, int? Days, bool IsOverdue = false);

/// <summary>Ligne de la grille : une vache, ses motifs triés du plus urgent au moins urgent, et son urgence globale.</summary>
public sealed record RegieItem(Cow Cow, IReadOnlyList<RegieMotive> Motives, Urgency Urgency, RegieAnomaly? Anomaly);

/// <summary>Grille de régie d'un troupeau à une date donnée.</summary>
/// <param name="Items">Lignes triées par urgence décroissante, puis par numéro de vache.</param>
/// <param name="UsesDefaultThresholds">Vrai si les seuils fournis étaient invalides et ont été remplacés par les valeurs par défaut.</param>
public sealed record RegieGrid(IReadOnlyList<RegieItem> Items, bool UsesDefaultThresholds);
