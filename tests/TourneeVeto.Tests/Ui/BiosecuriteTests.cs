using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TourneeVeto.Domain;
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
        Assert.All(cut.FindAll(".section-tab-score"), score => Assert.Equal("Non évaluée", score.TextContent));
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

        Assert.Equal("Risque élevé · 0/100", cut.FindAll(".section-tab-score")[0].TextContent);
        Assert.Equal(critical.Text, cut.Find(".priority-text").TextContent);
        Assert.Contains("Point critique", cut.Find(".priority-meta").TextContent);
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
