using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Tests.Domain.Import;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Data;
using TourneeVeto.Ui.Pages;

namespace TourneeVeto.Tests.Ui;

/// <summary>Page d'import du troupeau (issue #49) : fichiers d'exemple fictifs, module visitStore.js simulé, jamais la vraie base.</summary>
public class ImportHerdTests : BunitContext, IAsyncLifetime
{
    private const string FarmId = ImportFixtures.FarmId;
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
        module.SetupVoid("putCows", _ => true).SetVoidResult();
        module.SetupVoid("replaceCows", _ => true).SetVoidResult();
    }

    // Le repository n'implémente que IAsyncDisposable (il ferme le module JS) : libérer le contexte de façon asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    private IRenderedComponent<ImportHerd> RenderWithHerd(IReadOnlyList<Cow> existingHerd)
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", FarmId).SetResult(existingHerd);
        var cut = Render<ImportHerd>(parameters => parameters.Add(p => p.FarmId, FarmId));
        cut.WaitForElement("input#herd-file");
        return cut;
    }

    private static void Upload(IRenderedComponent<ImportHerd> cut, byte[] content, string fileName) =>
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(content, fileName));

    private static string Text(IRenderedComponent<ImportHerd> cut, string selector) =>
        Regex.Replace(cut.Find(selector).TextContent.Trim(), @"\s+", " ");

    private IReadOnlyList<Cow> DemoHerd => [.. data.Cows.Where(cow => cow.FarmId == FarmId)];

    [Fact]
    public void Affiche_la_ferme_et_un_champ_fichier_avec_son_libelle()
    {
        var cut = RenderWithHerd(DemoHerd);

        Assert.Equal($"{data.Farms[0].Name} · {data.Farms[0].Municipality}", cut.Find(".farm").TextContent);
        Assert.Equal("herd-file", cut.Find("label.label").GetAttribute("for"));
        Assert.Equal(".csv,text/csv", cut.Find("input#herd-file").GetAttribute("accept"));
    }

    [Fact]
    public void Fichier_valide_de_60_lignes_affiche_60_vaches_puis_s_enregistre_a_la_confirmation()
    {
        var cut = RenderWithHerd(DemoHerd);

        Upload(cut, ImportFixtures.Bytes("troupeau-fictif.csv"), "troupeau-fictif.csv");

        cut.WaitForAssertion(() => Assert.Equal("60 vaches prêtes à importer.", Text(cut, ".summary")));
        Assert.Equal(60, cut.FindAll(".cows tbody tr").Count);
        Assert.Empty(module.Invocations["putCows"]);   // rien n'est enregistré avant la confirmation

        cut.Find("button.confirm").Click();

        cut.WaitForAssertion(() => Assert.StartsWith("Import enregistré : 60 ajoutée(s)", Text(cut, ".success")));
        var saved = Assert.IsAssignableFrom<IReadOnlyList<Cow>>(Assert.Single(module.Invocations["putCows"]).Arguments[0]);
        Assert.Equal(60, saved.Count);
        Assert.Empty(module.Invocations["replaceCows"]);
    }

    [Fact]
    public void Date_de_velage_invalide_ligne_12_est_signalee_et_les_autres_lignes_restent_importables()
    {
        var cut = RenderWithHerd(DemoHerd);

        Upload(cut, ImportFixtures.Bytes("troupeau-corrompu.csv"), "troupeau-corrompu.csv");

        cut.WaitForAssertion(() => Assert.Equal("59 vaches prêtes à importer, 1 ligne(s) ignorée(s).", Text(cut, ".summary")));
        Assert.Equal("ligne 12, date_velage : date invalide", Text(cut, ".errors li"));
        Assert.Equal("Confirmer l'import de 59 vaches", Text(cut, "button.confirm"));
    }

    [Fact]
    public void Fichier_de_plus_de_2_Mo_affiche_un_message_clair_et_ne_change_rien()
    {
        var cut = RenderWithHerd(DemoHerd);

        Upload(cut, new byte[(2 * 1024 * 1024) + 1], "troupeau-enorme.csv");

        cut.WaitForAssertion(() => Assert.Equal(
            "« troupeau-enorme.csv » fait 2,0 Mo : la limite est de 2 Mo. Rien n'a été importé, le troupeau est inchangé.",
            Text(cut, "[role=alert]")));
        Assert.Empty(cut.FindAll(".preview"));
        Assert.Empty(module.Invocations["putCows"]);
        Assert.Empty(module.Invocations["replaceCows"]);
    }

    [Fact]
    public void Fusionner_met_a_jour_la_vache_existante_sans_doublon()
    {
        var fromFile = ImportFixtures.Parse("troupeau-fictif.csv").Cows;
        var cut = RenderWithHerd([fromFile[0] with { LastSccThousands = 999 }]);

        Upload(cut, ImportFixtures.Bytes("troupeau-fictif.csv"), "troupeau-fictif.csv");
        cut.WaitForAssertion(() => Assert.Equal(
            "59 ajoutée(s) · 1 mise(s) à jour · 0 inchangée(s) · 0 retirée(s)",
            Text(cut, ".impact")));
        cut.Find("button.confirm").Click();

        cut.WaitForAssertion(() => Assert.Single(module.Invocations["putCows"]));
        var saved = (IReadOnlyList<Cow>)module.Invocations["putCows"][0].Arguments[0]!;
        Assert.Equal(saved.Count, saved.Select(cow => (cow.FarmId, cow.Id)).Distinct().Count());
        Assert.Equal(fromFile[0], saved.Single(cow => cow.Id == fromFile[0].Id));
    }

    [Fact]
    public void Remplacer_retire_les_vaches_absentes_du_fichier_en_un_seul_appel()
    {
        var cut = RenderWithHerd(DemoHerd);

        Upload(cut, ImportFixtures.Bytes("troupeau-fictif.csv"), "troupeau-fictif.csv");
        cut.WaitForElement("input[name=import-mode]");
        cut.FindAll("input[name=import-mode]")[1].Change(true);
        cut.WaitForAssertion(() => Assert.Equal(
            $"60 ajoutée(s) · 0 mise(s) à jour · 0 inchangée(s) · {DemoHerd.Count} retirée(s)",
            Text(cut, ".impact")));
        cut.Find("button.confirm").Click();

        cut.WaitForAssertion(() => Assert.Single(module.Invocations["replaceCows"]));
        Assert.Equal(FarmId, module.Invocations["replaceCows"][0].Arguments[0]);
        Assert.Empty(module.Invocations["putCows"]);
    }

    [Fact]
    public void Erreur_de_stockage_a_la_confirmation_garde_l_apercu()
    {
        module.SetupVoid("putCows", _ => true).SetException(new JSException("TOURNEEVETO_STORAGE:Quota:QuotaExceededError plein"));
        var cut = RenderWithHerd(DemoHerd);

        Upload(cut, ImportFixtures.Bytes("troupeau-fictif.csv"), "troupeau-fictif.csv");
        cut.WaitForElement("button.confirm").Click();

        cut.WaitForAssertion(() => Assert.Equal("Enregistrement impossible : espace de stockage plein.", Text(cut, "[role=alert]")));
        Assert.NotEmpty(cut.FindAll(".preview"));
        Assert.Empty(cut.FindAll(".success"));
    }
}
