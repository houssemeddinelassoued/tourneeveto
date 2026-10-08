using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Components;
using TourneeVeto.Ui.Platform;

namespace TourneeVeto.Tests.Ui;

/// <summary>Stories 3.2 et 3.3 : l'état de connexion et la disponibilité hors ligne sont écrits, pas seulement colorés.</summary>
public class ConnectionStatusTests : BunitContext, IAsyncLifetime
{
    public ConnectionStatusTests()
    {
        Services.AddTourneeVeto();
        var module = JSInterop.SetupModule(AppStatusService.ModulePath);
        module.Setup<AppStatus>("watch", _ => true).SetResult(new AppStatus(Online: true, OfflineReady: false, UpdateAvailable: false));
        module.SetupVoid("unwatch").SetVoidResult();
    }

    // AppStatusService n'implémente que IAsyncDisposable (il libère le module JS) : libération asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    [Theory]
    [InlineData(true, true, "En ligne · prêt hors ligne", "ok")]
    [InlineData(false, true, "Hors ligne · prêt", "ok")]
    [InlineData(true, false, "En ligne · mise en cache…", "warning")]
    [InlineData(false, false, "Hors ligne · non disponible", "urgent")]
    public void Etat_affiche_en_texte_selon_la_connexion_et_le_cache(bool online, bool offlineReady, string label, string tone)
    {
        var cut = Render<ConnectionStatus>();

        Services.GetRequiredService<AppStatusService>().OnStatusChanged(new AppStatus(online, offlineReady, UpdateAvailable: false));

        cut.WaitForAssertion(() => Assert.Equal(label, cut.Find(".label").TextContent));
        Assert.Contains($"root--{tone}", cut.Find("[role=status]").ClassList);
    }
}
