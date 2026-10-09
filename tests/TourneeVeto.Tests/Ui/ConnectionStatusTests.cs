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

    [Theory]
    [InlineData(true, true, "En ligne")]
    [InlineData(false, true, "Hors-ligne (Prêt)")]
    [InlineData(false, false, "Hors-ligne")]
    public void La_pastille_compacte_garde_le_texte_complet_pour_les_lecteurs_d_ecran(bool online, bool offlineReady, string compact)
    {
        var cut = Render<ConnectionStatus>(parameters => parameters.Add(p => p.Variant, "pill"));

        Services.GetRequiredService<AppStatusService>().OnStatusChanged(new AppStatus(online, offlineReady, UpdateAvailable: false));

        cut.WaitForAssertion(() => Assert.Equal(compact, cut.Find(".label").TextContent));
        Assert.Equal("true", cut.Find(".label").GetAttribute("aria-hidden"));
        Assert.NotEmpty(cut.Find(".sr").TextContent);
    }

    [Fact]
    public void La_carte_synchronise_reflete_le_vrai_statut()
    {
        var cut = Render<ConnectionStatus>(parameters => parameters.Add(p => p.Variant, "card"));

        Services.GetRequiredService<AppStatusService>().OnStatusChanged(new AppStatus(Online: false, OfflineReady: true, UpdateAvailable: false));
        cut.WaitForAssertion(() => Assert.Equal("Synchronisé", cut.Find(".eyebrow").TextContent));
        Assert.Equal("Élevages & cheptels à jour", cut.Find(".detail").TextContent);
        Assert.Equal("Hors ligne · prêt", cut.Find(".full").TextContent);

        Services.GetRequiredService<AppStatusService>().OnStatusChanged(new AppStatus(Online: true, OfflineReady: false, UpdateAvailable: false));
        cut.WaitForAssertion(() => Assert.Equal("Synchronisation…", cut.Find(".eyebrow").TextContent));
    }
}
