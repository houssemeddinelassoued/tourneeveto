using Bunit;
using Microsoft.AspNetCore.Components;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Layout;
using TourneeVeto.Ui.Platform;

namespace TourneeVeto.Tests.Ui;

/// <summary>Structure des maquettes : en-tête avec l'état hors ligne, navigation principale, contenu atteignable au clavier.</summary>
public class MainLayoutTests : BunitContext, IAsyncLifetime
{
    public MainLayoutTests()
    {
        Services.AddTourneeVeto();
        var module = JSInterop.SetupModule(AppStatusService.ModulePath);
        module.Setup<AppStatus>("watch", _ => true).SetResult(new AppStatus(Online: true, OfflineReady: true, UpdateAvailable: false));
        module.SetupVoid("unwatch").SetVoidResult();
    }

    // AppStatusService n'implémente que IAsyncDisposable (il libère le module JS) : libération asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<h1>Page</h1>"))));

    [Fact]
    public void Contient_l_en_tete_la_navigation_et_le_contenu()
    {
        var cut = RenderLayout();

        Assert.Equal("TournéeVéto", cut.Find(".brand-name").TextContent);
        Assert.Equal("Navigation principale", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("Page", cut.Find("main#contenu h1").TextContent);
        cut.WaitForAssertion(() => Assert.Equal("En ligne · prêt hors ligne", cut.Find(".label").TextContent));
    }

    [Fact]
    public void Le_lien_d_evitement_vise_le_contenu_de_la_page_courante()
    {
        var cut = RenderLayout();

        Assert.EndsWith("#contenu", cut.Find("a.skip").GetAttribute("href"));
        Assert.StartsWith("http", cut.Find("a.skip").GetAttribute("href"));
    }
}
