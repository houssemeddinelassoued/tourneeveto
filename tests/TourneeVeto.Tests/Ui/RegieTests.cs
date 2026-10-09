using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Showcase;
using TourneeVeto.Domain.Visits;
using AngleSharp.Dom;
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
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult(data.Visits);
        module.Setup<IReadOnlyList<CowVisitRecord>>("getVisitRecords", _ => true).SetResult([]);
        module.SetupVoid("putVisitRecord", _ => true).SetVoidResult();
    }

    private IRenderedComponent<Regie> RenderWithDemoHerd()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult(data.Cows);
        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));
        cut.WaitForElement("article.cow-card");
        return cut;
    }

    private static AngleSharp.Dom.IElement OutcomeButton(IRenderedComponent<Regie> cut, string cowId, string label) =>
        cut.FindAll("article.cow-card")
            .Single(card => card.QuerySelector(".cow-card__number-value")!.TextContent == cowId)
            .QuerySelectorAll(".cow-card__outcome")
            .Single(button => button.TextContent.Trim() == label);

    private string FirstPregnancyCheckCow() => DailyActions.Compute(data.Cows, Today).Items
        .First(item => item.Motives[0].Action == RegieAction.PregnancyCheck).Cow.Id;

    [Fact]
    public void La_recherche_de_la_barre_du_haut_prefiltre_la_grille()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult(data.Cows);
        var target = DailyActions.Compute(data.Cows, Today).Items[0];

        var navigation = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("q", target.Cow.Id));
        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));
        cut.WaitForElement("article.cow-card");

        Assert.Equal(target.Cow.Id, cut.Find("input[type=search]").GetAttribute("value"));
        Assert.All(cut.FindAll("article.cow-card .cow-card__number-value"), number => Assert.Contains(target.Cow.Id, number.TextContent));
    }

    [Fact]
    public void Un_resultat_saisi_est_enregistre_aussitot()
    {
        var cut = RenderWithDemoHerd();
        var cowId = FirstPregnancyCheckCow();

        OutcomeButton(cut, cowId, "Positif").Click();

        cut.WaitForAssertion(() => Assert.Equal("Enregistré à 07:30", cut.Find(".saved").TextContent));
        var record = Assert.IsType<CowVisitRecord>(Assert.Single(module.Invocations["putVisitRecord"]).Arguments[0]);
        Assert.Equal((data.Visits[0].Id, cowId, ResultOutcome.Positive), (record.VisitId, record.CowId, record.ResultFor(RegieAction.PregnancyCheck)));
        Assert.Equal("true", OutcomeButton(cut, cowId, "Positif").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Les_saisies_deja_enregistrees_sont_reprises()
    {
        var cowId = FirstPregnancyCheckCow();
        var saved = CowVisitRecord.Empty(data.Visits[0].Id, cowId)
            .WithResult(RegieAction.PregnancyCheck, ResultOutcome.Negative, new DateTimeOffset(2026, 10, 8, 7, 0, 0, TimeSpan.Zero))
            .WithNote("Revoir dans 15 j", new DateTimeOffset(2026, 10, 8, 7, 0, 0, TimeSpan.Zero));
        module.Setup<IReadOnlyList<CowVisitRecord>>("getVisitRecords", data.Visits[0].Id).SetResult([saved]);

        var cut = RenderWithDemoHerd();

        Assert.Equal("true", OutcomeButton(cut, cowId, "Négatif").GetAttribute("aria-pressed"));
        Assert.Contains(cut.FindAll("textarea"), textarea => textarea.GetAttribute("value") == "Revoir dans 15 j");
    }

    [Fact]
    public void Stockage_plein_affiche_le_message_et_garde_la_saisie_visible()
    {
        module.SetupVoid("putVisitRecord", _ => true).SetException(new JSException("TOURNEEVETO_STORAGE:Quota:QuotaExceededError plein"));
        var cut = RenderWithDemoHerd();
        var cowId = FirstPregnancyCheckCow();

        OutcomeButton(cut, cowId, "Douteux").Click();

        cut.WaitForAssertion(() => Assert.Equal("Enregistrement impossible : espace de stockage plein.", cut.Find("[role=alert]").TextContent));
        Assert.Equal("true", OutcomeButton(cut, cowId, "Douteux").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void La_note_d_une_vache_est_enregistree()
    {
        var cut = RenderWithDemoHerd();

        cut.FindAll("textarea")[0].Change("Boiterie AP gauche");

        cut.WaitForAssertion(() => Assert.Single(module.Invocations["putVisitRecord"]));
        Assert.Equal("Boiterie AP gauche", ((CowVisitRecord)module.Invocations["putVisitRecord"][0].Arguments[0]!).Note);
    }

    [Fact]
    public void Sans_visite_prevue_aujourd_hui_la_saisie_est_desactivee()
    {
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult([]);

        var cut = RenderWithDemoHerd();

        Assert.Contains("la saisie des résultats est désactivée", cut.Find(".warning").TextContent);
        Assert.Empty(cut.FindAll(".cow-card__outcome"));
        Assert.Empty(cut.FindAll("textarea"));
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
        Assert.Equal(farm.Name, cut.Find(".farm-name").TextContent);
        Assert.Contains(farm.Municipality, cut.Find(".farm-meta--mobile").TextContent);
    }

    [Fact]
    public void Sans_ferme_propose_les_fermes_de_la_tournee_du_jour()
    {
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult(data.Visits);

        var cut = Render<Regie>();

        cut.WaitForAssertion(() => Assert.Equal(
            data.Farms.Select(farm => $"regie/{farm.Id}"),
            cut.FindAll("a.choice").Select(link => link.GetAttribute("href"))));
        Assert.Empty(module.Invocations["getCowsByFarm"]);
    }

    [Fact]
    public void Filtre_DG_n_affiche_que_les_vaches_a_diagnostiquer()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult(data.Cows);
        var expected = DailyActions.Compute(data.Cows, Today).Items
            .Where(item => item.Motives.Any(motive => motive.Action == RegieAction.PregnancyCheck))
            .Select(item => item.Cow.Id)
            .ToList();

        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));
        var filterDg = cut.WaitForElement("button.filter:contains('DG')");
        filterDg.Click();

        Assert.Equal(DemoData.PregnancyChecksDue + DemoData.PregnancyChecksOverdue, expected.Count);
        Assert.Equal($"DG ({expected.Count})", cut.Find("button[aria-pressed=true]").TextContent.Trim());
        Assert.Equal(expected, cut.FindAll(".cow-card__number-value").Select(number => number.TextContent));
    }

    [Fact]
    public void Filtre_Tous_reaffiche_toute_la_grille()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult(data.Cows);
        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));
        cut.WaitForElement("button.filter:contains('DG')").Click();

        cut.Find("button.filter:contains('Tous')").Click();

        Assert.Equal(DailyActions.Compute(data.Cows, Today).Items.Count, cut.FindAll("article.cow-card").Count);
    }

    [Fact]
    public void Etat_vide_quand_aucune_vache_n_a_de_motif()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult([]);

        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));

        cut.WaitForAssertion(() => Assert.Equal("Aucune action de régie pour cette visite.", cut.Find(".status").TextContent));
        Assert.Empty(cut.FindAll("button.filter"));
        Assert.Equal("false", cut.Find("section").GetAttribute("aria-busy"));
    }

    [Fact]
    public void Ferme_inconnue_affiche_ferme_introuvable_sans_lire_de_vaches()
    {
        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F999"));

        cut.WaitForAssertion(() => Assert.Equal("Ferme introuvable.", cut.Find("[role=alert]").TextContent));
        Assert.Empty(module.Invocations["getCowsByFarm"]);
    }

    [Fact]
    public void Erreur_de_stockage_est_affichee_sans_planter()
    {
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001")
            .SetException(new JSException("TOURNEEVETO_STORAGE:Unavailable:InvalidStateError navigation privée"));

        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));

        cut.WaitForAssertion(() => Assert.Equal(
            "Stockage local indisponible : les données ne peuvent être ni lues ni enregistrées.",
            cut.Find("[role=alert]").TextContent));
    }

    [Fact]
    public void Seuils_invalides_affichent_un_avertissement()
    {
        Services.AddSingleton(RegieThresholds.Default with { HighSccThousands = 0 });
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", "F001").SetResult(data.Cows);

        var cut = Render<Regie>(parameters => parameters.Add(p => p.FarmId, "F001"));

        cut.WaitForAssertion(() => Assert.Equal(
            "Configuration des seuils invalide : valeurs par défaut utilisées.",
            cut.Find("[role=status]").TextContent));
    }

    [Fact]
    public void Lien_vers_le_rapport_de_la_ferme()
    {
        var cut = RenderWithDemoHerd();

        Assert.Equal("rapport/F001", cut.Find("a.report-link").GetAttribute("href"));
    }

    [Fact]
    public void La_recherche_filtre_par_numero_de_boucle_ou_nom_et_garde_les_compteurs()
    {
        var cut = RenderWithDemoHerd();
        var items = DailyActions.Compute(data.Cows, Today).Items;
        var target = items[1];
        var allLabel = cut.Find("button.filter").TextContent.Trim();

        cut.Find("input[type=search]").Input(target.Cow.Name.ToUpperInvariant());

        var expected = CowSearch.Filter(items, target.Cow.Name).Select(item => item.Cow.Id);
        Assert.Equal(expected, cut.FindAll(".cow-card__number-value").Select(number => number.TextContent));
        Assert.Equal(allLabel, cut.Find("button.filter").TextContent.Trim());
        Assert.Equal("N° boucle ou nom", cut.Find("label[for=cow-search]").TextContent.Trim());
    }

    [Fact]
    public void Etat_vide_quand_aucune_vache_ne_correspond_a_la_recherche()
    {
        var cut = RenderWithDemoHerd();

        cut.Find("input[type=search]").Input("zzzzz");

        Assert.Empty(cut.FindAll("article.cow-card"));
        Assert.Equal("Aucune vache ne correspond à « zzzzz ».", cut.Find(".empty").TextContent);
        Assert.NotEmpty(cut.FindAll("button.filter"));
    }

    [Fact]
    public void Le_bandeau_indique_le_nombre_d_actes_prevus_reel()
    {
        var cut = RenderWithDemoHerd();

        var expected = DailyActions.Compute(data.Cows, Today).Items.Count;

        Assert.Equal($"{expected} actes prévus", cut.Find(".banner--mobile .banner-count").TextContent);
        Assert.Contains("RFID Active", cut.Find(".banner--mobile .banner-text").TextContent);
    }

    [Fact]
    public void La_carte_elevage_presente_l_effectif_la_race_et_l_eleveur_fictifs()
    {
        var cut = RenderWithDemoHerd();
        var profile = ShowcaseBuilder.ProfileOf(data.Farms, data.Visits, "F001", Today)!;

        Assert.Contains($"{profile.HerdSize} vaches laitières · {profile.Breed}", cut.Find(".farm-meta").TextContent);
        Assert.Contains($"{profile.Farmer} ({profile.Phone})", cut.Find(".farm-meta--desktop:last-of-type").TextContent);
        Assert.Equal(profile.Identifier, cut.Find(".chip--id").TextContent);
    }

    [Fact]
    public void Le_lecteur_de_boucle_est_simule_sans_envoi()
    {
        var cut = RenderWithDemoHerd();

        cut.Find("button.reader").Click();

        Assert.Equal("Démonstration : fonction simulée, aucune donnée envoyée.", cut.Find(".demo").TextContent);
        Assert.Equal("Lire une boucle avec le lecteur RFID (simulé)", cut.Find("button.reader").GetAttribute("aria-label"));
    }

    [Fact]
    public void Trier_classe_les_cartes_par_numero_de_boucle()
    {
        var cut = RenderWithDemoHerd();

        cut.Find("button.sort").Click();

        var numbers = cut.FindAll(".cow-card__number-value").Select(number => number.TextContent).ToList();
        Assert.Equal(numbers.Order(StringComparer.Ordinal), numbers);
        Assert.Equal("true", cut.Find("button.sort").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void La_pastille_d_un_CCS_eleve_donne_le_comptage()
    {
        var cut = RenderWithDemoHerd();
        var high = DailyActions.Compute(data.Cows, Today).Items.First(item => item.Motives.FirstOrDefault()?.Action == RegieAction.HighScc);

        var card = cut.FindAll("article.cow-card").Single(article => article.QuerySelector(".cow-card__number-value")!.TextContent == high.Cow.Id);

        Assert.Equal($"{high.Cow.LastSccThousands}k sp/mL", card.QuerySelector(".cow-card__due")!.TextContent);
        Assert.StartsWith("CA QC ", card.QuerySelector(".cow-card__national")!.TextContent);
    }

    [Fact]
    public void La_fiche_de_la_vache_selectionnee_reprend_les_actes_et_la_note_enregistres()
    {
        var cut = RenderWithDemoHerd();
        var first = DailyActions.Compute(data.Cows, Today).Items[0];

        Assert.Equal(first.Cow.Name, cut.Find("aside h2").TextContent);

        cut.Find("aside .act").Click();
        cut.WaitForAssertion(() => Assert.Single(module.Invocations["putVisitRecord"]));

        cut.Find("aside input.note-input").Input("Ligament relâché");
        cut.Find("aside form.note").Submit();

        cut.WaitForAssertion(() => Assert.Equal(2, module.Invocations["putVisitRecord"].Count));
        Assert.Equal("Ligament relâché", ((CowVisitRecord)module.Invocations["putVisitRecord"][1].Arguments[0]!).Note);
    }

    [Fact]
    public void Choisir_une_vache_change_la_fiche()
    {
        var cut = RenderWithDemoHerd();
        var second = DailyActions.Compute(data.Cows, Today).Items[1];

        cut.FindAll(".cow-card__select")[1].Click();

        Assert.Equal(second.Cow.Name, cut.Find("aside h2").TextContent);
    }
}
