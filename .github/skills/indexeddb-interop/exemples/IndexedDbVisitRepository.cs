// Copie de référence du skill indexeddb-interop : réunit les types de src/TourneeVeto.Ui/Data/
// (IVisitRepository.cs, StorageFailure.cs, StorageUnavailableException.cs, IndexedDbVisitRepository.cs).
// Enregistrement : services.AddTourneeVeto() (src/TourneeVeto.Ui/ServiceCollectionExtensions.cs).

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Domain;

namespace TourneeVeto.Ui.Data;

/// <summary>
/// Accès aux élevages, aux visites, aux vaches et aux photos stockés sur l'appareil (ADR 0001, skill indexeddb-interop).
/// Toute méthode peut lever <see cref="StorageUnavailableException"/>.
/// </summary>
public interface IVisitRepository
{
    /// <summary>Enregistre le jeu de démonstration en une seule transaction, uniquement si la base est vide.</summary>
    /// <returns><c>true</c> si le jeu a été enregistré, <c>false</c> si la base contenait déjà des données.</returns>
    Task<bool> SeedIfEmptyAsync(DemoDataSet data, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Farm>> GetFarmsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Visit>> GetVisitsByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<Visit?> GetVisitAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveVisitAsync(Visit visit, CancellationToken cancellationToken = default);

    /// <summary>Supprime la visite, ses saisies, son bilan, ses recommandations et ses photos.</summary>
    Task DeleteVisitAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cow>> GetCowsByFarmAsync(string farmId, CancellationToken cancellationToken = default);

    /// <summary>Ajoute ou met à jour des vaches ; une vache est identifiée par son élevage et son numéro.</summary>
    Task SaveCowsAsync(IReadOnlyList<Cow> cows, CancellationToken cancellationToken = default);

    /// <summary>Remplace tout le troupeau de l'élevage en une seule transaction ; en cas d'échec, l'ancien troupeau reste intact.</summary>
    Task ReplaceCowsAsync(string farmId, IReadOnlyList<Cow> cows, CancellationToken cancellationToken = default);

    /// <summary>Saisies de la visite (une par vache ayant un résultat ou une note).</summary>
    Task<IReadOnlyList<CowVisitRecord>> GetVisitRecordsAsync(Guid visitId, CancellationToken cancellationToken = default);

    /// <summary>Enregistre la saisie d'une vache ; une saisie existante pour la même visite et la même vache est remplacée.</summary>
    Task SaveVisitRecordAsync(CowVisitRecord record, CancellationToken cancellationToken = default);

    /// <summary>Réponses au bilan de biosécurité de la visite ; <c>null</c> si aucune.</summary>
    Task<BiosecurityAnswers?> GetBiosecurityAsync(Guid visitId, CancellationToken cancellationToken = default);

    Task SaveBiosecurityAsync(BiosecurityAnswers answers, CancellationToken cancellationToken = default);

    /// <summary>Recommandations de la visite ; <c>null</c> si aucune n'a encore été enregistrée.</summary>
    Task<VisitRecommendations?> GetRecommendationsAsync(Guid visitId, CancellationToken cancellationToken = default);

    /// <summary>Enregistre la liste complète des recommandations de la visite, en remplaçant la précédente.</summary>
    Task SaveRecommendationsAsync(VisitRecommendations recommendations, CancellationToken cancellationToken = default);

    /// <summary>Redimensionne et stocke la photo choisie dans <paramref name="fileInput"/> ; <c>null</c> si aucun fichier.</summary>
    Task<Guid?> AddPhotoFromInputAsync(Guid visitId, ElementReference fileInput, CancellationToken cancellationToken = default);

    Task DeletePhotoAsync(Guid photoId, CancellationToken cancellationToken = default);
}

/// <summary>Cause d'une indisponibilité du stockage local.</summary>
public enum StorageFailure
{
    /// <summary>Espace de stockage plein (QuotaExceededError).</summary>
    QuotaExceeded,

    /// <summary>Base inaccessible : navigation privée, base bloquée par un autre onglet, IndexedDB absent.</summary>
    Unavailable,
}

/// <summary>Le stockage local a refusé l'opération ; l'interface affiche <see cref="Exception.Message"/> et conserve la saisie à l'écran.</summary>
public sealed class StorageUnavailableException(StorageFailure failure, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public StorageFailure Failure { get; } = failure;
}

/// <summary>Implémentation IndexedDB de <see cref="IVisitRepository"/>, via le module isolé wwwroot/js/visitStore.js.</summary>
public sealed class IndexedDbVisitRepository(IJSRuntime jsRuntime) : IVisitRepository, IAsyncDisposable
{
    public const string ModulePath = "./_content/TourneeVeto.Ui/js/visitStore.js";

    private const string ErrorPrefix = "TOURNEEVETO_STORAGE:";

    private Task<IJSObjectReference>? module;

    public Task<bool> SeedIfEmptyAsync(DemoDataSet data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        return InvokeAsync<bool>("seedIfEmpty", cancellationToken, data.Farms, data.Cows, data.Visits);
    }

    public Task<IReadOnlyList<Farm>> GetFarmsAsync(CancellationToken cancellationToken = default) =>
        InvokeAsync<IReadOnlyList<Farm>>("getFarms", cancellationToken);

    public Task<IReadOnlyList<Visit>> GetVisitsByDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        InvokeAsync<IReadOnlyList<Visit>>("getVisitsByDate", cancellationToken, date);

    public Task<Visit?> GetVisitAsync(Guid id, CancellationToken cancellationToken = default) =>
        InvokeAsync<Visit?>("getVisit", cancellationToken, id);

    public Task SaveVisitAsync(Visit visit, CancellationToken cancellationToken = default) =>
        InvokeVoidAsync("putVisit", cancellationToken, visit);

    public Task DeleteVisitAsync(Guid id, CancellationToken cancellationToken = default) =>
        InvokeVoidAsync("deleteVisit", cancellationToken, id);

    public Task<IReadOnlyList<Cow>> GetCowsByFarmAsync(string farmId, CancellationToken cancellationToken = default) =>
        InvokeAsync<IReadOnlyList<Cow>>("getCowsByFarm", cancellationToken, farmId);

    public Task SaveCowsAsync(IReadOnlyList<Cow> cows, CancellationToken cancellationToken = default) =>
        InvokeVoidAsync("putCows", cancellationToken, cows);

    public Task ReplaceCowsAsync(string farmId, IReadOnlyList<Cow> cows, CancellationToken cancellationToken = default) =>
        InvokeVoidAsync("replaceCows", cancellationToken, farmId, cows);

    public Task<IReadOnlyList<CowVisitRecord>> GetVisitRecordsAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        InvokeAsync<IReadOnlyList<CowVisitRecord>>("getVisitRecords", cancellationToken, visitId);

    public Task SaveVisitRecordAsync(CowVisitRecord record, CancellationToken cancellationToken = default) =>
        InvokeVoidAsync("putVisitRecord", cancellationToken, record);

    public Task<BiosecurityAnswers?> GetBiosecurityAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        InvokeAsync<BiosecurityAnswers?>("getBiosecurity", cancellationToken, visitId);

    public Task SaveBiosecurityAsync(BiosecurityAnswers answers, CancellationToken cancellationToken = default) =>
        InvokeVoidAsync("putBiosecurity", cancellationToken, answers);

    public Task<VisitRecommendations?> GetRecommendationsAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        InvokeAsync<VisitRecommendations?>("getRecommendations", cancellationToken, visitId);

    public Task SaveRecommendationsAsync(VisitRecommendations recommendations, CancellationToken cancellationToken = default) =>
        InvokeVoidAsync("putRecommendations", cancellationToken, recommendations);

    public Task<Guid?> AddPhotoFromInputAsync(Guid visitId, ElementReference fileInput, CancellationToken cancellationToken = default) =>
        InvokeAsync<Guid?>("addPhotoFromInput", cancellationToken, visitId, fileInput);

    public Task DeletePhotoAsync(Guid photoId, CancellationToken cancellationToken = default) =>
        InvokeVoidAsync("deletePhoto", cancellationToken, photoId);

    public async ValueTask DisposeAsync()
    {
        if (module is { IsCompletedSuccessfully: true })
        {
            try
            {
                await module.Result.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Le circuit ou la WebView est déjà fermé : il n'y a plus rien à libérer côté JS.
            }
        }
    }

    private async Task<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, params object?[] arguments)
    {
        try
        {
            var store = await GetModuleAsync();
            return await store.InvokeAsync<T>(identifier, cancellationToken, arguments);
        }
        catch (JSException exception) when (exception.Message.Contains(ErrorPrefix, StringComparison.Ordinal))
        {
            throw ToStorageException(exception);
        }
    }

    private async Task InvokeVoidAsync(string identifier, CancellationToken cancellationToken, params object?[] arguments)
    {
        try
        {
            var store = await GetModuleAsync();
            await store.InvokeVoidAsync(identifier, cancellationToken, arguments);
        }
        catch (JSException exception) when (exception.Message.Contains(ErrorPrefix, StringComparison.Ordinal))
        {
            throw ToStorageException(exception);
        }
    }

    // Import paresseux, une seule fois ; un import échoué (ex. hors ligne avant mise en cache) est retenté à l'appel suivant.
    private Task<IJSObjectReference> GetModuleAsync()
    {
        if (module is null || module.IsFaulted || module.IsCanceled)
        {
            module = jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        }

        return module;
    }

    private static StorageUnavailableException ToStorageException(JSException exception)
    {
        var failure = exception.Message.Contains($"{ErrorPrefix}Quota", StringComparison.Ordinal)
            ? StorageFailure.QuotaExceeded
            : StorageFailure.Unavailable;
        var message = failure == StorageFailure.QuotaExceeded
            ? "Enregistrement impossible : espace de stockage plein."
            : "Stockage local indisponible : les données ne peuvent être ni lues ni enregistrées.";
        return new StorageUnavailableException(failure, message, exception);
    }
}
