using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Ui.Formatting;

/// <summary>Libellés français de la page Tournée : état des étapes, statuts de la liste compacte et action rapide.</summary>
public static class TourLabels
{
    /// <summary>État d'une étape sur tablette : « En cours », « Suivante » ou « En attente ».</summary>
    public static string State(VisitProgress progress, bool isNext) =>
        progress == VisitProgress.InProgress ? "En cours" : isNext ? "Suivante" : "En attente";

    /// <summary>Classe CSS de l'état d'une étape.</summary>
    public static string StateClass(VisitProgress progress, bool isNext) =>
        progress == VisitProgress.InProgress ? "state--progress" : isNext ? "state--next" : "state--waiting";

    /// <summary>Statut d'une étape dans la liste du poste : « Sur place », « Priorité haute » ou « En attente ».</summary>
    public static string DeskStatus(VisitProgress progress, int urgent) =>
        progress == VisitProgress.InProgress ? "Sur place" : urgent > 0 ? "Priorité haute" : "En attente";

    /// <summary>Classe CSS du statut d'une étape dans la liste du poste.</summary>
    public static string DeskStatusClass(VisitProgress progress, int urgent) =>
        progress == VisitProgress.InProgress ? "state--progress" : urgent > 0 ? "state--urgent" : "state--waiting";

    /// <summary>Stade de la vache dans le protocole (« IA J+35 »…), ou un tiret.</summary>
    public static string Stage(RegieItem item) => RegieLabels.Due(item.Motives.FirstOrDefault()) ?? "—";

    /// <summary>Statut prévu de la vache : son motif principal.</summary>
    public static string Planned(RegieItem item) => item.Motives.Count == 0 ? "—" : RegieLabels.Action(item.Motives[0].Action);

    /// <summary>Action rapide d'une ligne du protocole.</summary>
    public static string QuickAction(RegieItem item) =>
        item.Motives.Any(motive => motive.Action == RegieAction.PregnancyCheck) ? "Saisir DG" : "Voir la fiche";
}
