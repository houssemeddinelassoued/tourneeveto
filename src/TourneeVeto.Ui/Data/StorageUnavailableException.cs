namespace TourneeVeto.Ui.Data;

/// <summary>Le stockage local a refusé l'opération ; l'interface affiche <see cref="Exception.Message"/> et conserve la saisie à l'écran.</summary>
public sealed class StorageUnavailableException(StorageFailure failure, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public StorageFailure Failure { get; } = failure;
}
