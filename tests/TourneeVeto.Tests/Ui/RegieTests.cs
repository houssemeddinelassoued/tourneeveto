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

/// <summary>Page de la grille de régie, avec le module visitStore.js simulé (jamais la vraie base).</summary>
public class RegieTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private readonly DemoDataSet data = DemoData.Generate(Today, DemoDataSeeder.Seed);
    private readonly BunitJSModuleInterop module;

    public RegieTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 7, 30, 0, TimeSpan.Zero)));
        Services.AddTourneeVeto();
        module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult(data.Farms);
    }

    // Le repository n'implémente que IAsyncDisposable (il ferme le module JS) : libérer le contexte de façon asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    [Fact]
    public void Affiche_la_ferme_et_ses_vaches_a_voir()
    {
        var farm = data.Farms[0];
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", farm.Id).SetResult([.. data.Cows.Where(cow => cow.FarmId == farm.Id)]);

        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, farm.Id));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("article.cow-card")));
        Assert.Equal($"{farm.Name} · {farm.Municipality}", cut.Find(".farm").TextContent);
    }
}
