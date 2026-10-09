using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Showcase;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Ui.Components;

/// <summary>Une étape de la tournée telle que la page Tournée l'affiche.</summary>
/// <param name="Info">Données de terrain (réelles et fictives) de l'élevage.</param>
/// <param name="Visit">Visite du jour.</param>
/// <param name="Index">Rang dans la tournée complète, à partir de 0 (reste le même quand un filtre masque des visites).</param>
/// <param name="Items">Grille de régie de l'élevage : les vaches à voir.</param>
/// <param name="Progress">Avancement de la saisie.</param>
public sealed record TourStop(FarmShowcase Info, Visit Visit, int Index, IReadOnlyList<RegieItem> Items, VisitProgress Progress)
{
    /// <summary>Vaches à voir aujourd'hui.</summary>
    public int ToSee => Items.Count;

    /// <summary>Vaches urgentes.</summary>
    public int Urgent => Items.Count(item => item.Urgency == Urgency.Urgent);

    /// <summary>Première étape de la tournée : la visite à démarrer.</summary>
    public bool IsNext => Index == 0;
}
