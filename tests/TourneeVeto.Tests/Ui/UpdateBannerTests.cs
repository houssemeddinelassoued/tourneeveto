using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Components;
using TourneeVeto.Ui.Platform;

namespace TourneeVeto.Tests.Ui;

/// <summary>Story 3.3 : mise à jour proposée, appliquée seulement après confirmation (module appStatus.js simulé).</summary>
public class UpdateBannerTests : BunitContext, IAsyncLifetime
{
    private readonly BunitJSModuleInterop module;

    public UpdateBannerTests()
    {
        Services.AddTourneeVeto();
        module = JSInterop.SetupModule(AppStatusService.ModulePath);
        module.SetupVoid("unwatch").SetVoidResult();
    }

    // AppStatusService n'implémente que IAsyncDisposable (il libère le module JS) : libération asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    [Fact]
    public void Rien_n_est_affiche_sans_nouvelle_version()
    {
        module.Setup<AppStatus>("watch", _ => true).SetResult(new AppStatus(Online: true, OfflineReady: true, UpdateAvailable: false));

        var cut = Render<UpdateBanner>();

        Assert.Empty(cut.FindAll("[role=status]"));
    }

    [Fact]
    public void Nouvelle_version_detectee_plus_tard_affiche_le_bandeau()
    {
        module.Setup<AppStatus>("watch", _ => true).SetResult(new AppStatus(true, true, false));
        var cut = Render<UpdateBanner>();

        Services.GetRequiredService<AppStatusService>().OnStatusChanged(new AppStatus(true, true, UpdateAvailable: true));

        cut.WaitForAssertion(() => Assert.Equal(
            "Nouvelle version disponible. Vos saisies sont enregistrées sur l'appareil.",
            cut.Find(".message").TextContent));
    }

    [Fact]
    public void La_mise_a_jour_n_est_appliquee_qu_apres_le_clic_sur_Recharger()
    {
        module.Setup<AppStatus>("watch", _ => true).SetResult(new AppStatus(true, true, UpdateAvailable: true));
        module.Setup<bool>("applyUpdate").SetResult(true);
        var cut = Render<UpdateBanner>();
        cut.WaitForElement("button.reload");
        Assert.Empty(module.Invocations["applyUpdate"]);

        cut.Find("button.reload").Click();

        Assert.Single(module.Invocations["applyUpdate"]);
        Assert.Equal("Rechargement…", cut.Find("button.reload").TextContent.Trim());
    }

    [Fact]
    public void Si_aucune_version_n_attend_plus_le_bouton_redevient_utilisable()
    {
        module.Setup<AppStatus>("watch", _ => true).SetResult(new AppStatus(true, true, UpdateAvailable: true));
        module.Setup<bool>("applyUpdate").SetResult(false);
        var cut = Render<UpdateBanner>();

        cut.WaitForElement("button.reload").Click();

        cut.WaitForAssertion(() => Assert.Equal("Recharger", cut.Find("button.reload").TextContent.Trim()));
    }
}
