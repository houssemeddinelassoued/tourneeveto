using Microsoft.JSInterop;

namespace TourneeVeto.Ui.Platform;

/// <summary>Lance l'impression du rapport via le module isolé wwwroot/js/printReport.js (story 10.2).</summary>
public sealed class ReportPrinter(IJSRuntime jsRuntime) : IAsyncDisposable
{
    public const string ModulePath = "./_content/TourneeVeto.Ui/js/printReport.js";

    private Task<IJSObjectReference>? module;

    public async Task PrintAsync()
    {
        var printer = await GetModuleAsync();
        await printer.InvokeVoidAsync("printPage");
    }

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
                // L'hôte est déjà fermé : rien à libérer côté JS.
            }
        }
    }

    private Task<IJSObjectReference> GetModuleAsync() =>
        module ??= jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
}
