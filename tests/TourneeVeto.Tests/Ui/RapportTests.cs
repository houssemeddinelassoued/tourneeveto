using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Biosecurity;
using TourneeVeto.Ui.Data;
using TourneeVeto.Ui.Pages;
using TourneeVeto.Ui.Platform;

namespace TourneeVeto.Tests.Ui;

/// <summary>Epic 10 : rapport de visite (contenu et impression), avec visitStore.js et printReport.js simulés.</summary>
public class RapportTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);
    private static readonly DateTimeOffset At = new(2026, 10, 8, 7, 0, 0, TimeSpan.Zero);

    private readonly DemoDataSet data = DemoData.Generate(Today, DemoDataSeeder.Seed);
    private readonly BunitJSModuleInterop module;
    private readonly BunitJSModuleInterop printModule;

    public RapportTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 7, 30, 0, TimeSpan.Zero)));
        Services.AddTourneeVeto();
        module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult(data.Farms);
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult(data.Visits);
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult(data.Cows);
        module.Setup<IReadOnlyList<CowVisitRecord>>("getVisitRecords", _ => true).SetResult([]);
        module.Setup<BiosecurityAnswers?>("getBiosecurity", _ => true).SetResult(null);
        printModule = JSInterop.SetupModule(ReportPrinter.ModulePath);
        printModule.SetupVoid("printPage").SetVoidResult();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    private IRenderedComponent<Rapport> RenderFarm()
    {
        var cut = Render<Rapport>(parameters => parameters.Add(p => p.FarmId, "F001"));
        cut.WaitForElement(".report-table");
        return cut;
    }

    private static BiosecurityAnswers ThreePracticesToImprove(Guid visitId)
    {
        var answers = BiosecurityQuestionnaire.Default.Take(3).ToDictionary(question => question.Id, _ => Answer.No);
        return new BiosecurityAnswers(visitId, answers, At);
    }

    [Fact]
    public void Sans_ferme_propose_les_fermes_de_la_tournee_du_jour()
    {
        var cut = Render<Rapport>();

        cut.WaitForAssertion(() => Assert.Equal(
            data.Farms.Select(farm => $"rapport/{farm.Id}"),
            cut.FindAll("a.choice").Select(link => link.GetAttribute("href"))));
    }

    [Fact]
    public void Rapport_complet_affiche_ferme_date_vaches_rubriques_recommandations_et_mention()
    {
        module.Setup<BiosecurityAnswers?>("getBiosecurity", data.Visits[0].Id).SetResult(ThreePracticesToImprove(data.Visits[0].Id));
        var farm = data.Farms[0];

        var cut = RenderFarm();

        Assert.Contains(farm.Name, cut.Find(".farm").TextContent);
        Assert.Contains("8 octobre 2026", cut.Find(".date").TextContent);
        Assert.Equal(DailyActions.Compute(data.Cows, Today).Items.Select(item => item.Cow.Id), cut.FindAll(".report-table tbody tr").Select(row => row.QuerySelector(".cow-id")!.TextContent));
        Assert.Equal(5, cut.FindAll(".bio-sections li").Count);
        Assert.Contains("3 Pratiques prioritaires prescrites", cut.Find("#priority-title").TextContent);
        Assert.Equal(3, cut.FindAll(".priority-list li").Count);
        Assert.Contains("Données fictives — règles simplifiées", cut.Find(".disclaimer").TextContent);
        Assert.NotEmpty(cut.Find(".global-score").TextContent);
        Assert.Empty(cut.FindAll(".not-done"));
    }

    [Fact]
    public void Vache_sans_resultat_apparait_comme_non_vue()
    {
        var cut = RenderFarm();

        Assert.All(cut.FindAll(".report-table tbody tr"), row => Assert.Equal("Non vue", row.QuerySelector(".result")!.TextContent.Trim()));
    }

    [Fact]
    public void Resultat_et_note_saisis_sont_repris()
    {
        var cowId = DailyActions.Compute(data.Cows, Today).Items.First(item => item.Motives[0].Action == RegieAction.PregnancyCheck).Cow.Id;
        var record = CowVisitRecord.Empty(data.Visits[0].Id, cowId).WithResult(RegieAction.PregnancyCheck, ResultOutcome.Positive, At).WithNote("Revoir dans 15 j", At);
        module.Setup<IReadOnlyList<CowVisitRecord>>("getVisitRecords", data.Visits[0].Id).SetResult([record]);

        var cut = RenderFarm();

        var row = cut.FindAll(".report-table tbody tr").Single(candidate => candidate.QuerySelector(".cow-id")!.TextContent == cowId);
        Assert.Contains("DG : Positif", row.QuerySelector(".result")!.TextContent);
        Assert.Equal("Revoir dans 15 j", row.QuerySelector(".note")!.TextContent.Trim());
    }

    [Fact]
    public void Sans_bilan_de_biosecurite_la_section_le_signale_et_le_reste_s_affiche()
    {
        var cut = RenderFarm();

        Assert.Equal("Bilan de biosécurité non réalisé", cut.Find(".not-done").TextContent.Trim());
        Assert.NotEmpty(cut.FindAll(".report-table tbody tr"));
        Assert.Empty(cut.FindAll(".global-score"));
    }

    [Fact]
    public async Task Le_bouton_lance_l_impression_par_le_module_isole()
    {
        var cut = RenderFarm();

        await cut.Find("button.print").ClickAsync(new());

        Assert.Single(printModule.Invocations["printPage"]);
        Assert.Contains("Imprimer / Exporter PDF", cut.Find("button.print").TextContent);
    }

    [Fact]
    public void Document_affiche_reference_praticien_duree_et_pied_fictif()
    {
        var cut = RenderFarm();

        Assert.Equal("SYN-2026-1008", cut.Find(".reference").TextContent);
        Assert.Contains("Dre Camille Exemple", cut.Find("#party-vet").ParentElement!.TextContent);
        Assert.Contains("Durée effective", cut.Find("#party-date").ParentElement!.TextContent);
        Assert.Contains("Rapport finalisé · Conforme ordre vétérinaire", cut.Find(".banner").TextContent);
        Assert.Contains("Données fictives", cut.Find(".doc-foot").TextContent);
    }

    [Fact]
    public void Constats_sans_saisie_ont_un_repli_lisible_et_pas_de_barre()
    {
        var cut = RenderFarm();

        Assert.Empty(cut.FindAll("progress"));
        Assert.Contains("Aucun diagnostic saisi", cut.Find("#tile-repro").ParentElement!.TextContent);
        Assert.Contains("Aucun résultat saisi", cut.Find("#tile-mammary").ParentElement!.TextContent);
    }

    [Fact]
    public void Diagnostics_saisis_donnent_un_taux_et_une_barre_de_progression()
    {
        var cowId = DailyActions.Compute(data.Cows, Today).Items.First(item => item.Motives[0].Action == RegieAction.PregnancyCheck).Cow.Id;
        var record = CowVisitRecord.Empty(data.Visits[0].Id, cowId).WithResult(RegieAction.PregnancyCheck, ResultOutcome.Positive, At);
        module.Setup<IReadOnlyList<CowVisitRecord>>("getVisitRecords", data.Visits[0].Id).SetResult([record]);

        var cut = RenderFarm();

        Assert.Equal("100", cut.Find("progress").GetAttribute("value"));
        Assert.Contains("100 %", cut.Find("#tile-repro").ParentElement!.TextContent);
    }

    [Fact]
    public void Photos_et_signatures_fictives_sont_signalees_et_l_empreinte_est_affichee()
    {
        var cut = RenderFarm();

        Assert.Equal(2, cut.FindAll(".photo .photo-tag").Count);
        Assert.All(cut.FindAll(".photo-tag"), tag => Assert.Equal("Photo fictive", tag.TextContent));
        Assert.Equal(2, cut.FindAll(".signature").Count);
        Assert.Contains("Validé hors-ligne à 07:30", cut.Find(".validated").TextContent);
        Assert.Matches("^[0-9a-f]{64}$", cut.Find(".hash-full").TextContent);
        Assert.Contains("…", cut.Find(".hash-short").TextContent);
    }

    [Fact]
    public void Historique_propose_la_visite_du_jour_et_trois_visites_passees_filtrables()
    {
        var cut = RenderFarm();

        Assert.Equal(4, cut.FindAll(".history-list .visit-card").Count);
        Assert.Contains("Sélectionné", cut.Find(".visit-card--current").TextContent);

        cut.Find("input.search").Input("zzz-introuvable");

        Assert.Empty(cut.FindAll(".visit-card"));
        Assert.Contains("Aucune synthèse ne correspond", cut.Find(".history [role=status]").TextContent);
    }

    [Fact]
    public async Task Boutons_d_envoi_sont_simules_sans_navigation()
    {
        var cut = RenderFarm();

        await cut.FindAll("button.action").Single(button => button.TextContent.Trim() == "Télétransmettre").ClickAsync(new());

        Assert.Equal("Démonstration : fonction simulée, aucune donnée envoyée.", cut.Find(".simulated").TextContent);
        Assert.Empty(printModule.Invocations["printPage"]);
    }

    [Fact]
    public void Ferme_inconnue_affiche_ferme_introuvable()
    {
        var cut = Render<Rapport>(parameters => parameters.Add(p => p.FarmId, "F999"));

        cut.WaitForAssertion(() => Assert.Equal("Ferme introuvable.", cut.Find("[role=alert]").TextContent));
    }

    [Fact]
    public void Erreur_de_stockage_est_affichee_sans_planter()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001")
            .SetException(new JSException("TOURNEEVETO_STORAGE:Unavailable:InvalidStateError navigation privée"));

        var cut = Render<Rapport>(parameters => parameters.Add(p => p.FarmId, "F001"));

        cut.WaitForAssertion(() => Assert.Equal(
            "Stockage local indisponible : les données ne peuvent être ni lues ni enregistrées.",
            cut.Find("[role=alert]").TextContent));
        Assert.Empty(cut.FindAll(".report-table"));
    }
}
