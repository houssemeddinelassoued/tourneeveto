using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Components;
using TourneeVeto.Ui.Data;
using TourneeVeto.Ui.Pages;
using TourneeVeto.Ui.Platform;

namespace TourneeVeto.Tests.Ui;

/// <summary>Epic 9 (stories 9.1 et 9.2) : recommandations au producteur, composant et rapport, avec visitStore.js simulé.</summary>
public class RecommendationsEditorTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);
    private static readonly DateTimeOffset At = new(2026, 10, 8, 7, 30, 0, TimeSpan.Zero);
    private const string TwoLines = "Tarir 4521 et 4533 cette semaine\nDeuxième ligne : vêlage près de l'été";
    private const string SaveFailure = "TOURNEEVETO_STORAGE:Unavailable:InvalidStateError";

    private readonly DemoDataSet data = DemoData.Generate(Today, DemoDataSeeder.Seed);
    private readonly BunitJSModuleInterop module;
    private readonly Guid visitId;

    public RecommendationsEditorTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(At));
        Services.AddTourneeVeto();
        visitId = data.Visits[0].Id;
        module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult(data.Farms);
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult(data.Visits);
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult(data.Cows);
        module.Setup<IReadOnlyList<CowVisitRecord>>("getVisitRecords", _ => true).SetResult([]);
        module.Setup<BiosecurityAnswers?>("getBiosecurity", _ => true).SetResult(null);
        module.Setup<VisitRecommendations?>("getRecommendations", _ => true).SetResult(null);
        module.SetupVoid("putRecommendations", _ => true).SetVoidResult();
        JSInterop.SetupModule(ReportPrinter.ModulePath).SetupVoid("printPage").SetVoidResult();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    private static VisitRecommendations Saved(Guid id, params string[] texts)
    {
        var recommendations = VisitRecommendations.Empty(id, At);
        foreach (var text in texts)
        {
            recommendations.TryAdd(text, Guid.NewGuid(), At, out recommendations);
        }

        return recommendations;
    }

    private IRenderedComponent<RecommendationsEditor> RenderEditor(params string[] existing)
    {
        if (existing.Length > 0)
        {
            module.Setup<VisitRecommendations?>("getRecommendations", visitId).SetResult(Saved(visitId, existing));
        }

        var cut = Render<RecommendationsEditor>(parameters => parameters.Add(p => p.VisitId, visitId));
        cut.WaitForElement("textarea");
        return cut;
    }

    private static void ClickButton(IRenderedComponent<RecommendationsEditor> cut, string name) =>
        cut.FindAll("button").Single(button => button.TextContent.Trim() == name).Click();

    private static void Add(IRenderedComponent<RecommendationsEditor> cut, string text)
    {
        cut.Find("textarea.new-text").Input(text);
        ClickButton(cut, "Ajouter");
    }

    private static IReadOnlyList<string> Texts(IRenderedComponent<RecommendationsEditor> cut) =>
        cut.FindAll("ol.recommendation-list li .recommendation-text").Select(element => element.TextContent).ToList();

    private VisitRecommendations LastSaved() =>
        Assert.IsType<VisitRecommendations>(module.Invocations["putRecommendations"].Last().Arguments[0]);

    private bool HasSaved => module.Invocations.Select(invocation => invocation.Identifier).Contains("putRecommendations");

    [Fact]
    public void Le_champ_a_un_label_visible_et_une_longueur_maximale()
    {
        var cut = RenderEditor();

        var field = cut.Find("textarea.new-text");
        Assert.Equal(VisitRecommendations.MaxTextLength.ToString(), field.GetAttribute("maxlength"));
        Assert.Contains(cut.FindAll("label"), label => label.TextContent.Trim() == "Nouvelle recommandation" && label.GetAttribute("for") == field.Id);
    }

    [Fact]
    public void Enregistrement_sur_deux_lignes_puis_rechargement_a_l_identique()
    {
        var cut = RenderEditor();

        Add(cut, TwoLines);

        cut.WaitForAssertion(() => Assert.Equal(TwoLines, LastSaved().Items.Single().Text));
        Assert.Contains("Enregistré", cut.Find("[role=status].saved").TextContent);
        Assert.True(string.IsNullOrEmpty(cut.Find("textarea.new-text").GetAttribute("value")));

        var reloaded = RenderEditor(TwoLines);
        Assert.Equal([TwoLines], Texts(reloaded));
        Assert.Equal("01", reloaded.Find(".rank").TextContent);
    }

    [Fact]
    public void Ecriture_impossible_affiche_le_message_et_garde_le_texte_dans_le_champ()
    {
        module.SetupVoid("putRecommendations", _ => true).SetException(new JSException(SaveFailure));
        var cut = RenderEditor();

        Add(cut, TwoLines);

        cut.WaitForAssertion(() => Assert.Equal("Enregistrement impossible", cut.Find("[role=alert]").TextContent.Trim()));
        Assert.Equal(TwoLines, cut.Find("textarea.new-text").GetAttribute("value"));
        Assert.Empty(Texts(cut));
        Assert.Empty(cut.FindAll(".saved"));
    }

    [Fact]
    public void Une_troisieme_recommandation_est_numerotee_a_la_suite_dans_l_ordre_de_saisie()
    {
        var cut = RenderEditor("Première", "Deuxième");

        Add(cut, "Troisième");

        cut.WaitForAssertion(() => Assert.Equal(["Première", "Deuxième", "Troisième"], Texts(cut)));
        Assert.Equal(["01", "02", "03"], cut.FindAll(".rank").Select(rank => rank.TextContent));
        Assert.Equal(["Première", "Deuxième", "Troisième"], LastSaved().Items.Select(item => item.Text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void Un_champ_vide_ou_d_espaces_ne_cree_rien_et_explique_pourquoi(string text)
    {
        var cut = RenderEditor();

        Add(cut, text);

        Assert.Empty(Texts(cut));
        Assert.False(HasSaved);
        Assert.Contains("Saisissez une recommandation", cut.Find(".hint").TextContent);
    }

    [Fact]
    public void Au_dela_du_nombre_maximal_l_ajout_est_refuse_avec_un_message()
    {
        var cut = RenderEditor(Enumerable.Range(1, VisitRecommendations.MaxItems).Select(n => $"R{n}").ToArray());

        Add(cut, "De trop");

        Assert.Equal(VisitRecommendations.MaxItems, Texts(cut).Count);
        Assert.False(HasSaved);
        Assert.Contains($"{VisitRecommendations.MaxItems} recommandations", cut.Find(".hint").TextContent);
    }

    [Fact]
    public void Suppression_demande_confirmation_puis_retire_la_recommandation()
    {
        var cut = RenderEditor("A garder", "A supprimer");

        cut.Find("button[aria-label='Supprimer la recommandation 2']").Click();
        var dialog = cut.Find("[role=dialog]");
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Contains("Supprimer la recommandation 2", dialog.TextContent);
        Assert.Equal(2, Texts(cut).Count);
        Assert.False(HasSaved);

        cut.Find("[role=dialog] button.confirm").Click();

        cut.WaitForAssertion(() => Assert.Equal(["A garder"], Texts(cut)));
        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.Equal(["A garder"], LastSaved().Items.Select(item => item.Text));
    }

    [Fact]
    public void Annuler_la_suppression_garde_la_recommandation()
    {
        var cut = RenderEditor("A garder");

        cut.Find("button[aria-label='Supprimer la recommandation 1']").Click();
        cut.Find("[role=dialog] button.cancel").Click();

        Assert.Equal(["A garder"], Texts(cut));
        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.False(HasSaved);
    }

    [Fact]
    public void Suppression_impossible_garde_la_recommandation_et_signale_l_echec()
    {
        module.SetupVoid("putRecommendations", _ => true).SetException(new JSException(SaveFailure));
        var cut = RenderEditor("A garder");

        cut.Find("button[aria-label='Supprimer la recommandation 1']").Click();
        cut.Find("[role=dialog] button.confirm").Click();

        cut.WaitForAssertion(() => Assert.Equal("Enregistrement impossible", cut.Find("[role=alert]").TextContent.Trim()));
        Assert.Equal(["A garder"], Texts(cut));
    }

    [Fact]
    public void Modification_en_place_enregistre_le_nouveau_texte_a_la_meme_place()
    {
        var cut = RenderEditor("Un", "Deux", "Trois");

        cut.Find("button[aria-label='Modifier la recommandation 2']").Click();
        cut.Find("textarea[aria-label='Modifier la recommandation 2']").Input("Deux modifié\nsuite");
        ClickButton(cut, "Enregistrer");

        cut.WaitForAssertion(() => Assert.Equal(["Un", "Deux modifié\nsuite", "Trois"], Texts(cut)));
        Assert.Equal(["Un", "Deux modifié\nsuite", "Trois"], LastSaved().Items.Select(item => item.Text));
    }

    [Fact]
    public void Annuler_la_modification_garde_le_texte_d_origine()
    {
        var cut = RenderEditor("Un");

        cut.Find("button[aria-label='Modifier la recommandation 1']").Click();
        cut.Find("textarea[aria-label='Modifier la recommandation 1']").Input("Autre");
        ClickButton(cut, "Annuler");

        Assert.Equal(["Un"], Texts(cut));
        Assert.False(HasSaved);
    }

    [Fact]
    public void Modification_vide_est_refusee_et_garde_l_edition_ouverte()
    {
        var cut = RenderEditor("Un");

        cut.Find("button[aria-label='Modifier la recommandation 1']").Click();
        cut.Find("textarea[aria-label='Modifier la recommandation 1']").Input("  ");
        ClickButton(cut, "Enregistrer");

        Assert.NotEmpty(cut.FindAll("textarea[aria-label='Modifier la recommandation 1']"));
        Assert.False(HasSaved);
        Assert.Contains("Saisissez une recommandation", cut.Find(".hint").TextContent);
    }

    [Fact]
    public void Modification_impossible_garde_le_texte_saisi_dans_le_champ_d_edition()
    {
        module.SetupVoid("putRecommendations", _ => true).SetException(new JSException(SaveFailure));
        var cut = RenderEditor("Un");

        cut.Find("button[aria-label='Modifier la recommandation 1']").Click();
        cut.Find("textarea[aria-label='Modifier la recommandation 1']").Input("Nouveau");
        ClickButton(cut, "Enregistrer");

        cut.WaitForAssertion(() => Assert.Equal("Enregistrement impossible", cut.Find("[role=alert]").TextContent.Trim()));
        Assert.Equal("Nouveau", cut.Find("textarea[aria-label='Modifier la recommandation 1']").GetAttribute("value"));
    }

    [Fact]
    public void Un_texte_html_saisi_est_rendu_comme_du_texte()
    {
        const string script = "<script>alert(1)</script>";
        var cut = RenderEditor(script);

        Assert.Empty(cut.FindAll("script"));
        Assert.Equal([script], Texts(cut));
        Assert.Contains("&lt;script&gt;", cut.Find(".recommendation-text").InnerHtml);
    }

    [Fact]
    public void Sans_recommandation_la_liste_dit_Aucune_recommandation()
    {
        var cut = RenderEditor();

        Assert.Contains("Aucune recommandation", cut.Find(".empty").TextContent);
        Assert.Empty(cut.FindAll("ol"));
    }

    [Fact]
    public void Lecture_impossible_signale_l_erreur_sans_proposer_d_ecraser_les_recommandations()
    {
        module.Setup<VisitRecommendations?>("getRecommendations", _ => true).SetException(new JSException(SaveFailure));
        var cut = Render<RecommendationsEditor>(parameters => parameters.Add(p => p.VisitId, visitId));

        cut.WaitForAssertion(() => Assert.Contains("Lecture impossible", cut.Find("[role=alert]").TextContent));
        Assert.Empty(cut.FindAll("textarea"));
    }

    [Fact]
    public void Le_rapport_affiche_la_section_apres_les_pratiques_prioritaires_avec_les_recommandations_enregistrees()
    {
        module.Setup<VisitRecommendations?>("getRecommendations", visitId).SetResult(Saved(visitId, "Première", "Seconde"));

        var cut = Render<Rapport>(parameters => parameters.Add(p => p.FarmId, "F001"));
        cut.WaitForElement("#recommandations .recommendation-text");

        var section = cut.Find("#recommandations");
        Assert.Contains("Recommandations au producteur", section.QuerySelector("h2")!.TextContent);
        Assert.Equal(["Première", "Seconde"], section.QuerySelectorAll(".recommendation-text").Select(element => element.TextContent));
        Assert.True(cut.Markup.IndexOf("id=\"priority-title\"", StringComparison.Ordinal) < cut.Markup.IndexOf("id=\"recommandations\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Une_recommandation_supprimee_disparait_aussi_du_rapport()
    {
        module.Setup<VisitRecommendations?>("getRecommendations", visitId).SetResult(Saved(visitId, "A supprimer"));
        var cut = Render<Rapport>(parameters => parameters.Add(p => p.FarmId, "F001"));
        cut.WaitForElement("#recommandations .recommendation-text");

        cut.Find("button[aria-label='Supprimer la recommandation 1']").Click();
        cut.Find("[role=dialog] button.confirm").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("#recommandations .recommendation-text")));
        Assert.Contains("Aucune recommandation", cut.Find("#recommandations").TextContent);
        Assert.DoesNotContain("A supprimer", cut.Find("#recommandations").TextContent);
    }

    [Fact]
    public void Sans_visite_du_jour_le_rapport_n_offre_pas_la_saisie()
    {
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult([]);

        var cut = Render<Rapport>(parameters => parameters.Add(p => p.FarmId, "F001"));
        cut.WaitForElement("#recommandations");

        Assert.Empty(cut.FindAll("#recommandations textarea"));
        Assert.Contains("Aucune visite", cut.Find("#recommandations").TextContent);
    }

    [Fact]
    public void Donnees_alterees_relues_du_stockage_sont_assainies()
    {
        var items = new List<Recommendation> { new(Guid.NewGuid(), "  bon‮ "), new(Guid.NewGuid(), "   "), new(Guid.NewGuid(), new string('x', 1001)) };
        items.AddRange(Enumerable.Range(1, 30).Select(n => new Recommendation(Guid.NewGuid(), $"R{n}")));
        module.Setup<VisitRecommendations?>("getRecommendations", visitId).SetResult(new VisitRecommendations(visitId, items, At));

        var cut = Render<RecommendationsEditor>(parameters => parameters.Add(p => p.VisitId, visitId));
        cut.WaitForElement("textarea");

        var texts = Texts(cut);
        Assert.Equal(VisitRecommendations.MaxItems, texts.Count);
        Assert.Equal("bon", texts[0]);
        Assert.Equal("R1", texts[1]);
    }

    [Fact]
    public void Donnees_non_deserialisables_donnent_Lecture_impossible()
    {
        module.Setup<VisitRecommendations?>("getRecommendations", _ => true).SetException(new System.Text.Json.JsonException("altéré"));
        var cut = Render<RecommendationsEditor>(parameters => parameters.Add(p => p.VisitId, visitId));

        cut.WaitForAssertion(() => Assert.Contains("Lecture impossible", cut.Find("[role=alert]").TextContent));
        Assert.Empty(cut.FindAll("textarea"));
    }

    [Fact]
    public void Un_texte_trop_long_a_la_modification_donne_un_message_de_longueur()
    {
        var cut = RenderEditor("Un");

        cut.Find("button[aria-label='Modifier la recommandation 1']").Click();
        cut.Find("textarea[aria-label='Modifier la recommandation 1']").Input(new string('x', 1001));
        ClickButton(cut, "Enregistrer");

        Assert.Equal("1000 caractères au plus.", cut.Find(".hint").TextContent.Trim());
        Assert.False(HasSaved);
    }

    private void AssertLastFocusOn(AngleSharp.Dom.IElement element) =>
        JSInterop.Invocations.Last(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus").Arguments[0].ShouldBeElementReferenceTo(element);

    private int FocusCalls => JSInterop.Invocations.Count(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");

    [Fact]
    public void Le_dialogue_de_suppression_prend_le_focus_sur_Annuler()
    {
        var cut = RenderEditor("Un");

        cut.Find("button[aria-label='Supprimer la recommandation 1']").Click();

        AssertLastFocusOn(cut.Find("[role=dialog] button.cancel"));
    }

    [Fact]
    public void Echap_ferme_le_dialogue_et_rend_le_focus_au_bouton_Supprimer()
    {
        var cut = RenderEditor("Un");
        cut.Find("button[aria-label='Supprimer la recommandation 1']").Click();

        var before = FocusCalls;

        cut.Find("[role=dialog]").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.Equal(["Un"], Texts(cut));
        // bUnit ne met pas à jour l'identifiant de référence d'un élément conservé : on vérifie qu'un focus est redemandé.
        Assert.Equal(before + 1, FocusCalls);
    }

    [Fact]
    public void Une_autre_touche_ne_ferme_pas_le_dialogue()
    {
        var cut = RenderEditor("Un");
        cut.Find("button[aria-label='Supprimer la recommandation 1']").Click();

        cut.Find("[role=dialog]").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "a" });

        Assert.NotEmpty(cut.FindAll("[role=dialog]"));
    }

    [Fact]
    public void Apres_suppression_confirmee_le_focus_va_au_champ_de_nouvelle_recommandation()
    {
        var cut = RenderEditor("Un");
        cut.Find("button[aria-label='Supprimer la recommandation 1']").Click();

        var before = FocusCalls;

        cut.Find("[role=dialog] button.confirm").Click();

        cut.WaitForAssertion(() => Assert.True(FocusCalls > before));
    }

    [Fact]
    public void L_edition_prend_le_focus_dans_le_champ_et_le_rend_au_bouton_Modifier()
    {
        var cut = RenderEditor("Un");

        cut.Find("button[aria-label='Modifier la recommandation 1']").Click();
        AssertLastFocusOn(cut.Find("textarea[aria-label='Modifier la recommandation 1']"));

        ClickButton(cut, "Annuler");
        AssertLastFocusOn(cut.Find("button[aria-label='Modifier la recommandation 1']"));
    }

    [Fact]
    public void Un_compteur_relie_au_champ_affiche_le_nombre_de_caracteres()
    {
        var cut = RenderEditor("Un");
        var field = cut.Find("textarea.new-text");
        Assert.Equal("new-count", field.GetAttribute("aria-describedby"));
        Assert.Equal("0 / 1000 caractères", cut.Find("#new-count").TextContent.Trim());
        Assert.Equal("polite", cut.Find("#new-count").GetAttribute("aria-live"));

        field.Input("Bonjour");
        Assert.Equal("7 / 1000 caractères", cut.Find("#new-count").TextContent.Trim());

        cut.Find("button[aria-label='Modifier la recommandation 1']").Click();
        Assert.Equal("edit-count", cut.Find("textarea[aria-label='Modifier la recommandation 1']").GetAttribute("aria-describedby"));
        Assert.Equal("2 / 1000 caractères", cut.Find("#edit-count").TextContent.Trim());
    }
}
