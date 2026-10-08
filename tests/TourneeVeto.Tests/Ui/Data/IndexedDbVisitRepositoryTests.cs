using Bunit;
using Microsoft.JSInterop;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui.Data;

namespace TourneeVeto.Tests.Ui.Data;

/// <summary>Repository IndexedDB testé avec le module visitStore.js simulé par bUnit : jamais la vraie base.</summary>
public class IndexedDbVisitRepositoryTests : BunitContext
{
    private static readonly DateOnly Today = new(2026, 10, 8);
    private static readonly Visit SampleVisit = new(Guid.Parse("8f0c4a52-0c7e-4b8e-9d6a-3c1f2e5b7a90"), "F001", Today, "Suivi de reproduction", "", []);

    [Fact]
    public async Task Lit_les_visites_du_jour_via_le_module_isole()
    {
        var module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult([SampleVisit]);
        await using var repository = new IndexedDbVisitRepository(JSInterop.JSRuntime);

        var visits = await repository.GetVisitsByDateAsync(Today);

        Assert.Equal([SampleVisit], visits);
    }

    [Fact]
    public async Task Le_module_est_importe_une_seule_fois()
    {
        var module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult([]);
        module.Setup<Visit?>("getVisit", SampleVisit.Id).SetResult(SampleVisit);
        await using var repository = new IndexedDbVisitRepository(JSInterop.JSRuntime);

        await repository.GetFarmsAsync();
        await repository.GetVisitAsync(SampleVisit.Id);

        Assert.Single(JSInterop.Invocations["import"]);
    }

    [Fact]
    public async Task Le_jeu_de_demo_est_envoye_en_un_seul_appel()
    {
        var data = DemoData.Generate(Today, seed: 42);
        var module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(true);
        await using var repository = new IndexedDbVisitRepository(JSInterop.JSRuntime);

        var seeded = await repository.SeedIfEmptyAsync(data);

        Assert.True(seeded);
        var invocation = Assert.Single(module.Invocations["seedIfEmpty"]);
        Assert.Equal([data.Farms, data.Cows, data.Visits], invocation.Arguments);
    }

    [Fact]
    public async Task Remplacer_le_troupeau_envoie_la_ferme_et_les_vaches_en_un_seul_appel()
    {
        var cows = DemoData.Generate(Today, seed: 42).Cows.Where(cow => cow.FarmId == "F001").ToList();
        var module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.SetupVoid("replaceCows", _ => true).SetVoidResult();
        await using var repository = new IndexedDbVisitRepository(JSInterop.JSRuntime);

        await repository.ReplaceCowsAsync("F001", cows);

        var invocation = Assert.Single(module.Invocations["replaceCows"]);
        Assert.Equal(["F001", cows], invocation.Arguments);
    }

    [Fact]
    public async Task Quota_depasse_devient_StorageUnavailableException()
    {
        var module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.SetupVoid("putVisit", _ => true).SetException(new JSException("TOURNEEVETO_STORAGE:Quota:QuotaExceededError plein"));
        await using var repository = new IndexedDbVisitRepository(JSInterop.JSRuntime);

        var exception = await Assert.ThrowsAsync<StorageUnavailableException>(() => repository.SaveVisitAsync(SampleVisit));

        Assert.Equal(StorageFailure.QuotaExceeded, exception.Failure);
        Assert.Equal("Enregistrement impossible : espace de stockage plein.", exception.Message);
    }

    [Fact]
    public async Task Base_indisponible_devient_StorageUnavailableException()
    {
        var module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetException(new JSException("TOURNEEVETO_STORAGE:Unavailable:InvalidStateError navigation privée"));
        await using var repository = new IndexedDbVisitRepository(JSInterop.JSRuntime);

        var exception = await Assert.ThrowsAsync<StorageUnavailableException>(() => repository.GetFarmsAsync());

        Assert.Equal(StorageFailure.Unavailable, exception.Failure);
    }

    [Fact]
    public async Task Autre_erreur_JS_n_est_pas_masquee()
    {
        var module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.SetupVoid("deletePhoto", _ => true).SetException(new JSException("TypeError: bogue"));
        await using var repository = new IndexedDbVisitRepository(JSInterop.JSRuntime);

        await Assert.ThrowsAsync<JSException>(() => repository.DeletePhotoAsync(Guid.NewGuid()));
    }
}
