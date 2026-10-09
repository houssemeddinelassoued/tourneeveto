using TourneeVeto.Domain.Regie;

namespace TourneeVeto.Domain.Showcase;

/// <summary>Sélection des vaches du protocole actif d'un élevage (fonction pure).</summary>
public static class FarmProtocol
{
    /// <summary>
    /// Vaches du protocole : celles dont un diagnostic de gestation est à faire ; à défaut, toute la grille de régie.
    /// L'ordre de la grille (urgence décroissante, puis numéro) est conservé.
    /// </summary>
    public static IReadOnlyList<RegieItem> Select(RegieGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var checks = grid.Items.Where(item => item.Motives.Any(motive => motive.Action == RegieAction.PregnancyCheck)).ToList();
        return checks.Count > 0 ? checks : [.. grid.Items];
    }
}
