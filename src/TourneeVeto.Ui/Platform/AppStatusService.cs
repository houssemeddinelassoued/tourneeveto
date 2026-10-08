using Microsoft.JSInterop;

namespace TourneeVeto.Ui.Platform;

/// <summary>
/// Suit la connexion, la disponibilité hors ligne et les mises à jour via le module isolé wwwroot/js/appStatus.js.
/// Les composants s'abonnent à <see cref="Changed"/> ; aucun n'appelle le JS directement.
/// </summary>
public sealed class AppStatusService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    public const string ModulePath = "./_content/TourneeVeto.Ui/js/appStatus.js";

    private Task<IJSObjectReference>? module;
    private Task? watching;
    private DotNetObjectReference<AppStatusService>? selfReference;

    public AppStatus Current { get; private set; } = AppStatus.Unknown;

    /// <summary>Déclenché à chaque changement d'état (connexion, mise en cache, nouvelle version).</summary>
    public event Action? Changed;

    /// <summary>Démarre le suivi ; les appels suivants partagent le même démarrage.</summary>
    public Task StartAsync() => watching ??= WatchAsync();

    /// <summary>Active la nouvelle version et recharge l'application ; faux si aucune version n'attend.</summary>
    public async Task<bool> ApplyUpdateAsync()
    {
        var store = await GetModuleAsync();
        return await store.InvokeAsync<bool>("applyUpdate");
    }

    /// <summary>Appelé par appStatus.js.</summary>
    [JSInvokable]
    public void OnStatusChanged(AppStatus status)
    {
        if (status == Current)
        {
            return;
        }

        Current = status;
        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (module is { IsCompletedSuccessfully: true })
        {
            try
            {
                await module.Result.InvokeVoidAsync("unwatch");
                await module.Result.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Le circuit ou la WebView est déjà fermé : il n'y a plus rien à libérer côté JS.
            }
        }

        selfReference?.Dispose();
    }

    private async Task WatchAsync()
    {
        selfReference = DotNetObjectReference.Create(this);
        var store = await GetModuleAsync();
        OnStatusChanged(await store.InvokeAsync<AppStatus>("watch", selfReference));
    }

    private Task<IJSObjectReference> GetModuleAsync() =>
        module ??= jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
}
