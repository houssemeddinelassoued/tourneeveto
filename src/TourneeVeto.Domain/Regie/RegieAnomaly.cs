namespace TourneeVeto.Domain.Regie;

/// <summary>Donnée incohérente qui empêche de calculer certains motifs (voir PRODUCT.md, « Motifs de la grille de régie »).</summary>
public enum RegieAnomaly
{
    /// <summary>Insémination ou vêlage postérieurs à la date de la visite.</summary>
    InconsistentDate,

    /// <summary>Vache gestante ou tarie sans date d'insémination : vêlage prévu incalculable.</summary>
    MissingInsemination,
}
