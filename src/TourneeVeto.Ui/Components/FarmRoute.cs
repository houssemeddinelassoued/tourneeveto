namespace TourneeVeto.Ui.Components;

/// <summary>Lit l'écran et l'élevage courants dans une adresse relative (ex. « regie/F001?q=bella »).</summary>
public static class FarmRoute
{
    private static readonly string[] FarmSections = ["regie", "biosecurite", "rapport"];

    /// <summary>
    /// Écran propre à un élevage (« regie », « biosecurite » ou « rapport ») et identifiant de l'élevage ;
    /// l'import d'un troupeau (« fermes/F001/import ») compte comme la grille de régie. Valeurs <c>null</c> sinon.
    /// </summary>
    public static (string? Section, string? FarmId) Parse(string relativePath)
    {
        var path = relativePath.Split('?', '#')[0].Trim('/');
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return (null, null);
        }

        if (segments[0] == "fermes")
        {
            return ("regie", segments.Length > 1 ? segments[1] : null);
        }

        return FarmSections.Contains(segments[0]) ? (segments[0], segments.Length > 1 ? segments[1] : null) : (null, null);
    }
}
