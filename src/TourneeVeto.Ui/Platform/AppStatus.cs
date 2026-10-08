namespace TourneeVeto.Ui.Platform;

/// <summary>État de l'application dans le navigateur.</summary>
/// <param name="Online">Réseau disponible.</param>
/// <param name="OfflineReady">Application en cache : elle fonctionnera sans réseau (story 3.2).</param>
/// <param name="UpdateAvailable">Nouvelle version téléchargée, en attente de confirmation (story 3.3).</param>
public sealed record AppStatus(bool Online, bool OfflineReady, bool UpdateAvailable)
{
    public static AppStatus Unknown { get; } = new(Online: true, OfflineReady: false, UpdateAvailable: false);
}
