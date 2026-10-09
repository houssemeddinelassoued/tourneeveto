using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Ui.Data;

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
