namespace TourneeVeto.Ui.Data;

/// <summary>Cause d'une indisponibilité du stockage local.</summary>
public enum StorageFailure
{
    /// <summary>Espace de stockage plein (QuotaExceededError).</summary>
    QuotaExceeded,

    /// <summary>Base inaccessible : navigation privée, base bloquée par un autre onglet, IndexedDB absent.</summary>
    Unavailable,
}
