using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Showcase;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Biosecurity;
using TourneeVeto.Ui.Data;
using TourneeVeto.Ui.Pages;

namespace TourneeVeto.Tests.Ui;

/// <summary>Epic 8 : questionnaire de biosécurité, score par rubrique et pratiques prioritaires (module visitStore.js simulé).</summary>
public class BiosecuriteTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private readonly DemoDataSet data = DemoData.Generate(Today, DemoDataSeeder.Seed);
    private readonly BunitJSModuleInterop module;

    public BiosecuriteTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 7, 30, 0, TimeSpan.Zero)));
        Services.AddTourneeVeto();
        module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult(data.Farms);
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult(data.Visits);
        module.Setup<BiosecurityAnswers?>("getBiosecurity", _ => true).SetResult(null);
        module.SetupVoid("putBiosecurity", _ => true).SetVoidResult();
    }

    // Le repository n'implémente que IAsyncDisposable (il ferme le module JS) : libérer le contexte de façon asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    private IRenderedComponent<Biosecurite> RenderFarm()
    {
        var cut = Render<Biosecurite>(parameters => parameters.Add(p => p.FarmId, "F001"));
        cut.WaitForElement(".section-tab");
        return cut;
    }

    [Fact]
    public void Sans_ferme_propose_les_fermes_de_la_tournee_du_jour()
    {
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult(data.Visits);

        var cut = Render<Biosecurite>();

        cut.WaitForAssertion(() => Assert.Equal(
            data.Farms.Select(farm => $"biosecurite/{farm.Id}"),
            cut.FindAll("a.choice").Select(link => link.GetAttribute("href"))));
    }

    [Fact]
    public void Cinq_rubriques_non_evaluees_et_la_premiere_ouverte()
    {
        var cut = RenderFarm();

        Assert.Equal(5, cut.FindAll(".section-tab").Count);
        Assert.All(cut.FindAll(".section-tab-score"), score => Assert.Equal("À auditer", score.TextContent));
        Assert.Equal("true", cut.FindAll(".section-tab")[0].GetAttribute("aria-pressed"));
        Assert.Equal(3, cut.FindAll(".question").Count);
        Assert.Equal(4, cut.FindAll(".question")[0].QuerySelectorAll("input[type=radio]").Length);
    }

    [Fact]
    public void Question_critique_a_Non_rend_la_rubrique_a_risque_eleve_et_devient_prioritaire()
    {
        var cut = RenderFarm();
        var critical = BiosecurityQuestionnaire.Default.First(question => question.IsCritical && question.Section == "Introduction d'animaux");

        cut.FindAll($"input[name=q-{critical.Id}]")[2].Change(true);   // Non

        Assert.Equal("0 % · Point de vigilance", cut.FindAll(".section-tab-score")[0].TextContent);
        Assert.Equal(critical.Text, cut.Find(".priority-text").TextContent);
        Assert.Contains("Point critique", cut.Find(".priority-meta").TextContent);
    }

    [Fact]
    public void Panneau_indice_global_sans_reponse_est_non_evalue()
    {
        var cut = RenderFarm();

        Assert.Equal("Indice global exploitation", cut.Find(".global-title").TextContent);
        Assert.Equal("Non évalué", cut.Find(".global-level").TextContent);
        Assert.Equal("0 / 15 questions renseignées", cut.Find(".global-answered").TextContent);
        Assert.Empty(cut.FindAll(".global-value"));
    }

    [Fact]
    public void Panneau_indice_global_reflete_les_reponses_et_precede_les_rubriques()
    {
        var cut = RenderFarm();
        var first = BiosecurityQuestionnaire.Default[0];
        var second = BiosecurityQuestionnaire.Default[1];

        cut.FindAll($"input[name=q-{first.Id}]")[0].Change(true);    // Oui
        cut.FindAll($"input[name=q-{second.Id}]")[1].Change(true);   // Partiel

        var expected = BiosecurityScore.Compute(BiosecurityQuestionnaire.Default,
            new Dictionary<string, Answer> { [first.Id] = Answer.Yes, [second.Id] = Answer.Partial });
        Assert.Equal($"{expected.OverallScore} %", cut.Find(".global-value").TextContent.Trim());
        Assert.Equal(TourneeVeto.Ui.Formatting.BiosecurityLabels.Verdict(expected.OverallLevel), cut.Find(".global-level").TextContent);
        Assert.Equal("2 / 15 questions renseignées", cut.Find(".global-answered").TextContent);
        Assert.NotNull(cut.Find(".side > .global-panel + .sections-panel"));
    }

    [Fact]
    public void Rubrique_suivante_affiche_ses_propres_questions()
    {
        var cut = RenderFarm();

        cut.Find("button.next").Click();

        Assert.Equal("true", cut.FindAll(".section-tab")[1].GetAttribute("aria-pressed"));
        Assert.StartsWith("2. Visiteurs et véhicules", cut.Find("h2.section-title").TextContent);
    }

    [Fact]
    public void Chaque_reponse_est_enregistree_pour_la_visite_du_jour()
    {
        var cut = RenderFarm();
        var question = BiosecurityQuestionnaire.Default[0];

        cut.FindAll($"input[name=q-{question.Id}]")[0].Change(true);   // Oui

        cut.WaitForAssertion(() => Assert.Equal("Enregistré à 07:30", cut.Find(".saved").TextContent));
        var saved = Assert.IsType<BiosecurityAnswers>(Assert.Single(module.Invocations["putBiosecurity"]).Arguments[0]);
        Assert.Equal(data.Visits[0].Id, saved.VisitId);
        Assert.Equal(Answer.Yes, saved.Answers[question.Id]);
    }

    [Fact]
    public void Les_reponses_deja_enregistrees_sont_reprises()
    {
        var question = BiosecurityQuestionnaire.Default[0];
        module.Setup<BiosecurityAnswers?>("getBiosecurity", data.Visits[0].Id)
            .SetResult(new BiosecurityAnswers(data.Visits[0].Id, new Dictionary<string, Answer> { [question.Id] = Answer.Partial }, DateTimeOffset.UnixEpoch));

        var cut = RenderFarm();

        Assert.True(cut.FindAll($"input[name=q-{question.Id}]")[1].HasAttribute("checked"));
        Assert.Equal("50 % · En cours", cut.FindAll(".section-tab-score")[0].TextContent);
    }

    [Fact]
    public void Rubrique_entierement_Sans_objet_reste_non_evaluee_et_sans_pratique_prioritaire()
    {
        var cut = RenderFarm();
        var firstSection = BiosecurityQuestionnaire.Default.Where(question => question.Section == BiosecurityQuestionnaire.Default[0].Section).ToList();

        foreach (var question in firstSection)
        {
            cut.FindAll($"input[name=q-{question.Id}]")[3].Change(true);   // Sans objet (S.O.)
        }

        cut.WaitForAssertion(() => Assert.Equal(firstSection.Count, module.Invocations["putBiosecurity"].Count));
        Assert.Equal("Non évaluée", cut.FindAll(".section-tab-score")[0].TextContent);
        Assert.Contains("level--neutral", cut.Find(".section-meta .level").ClassName);
        Assert.Equal("Non évaluée", cut.Find(".section-meta .level").TextContent);
        Assert.Equal("Aucune pratique à améliorer pour l'instant : répondez aux questions de chaque rubrique.", cut.Find(".priorities .status").TextContent);
        Assert.All(cut.FindAll(".section-tab-score").Skip(1), score => Assert.Equal("À auditer", score.TextContent));
    }

    [Fact]
    public void Chaque_question_a_une_description_une_observation_un_constat_et_une_preconisation()
    {
        var generic = BiosecurityAudit.Criterion("inconnue");

        Assert.All(BiosecurityQuestionnaire.Default, question => Assert.NotEqual(generic, BiosecurityAudit.Criterion(question.Id)));
        Assert.All(BiosecurityQuestionnaire.Default.Select(question => question.Section).Distinct(), section => Assert.NotEqual(BiosecurityAudit.Section("inconnue"), BiosecurityAudit.Section(section)));
    }

    [Fact]
    public void En_tete_du_poste_affiche_agrement_auditeur_et_lien_vers_le_rapport_consolide()
    {
        var cut = RenderFarm();

        Assert.Contains("Agrément N° 76-BIO-2026-001 (fictif)", cut.Markup);
        Assert.Contains("Dre Camille Exemple", cut.Find(".meta").TextContent);
        Assert.Equal("rapport/F001", cut.Find("a.action--primary").GetAttribute("href"));
        Assert.Equal("Rapport consolidé", cut.Find("a.action--primary").TextContent);
    }

    [Fact]
    public void Exporter_audit_est_simule_sans_envoi()
    {
        var cut = RenderFarm();

        cut.Find("button.action--secondary").Click();

        Assert.Equal("Démonstration : fonction simulée, aucune donnée envoyée.", cut.Find(".saved").TextContent);
        Assert.DoesNotContain("putBiosecurity", module.Invocations.Select(invocation => invocation.Identifier));
    }

    [Fact]
    public void Reponse_Non_affiche_le_constat_clinique_et_l_alerte_reglementaire()
    {
        var cut = RenderFarm();
        var question = BiosecurityQuestionnaire.Default[0];
        Assert.Empty(cut.FindAll(".regulatory"));

        cut.FindAll($"input[name=q-{question.Id}]")[2].Change(true);   // Non

        Assert.Equal("Alerte réglementaire", cut.Find(".regulatory").TextContent);
        Assert.Equal($"Constat clinique : {BiosecurityAudit.Criterion(question.Id).ClinicalFinding}", cut.Find(".observation--no").TextContent);
        Assert.Contains("Critère 1.1", cut.Find(".criterion").TextContent);
        Assert.Equal(BiosecurityAudit.Criterion(question.Id).Recommendation, cut.Find(".recommendation-text").TextContent);
        Assert.Equal(question.Text, cut.Find(".recommendation-title").TextContent);
        Assert.Contains("Alertes critiques : 1", cut.Find(".stats").TextContent);
    }

    [Fact]
    public void Reponse_Partiel_affiche_l_observation()
    {
        var cut = RenderFarm();
        var question = BiosecurityQuestionnaire.Default[1];

        cut.FindAll($"input[name=q-{question.Id}]")[1].Change(true);   // Partiel

        Assert.Equal($"Observation : {BiosecurityAudit.Criterion(question.Id).PartialObservation}", cut.Find(".observation").TextContent);
    }

    [Fact]
    public void Anneau_de_l_indice_global_n_apparait_qu_avec_une_reponse_evaluable()
    {
        var cut = RenderFarm();
        Assert.Empty(cut.FindAll(".ring-value"));

        cut.FindAll($"input[name=q-{BiosecurityQuestionnaire.Default[0].Id}]")[0].Change(true);   // Oui

        Assert.Equal(TourneeVeto.Ui.Formatting.BiosecurityLabels.RingDash(100), cut.Find(".ring-value").GetAttribute("stroke-dasharray"));
    }

    [Fact]
    public void Recalculer_affiche_un_message_de_statut()
    {
        var cut = RenderFarm();

        cut.Find("button.footer-button.mobile-only").Click();

        Assert.Equal("Bilan recalculé à partir des réponses saisies.", cut.Find(".saved").TextContent);
    }

    [Fact]
    public void Enregistrer_la_section_reecrit_les_reponses_pour_la_visite_du_jour()
    {
        var cut = RenderFarm();
        cut.FindAll($"input[name=q-{BiosecurityQuestionnaire.Default[0].Id}]")[0].Change(true);

        cut.Find("button.footer-button.desktop-only").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, module.Invocations["putBiosecurity"].Count));
    }

    [Fact]
    public void Etape_et_navigation_suivent_la_rubrique_affichee()
    {
        var cut = RenderFarm();

        Assert.Contains("Étape 1 sur 5", cut.Find(".sections-count").TextContent);
        Assert.Contains("Suivant : Visiteurs et véhicules", cut.Find("button.next").TextContent);
        Assert.Contains("Section suivante (Visiteurs et véhicules)", cut.Find("button.next").TextContent);
    }

    [Fact]
    public void Questionnaire_fictif_de_5_rubriques_et_15_questions_valides()
    {
        var questions = BiosecurityQuestionnaire.Default;

        Assert.Equal(15, questions.Count);
        Assert.Equal(5, questions.Select(question => question.Section).Distinct().Count());
        Assert.Equal(questions.Count, questions.Select(question => question.Id).Distinct().Count());
        Assert.All(questions, question => Assert.InRange(question.Weight, 1, 3));
        Assert.Contains(questions, question => question.IsCritical);
    }
}
