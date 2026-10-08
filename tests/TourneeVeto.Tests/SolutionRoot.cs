namespace TourneeVeto.Tests;

/// <summary>Localise la racine du dépôt (dossier contenant TourneeVeto.slnx) depuis le dossier de sortie des tests.</summary>
internal static class SolutionRoot
{
    public static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TourneeVeto.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("TourneeVeto.slnx introuvable au-dessus du dossier de test.");
    }
}
