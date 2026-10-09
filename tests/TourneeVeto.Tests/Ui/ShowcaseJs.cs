using Bunit;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui.Data;

namespace TourneeVeto.Tests.Ui;

/// <summary>Simule le module visitStore.js avec le jeu de démonstration (lectures de l'accueil et de la coque).</summary>
internal static class ShowcaseJs
{
    public static DemoDataSet Setup(BunitContext context, DateOnly today)
    {
        var data = DemoData.Generate(today, DemoDataSeeder.Seed);
        var module = context.JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult(data.Farms);
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", today).SetResult(data.Visits);
        foreach (var farm in data.Farms)
        {
            module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", farm.Id).SetResult([.. data.Cows.Where(cow => cow.FarmId == farm.Id)]);
        }

        return data;
    }
}
