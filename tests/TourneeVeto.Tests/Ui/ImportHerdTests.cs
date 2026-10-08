using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Data;
using TourneeVeto.Ui.Pages;

namespace TourneeVeto.Tests.Ui;

/// <summary>Page d'import du troupeau (issue #49), module visitStore.js simulé ; les scénarios complets arrivent au lot 3.</summary>
public class ImportHerdTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private readonly DemoDataSet data = DemoData.Generate(Today, DemoDataSeeder.Seed);
    private readonly BunitJSModuleInterop module;

    public ImportHerdTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 7, 30, 0, TimeSpan.Zero)));
        Services.AddTourneeVeto();
        module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult(data.Farms);
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult([.. data.Cows.Where(cow => cow.FarmId == "F001")]);
    }

    // Le repository n'implémente que IAsyncDisposable (il ferme le module JS) : libérer le contexte de façon asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    [Fact]
    public void Affiche_la_ferme_et_un_champ_fichier_avec_son_libelle()
    {
        var cut = Render<ImportHerd>(parameters => parameters.Add(p => p.FarmId, "F001"));

        cut.WaitForAssertion(() => Assert.Equal($"{data.Farms[0].Name} · {data.Farms[0].Municipality}", cut.Find(".farm").TextContent));
        Assert.Equal("herd-file", cut.Find("label.label").GetAttribute("for"));
        Assert.Equal(".csv,text/csv", cut.Find("input#herd-file").GetAttribute("accept"));
    }
}
