using TourneeVeto.Domain.Herd;

namespace TourneeVeto.Ui.Formatting;

/// <summary>Libellés français des données du troupeau, communs aux pages (grille de régie, import).</summary>
public static class HerdLabels
{
    public static string Status(ReproStatus status) => status switch
    {
        ReproStatus.Open => "Vide",
        ReproStatus.Bred => "Inséminée",
        ReproStatus.Pregnant => "Gestante",
        ReproStatus.Dry => "Tarie",
        _ => status.ToString(),
    };
}
