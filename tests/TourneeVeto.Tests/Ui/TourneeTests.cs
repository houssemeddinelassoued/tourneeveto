using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Showcase;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui;
using TourneeVeto.Ui.Data;
using TourneeVeto.Ui.Pages;
using TourneeVeto.Ui.Platform;

namespace TourneeVeto.Tests.Ui;

/// <summary>Page Tournée : la tournée du jour est lue dans le stockage local (module JS simulé, jamais la vraie base).</summary>
public class TourneeTests : BunitContext, IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private readonly BunitJSModuleInterop module;
    private readonly BunitJSModuleInterop printModule;

    public TourneeTests()
    {
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 7, 30, 0, TimeSpan.Zero)));
        Services.AddTourneeVeto();
        module = JSInterop.SetupModule(IndexedDbVisitRepository.ModulePath);
        module.Setup<IReadOnlyList<CowVisitRecord>>("getVisitRecords", _ => true).SetResult([]);

        var status = JSInterop.SetupModule(AppStatusService.ModulePath);
        status.Setup<AppStatus>("watch", _ => true).SetResult(new AppStatus(Online: true, OfflineReady: true, UpdateAvailable: false));
        status.SetupVoid("unwatch").SetVoidResult();

        printModule = JSInterop.SetupModule(ReportPrinter.ModulePath);
        printModule.SetupVoid("printPage").SetVoidResult();
    }

    // Le repository n'implémente que IAsyncDisposable (il ferme le module JS) : libérer le contexte de façon asynchrone.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    private DemoDataSet SetupTour(CowVisitRecord? started = null, bool reverseVisits = false)
    {
        var data = DemoData.Generate(Today, DemoDataSeeder.Seed);
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult(data.Farms);
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult(reverseVisits ? [.. data.Visits.Reverse()] : data.Visits);
        foreach (var farm in data.Farms)
        {
            module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", farm.Id).SetResult([.. data.Cows.Where(cow => cow.FarmId == farm.Id)]);
        }

        if (started is not null)
        {
            module.Setup<IReadOnlyList<CowVisitRecord>>("getVisitRecords", data.Visits[1].Id).SetResult([started]);
        }

        return data;
    }

    private IRenderedComponent<Tournee> RenderLoaded()
    {
        var cut = Render<Tournee>();
        cut.WaitForElement("h2.farm");
        return cut;
    }

    private static int ToSee(DemoDataSet data, Farm farm) =>
        DailyActions.Compute(data.Cows.Where(cow => cow.FarmId == farm.Id), Today).Items.Count;

    [Fact]
    public void Affiche_la_tournee_du_jour_lue_dans_le_stockage()
    {
        var data = SetupTour(reverseVisits: true);

        var cut = Render<Tournee>();

        cut.WaitForAssertion(() => Assert.Equal(
            data.Farms.Select(farm => farm.Name),
            cut.FindAll("h2.farm").Select(farm => farm.TextContent)));
        Assert.Equal("false", cut.Find("section").GetAttribute("aria-busy"));
        Assert.Equal("Jeudi 8 octobre 2026", cut.Find("h1").TextContent);
        Assert.Equal(data.Farms.Select(farm => $"regie/{farm.Id}"), cut.FindAll("a.action").Select(link => link.GetAttribute("href")));
        Assert.Equal(["Démarrer la visite", "Voir l'élevage", "Voir l'élevage"], cut.FindAll("a.action").Select(link => link.TextContent.Trim()));
        Assert.StartsWith("Étape 1 · Suivante", cut.Find(".stop--next .state").TextContent);

        var expectedToSee = data.Farms.Sum(farm => ToSee(data, farm));
        Assert.Equal($"{expectedToSee} bovins ciblés", cut.Find(".summary .chip").TextContent);
        Assert.Contains($"{data.Farms.Count} visites prévues", cut.Find(".summary").TextContent);
        Assert.Contains("Données fictives", cut.Find(".demo-mark").TextContent);
    }

    [Fact]
    public void Une_visite_avec_des_saisies_est_en_cours()
    {
        var data = DemoData.Generate(Today, DemoDataSeeder.Seed);
        SetupTour(CowVisitRecord.Empty(data.Visits[1].Id, "2001").WithNote("Vue", new DateTimeOffset(2026, 10, 8, 7, 0, 0, TimeSpan.Zero)));

        var cut = Render<Tournee>();

        cut.WaitForAssertion(() => Assert.Equal(
            ["Étape 1 · Suivante", "Étape 2 · En cours", "Étape 3 · En attente", "Étape 4 · Fin de tournée"],
            cut.FindAll(".states .state:first-child").Select(state => state.TextContent)));
    }

    [Fact]
    public void Le_bandeau_hors_ligne_reflete_l_etat_reel_du_cache()
    {
        var data = SetupTour();

        var cut = RenderLoaded();

        cut.WaitForAssertion(() => Assert.Contains("Mode Hors-ligne actif", cut.Find(".only-mobile [role=status]").TextContent));
        Assert.Contains($"{data.Farms.Count} élevages synchronisés", cut.Find(".only-mobile [role=status]").TextContent);
        Assert.Contains("100 % Prêt", cut.Find(".only-mobile [role=status]").TextContent);

        Services.GetRequiredService<AppStatusService>().OnStatusChanged(new AppStatus(Online: true, OfflineReady: false, UpdateAvailable: false));

        cut.WaitForAssertion(() => Assert.Contains("Préparation du mode hors ligne", cut.Find(".only-mobile [role=status]").TextContent));
    }

    [Fact]
    public void Les_cartes_tablette_donnent_heure_lieu_identifiant_motif_et_etiquettes()
    {
        var data = SetupTour();
        var day = ShowcaseBuilder.Build(data, Today);

        var cut = RenderLoaded();
        var cards = cut.FindAll(".only-mobile .stop:not(.stop--alert)");

        Assert.Equal(data.Farms.Count, cards.Count);
        Assert.Equal("08h30", cards[0].QuerySelector(".time")!.TextContent);
        Assert.Contains(day.Farms[0].Identifier, cards[0].TextContent);
        Assert.Contains(day.Farms[0].Municipality, cards[0].QuerySelector(".place")!.TextContent);
        Assert.Contains(data.Visits[0].Reason, cards[0].QuerySelector(".reason")!.TextContent);
        Assert.Equal(day.Farms[0].Tags, cards[0].QuerySelectorAll(".chip").Select(chip => chip.TextContent));
        Assert.NotNull(cards[0].QuerySelector("button.icon-button[aria-label]"));
        Assert.Null(cards[1].QuerySelector("button.icon-button"));
    }

    [Fact]
    public void La_carte_d_urgence_de_fin_de_tournee_et_les_boutons_externes_sont_simules()
    {
        var data = SetupTour();
        var alert = ShowcaseBuilder.Build(data, Today).Alert;

        var cut = RenderLoaded();

        Assert.Equal(alert.Title, cut.Find(".stop--alert .alert-title").TextContent);
        Assert.Empty(cut.Find(".demo").TextContent);

        cut.Find("button.emergency").Click();

        Assert.Equal("Démonstration : fonction simulée, aucune donnée envoyée.", cut.Find(".demo").TextContent);
        Assert.Equal("status", cut.Find(".demo").GetAttribute("role"));
        Assert.Contains("Aperçu : Cas sans tournée / Repos", cut.Find("details.preview summary").TextContent);
    }

    [Fact]
    public void Filtres_de_la_tournee_avec_leurs_compteurs()
    {
        var data = DemoData.Generate(Today, DemoDataSeeder.Seed);
        SetupTour(CowVisitRecord.Empty(data.Visits[1].Id, "2001").WithNote("Vue", new DateTimeOffset(2026, 10, 8, 7, 0, 0, TimeSpan.Zero)));
        var urgent = data.Farms.Count(farm => DailyActions.Compute(data.Cows.Where(cow => cow.FarmId == farm.Id), Today).Items.Any(item => item.Urgency == Urgency.Urgent));

        var cut = RenderLoaded();

        Assert.Equal(
            [$"Toutes ({data.Farms.Count})", "En cours (1)", $"En attente ({data.Farms.Count - 1})", $"Urgence ({urgent})"],
            cut.FindAll("button.filter").Select(button => button.TextContent.Trim()));
        Assert.Equal("true", cut.Find("button.filter").GetAttribute("aria-pressed"));

        cut.FindAll("button.filter")[1].Click();

        Assert.Equal([data.Farms[1].Name], cut.FindAll("h2.farm").Select(farm => farm.TextContent));
        Assert.StartsWith("Étape 2 · En cours", cut.Find(".states .state").TextContent);
        Assert.Empty(cut.FindAll(".stop--alert"));
        cut.FindAll("button.filter")[2].Click();
        Assert.Equal(data.Farms.Count - 1, cut.FindAll("h2.farm").Count);
    }

    [Fact]
    public void Filtre_Urgence_ne_garde_que_les_visites_avec_urgence()
    {
        var data = DemoData.Generate(Today, DemoDataSeeder.Seed);
        SetupTour();
        var expected = data.Farms
            .Where(farm => DailyActions.Compute(data.Cows.Where(cow => cow.FarmId == farm.Id), Today).Items.Any(item => item.Urgency == Urgency.Urgent))
            .Select(farm => farm.Name);

        var cut = RenderLoaded();
        cut.FindAll("button.filter")[3].Click();

        Assert.Equal(expected, cut.FindAll("h2.farm").Select(farm => farm.TextContent));
    }

    [Fact]
    public void Filtre_sans_resultat_affiche_un_etat_vide_et_garde_les_filtres()
    {
        SetupTour();

        var cut = RenderLoaded();
        cut.FindAll("button.filter")[1].Click();   // En cours : aucune saisie

        Assert.Equal("Aucune visite ne correspond à ce filtre.", cut.Find(".status").TextContent);
        Assert.Equal(4, cut.FindAll("button.filter").Count);
    }

    [Fact]
    public void Indique_l_absence_de_visite()
    {
        module.Setup<bool>("seedIfEmpty", _ => true).SetResult(false);
        module.Setup<IReadOnlyList<Farm>>("getFarms").SetResult([]);
        module.Setup<IReadOnlyList<Visit>>("getVisitsByDate", Today).SetResult([]);

        var cut = Render<Tournee>();

        cut.WaitForAssertion(() => Assert.Equal("Aucune visite prévue aujourd'hui.", cut.Find(".status").TextContent));
        Assert.Contains("Aperçu : Cas sans tournée / Repos", cut.Find("details.preview summary").TextContent);
        Assert.Empty(cut.FindAll(".dstop"));
    }

    [Fact]
    public void Affiche_l_erreur_de_stockage_sans_planter()
    {
        module.Setup<bool>("seedIfEmpty", _ => true)
            .SetException(new JSException("TOURNEEVETO_STORAGE:Unavailable:InvalidStateError navigation privée"));

        var cut = Render<Tournee>();

        cut.WaitForAssertion(() => Assert.Equal(
            "Stockage local indisponible : les données ne peuvent être ni lues ni enregistrées.",
            cut.Find("[role=alert]").TextContent));
    }

    // --- Poste (≥ 1024 px) : l'écran contient les deux mises en page, la feuille de style masque l'une ou l'autre. ---

    [Fact]
    public void Le_poste_resume_le_secteur_les_visites_et_le_trajet()
    {
        var data = SetupTour();
        var day = ShowcaseBuilder.Build(data, Today);

        var cut = RenderLoaded();
        var extra = cut.Find(".desk-extra");

        Assert.Contains($"Planning de secteur · {day.Veterinarian.Sector}", cut.Find(".eyebrow").TextContent);
        Assert.Contains("Synchronisé à 07h45", extra.TextContent);
        Assert.Contains($"{data.Farms.Count} cheptels prêts", extra.TextContent);
        Assert.Contains(day.Tour.TotalKm.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("fr-CA")), extra.QuerySelector(".figures")!.TextContent);
        Assert.Equal(["Feuille de route", "+ Ajouter Urgence"], extra.QuerySelectorAll(".header-actions .button").Select(button => button.TextContent.Trim()));
    }

    [Fact]
    public void Feuille_de_route_lance_l_impression_et_ajouter_urgence_est_simule()
    {
        SetupTour();
        var cut = RenderLoaded();

        cut.FindAll(".header-actions .button")[0].Click();

        Assert.Single(printModule.Invocations["printPage"]);

        cut.FindAll(".header-actions .button")[1].Click();

        Assert.Contains("fonction simulée", cut.Find(".demo").TextContent);
    }

    [Fact]
    public void La_liste_du_poste_montre_les_etapes_et_selectionne_la_premiere()
    {
        var data = SetupTour();
        var day = ShowcaseBuilder.Build(data, Today);

        var cut = RenderLoaded();
        var cards = cut.FindAll(".dstop");

        Assert.Equal(data.Farms.Count, cards.Count);
        Assert.Contains("dstop--selected", cards[0].ClassList);
        Assert.Equal("true", cards[0].QuerySelector(".dstop-select")!.GetAttribute("aria-current"));
        Assert.Equal("Étape 1", cards[0].QuerySelector(".step-badge")!.TextContent);
        Assert.Equal("08h30", cards[0].QuerySelector(".dstop-time")!.TextContent);
        Assert.Contains("9,2 km", cards[0].QuerySelector(".dstop-km")!.TextContent);
        var urgent = DailyActions.Compute(data.Cows.Where(cow => cow.FarmId == data.Farms[0].Id), Today).Items.Any(item => item.Urgency == Urgency.Urgent);
        Assert.Equal(urgent ? "Priorité haute" : "En attente", cards[0].QuerySelector(".state")!.TextContent);
        Assert.Contains(day.Farms[0].Farmer, cards[0].QuerySelector(".dstop-who")!.TextContent);
        Assert.Equal(day.Farms[0].MotiveDetail, cards[0].QuerySelector(".dstop-motive-text")!.TextContent);
        Assert.Contains($"{day.Farms[0].HerdSize} têtes", cards[0].QuerySelector(".dstop-foot")!.TextContent);
        Assert.Equal(day.Farms[0].Identifier, cut.Find(".selected-id").TextContent);
        Assert.Equal(data.Farms[0].Name, cut.Find("h2.detail-title").TextContent);
    }

    [Fact]
    public void Choisir_une_etape_affiche_son_detail()
    {
        var data = SetupTour();
        var day = ShowcaseBuilder.Build(data, Today);
        var cut = RenderLoaded();

        cut.FindAll(".dstop-select")[1].Click();

        Assert.Equal(data.Farms[1].Name, cut.Find("h2.detail-title").TextContent);
        Assert.Equal(day.Farms[1].Identifier, cut.Find(".selected-id").TextContent);
        Assert.Contains("dstop--selected", cut.FindAll(".dstop")[1].ClassList);
        Assert.DoesNotContain("dstop--selected", cut.FindAll(".dstop")[0].ClassList);
        Assert.Equal(day.Farms[1].Protocol, cut.Find("h2.protocol-title").TextContent);
    }

    [Fact]
    public void Le_detail_donne_les_contacts_la_carte_et_les_actions()
    {
        var data = SetupTour();
        var info = ShowcaseBuilder.Build(data, Today).Farms[0];
        var cut = RenderLoaded();

        Assert.Equal(info.Description, cut.Find(".detail .detail-text").TextContent);
        var start = cut.Find("a.button--primary");
        Assert.Equal("Démarrer la consultation", start.TextContent);
        Assert.Equal($"regie/{info.FarmId}", start.GetAttribute("href"));
        Assert.Equal([info.Farmer, info.Phone, info.Address], cut.FindAll(".contact dd").Select(item => item.TextContent));
        Assert.Matches(@"^555-01\d\d$", info.Phone);
        Assert.Equal("Accès cour de ferme", cut.Find(".map-eyebrow").TextContent);
        Assert.Equal(info.YardAccess, cut.Find(".map-text").TextContent);
        Assert.Equal("true", cut.Find(".map-art").GetAttribute("aria-hidden"));
        Assert.Equal($"regie/{info.FarmId}", cut.Find(".quick a").GetAttribute("href"));
        Assert.Equal("Fiche complète cheptel", cut.Find(".quick a").TextContent);

        foreach (var label in new[] { "Lancer Waze / Maps", "Notifier arrivée (SMS)", "Ordonnancier élevage" })
        {
            cut.FindAll("button").Single(button => button.TextContent.Trim() == label).Click();
            Assert.Contains("fonction simulée", cut.Find(".demo").TextContent);
        }
    }

    [Fact]
    public void Le_protocole_liste_les_vaches_avec_l_action_rapide_vers_la_regie_filtree()
    {
        var data = SetupTour();
        var farm = data.Farms[0];
        var expected = FarmProtocol.Select(DailyActions.Compute(data.Cows.Where(cow => cow.FarmId == farm.Id), Today));

        var cut = RenderLoaded();
        var rows = cut.FindAll("table.cows tbody tr");

        Assert.Equal(["Boucle / tag", "Nom", "Stade", "Dernière IA", "Statut prévu", "Action rapide"], cut.FindAll("table.cows thead th").Select(th => th.TextContent));
        Assert.Equal(Math.Min(4, expected.Count), rows.Count);
        Assert.Equal(expected.Take(4).Select(item => item.Cow.Id), rows.Select(row => row.QuerySelector(".cow-id")!.TextContent));
        Assert.Equal(expected.Take(4).Select(item => item.Cow.Name), rows.Select(row => row.QuerySelectorAll("td")[0].TextContent));
        Assert.Equal(
            expected.Take(4).Select(item => $"regie/{farm.Id}?q={item.Cow.Id}"),
            rows.Select(row => row.QuerySelector("a.quick-action")!.GetAttribute("href")));
        Assert.Equal($"{expected.Count} vache(s) sélectionnée(s)", cut.Find(".protocol-head .chip").TextContent);
        Assert.Contains($"Affichage de {rows.Count} sur {expected.Count} animaux pré-chargés", cut.Find(".more").TextContent);
        Assert.Contains("Saisir DG", rows[0].QuerySelector("a.quick-action")!.TextContent);
    }

    [Fact]
    public void Afficher_les_autres_vaches_deplie_puis_replie_la_liste()
    {
        var data = SetupTour();
        var farm = data.Farms[0];
        // Six vaches à diagnostic de gestation de plus : le protocole dépasse les quatre lignes de l'aperçu.
        var template = data.Cows.First(cow => DailyActions.Compute([cow], Today).Items.Any(item => item.Motives.Any(motive => motive.Action == RegieAction.PregnancyCheck)));
        var cows = data.Cows.Where(cow => cow.FarmId == farm.Id).Concat(Enumerable.Range(0, 6).Select(number => template with { Id = $"90{number}", FarmId = farm.Id })).ToList();
        module.Setup<IReadOnlyList<Cow>>("getCowsByFarm", farm.Id).SetResult(cows);
        var total = FarmProtocol.Select(DailyActions.Compute(cows, Today)).Count;
        Assert.True(total > 4);

        var cut = RenderLoaded();
        var toggle = cut.Find("button.link-button");
        Assert.Equal($"Afficher les {total - 4} autres vaches →", toggle.TextContent.Trim());

        toggle.Click();

        Assert.Equal(total, cut.FindAll("table.cows tbody tr").Count);
        Assert.Equal("true", cut.Find("button.link-button").GetAttribute("aria-expanded"));

        cut.Find("button.link-button").Click();

        Assert.Equal(4, cut.FindAll("table.cows tbody tr").Count);
    }

    [Fact]
    public void Historique_et_telemetrie_sont_fictifs_et_propres_a_l_elevage()
    {
        var data = SetupTour();
        var info = ShowcaseBuilder.Build(data, Today).Farms[0];
        var cut = RenderLoaded();

        Assert.Equal(info.History.Select(entry => entry.Title), cut.FindAll(".history-head strong").Select(item => item.TextContent));
        Assert.Contains("Dernier : 12/09/2026", cut.Find(".pair-aside").TextContent);
        Assert.Equal("Télémétrie Robot", cut.Find("#telemetry-title").TextContent);
        Assert.Equal(["34,2 L/j", "148 k"], cut.FindAll(".metric-value").Select(item => item.TextContent));
        Assert.Contains("+4 %", cut.Find(".activity").TextContent);
        Assert.Contains("Données simulées", cut.Find("#telemetry-title").ParentElement!.TextContent);
    }

    [Fact]
    public void L_itineraire_optimise_affiche_depart_et_retour_de_la_clinique()
    {
        var data = SetupTour();
        var tour = ShowcaseBuilder.Build(data, Today).Tour;
        var cut = RenderLoaded();

        Assert.Equal("Optimisation de l'itinéraire", cut.Find("#optim-title").TextContent);
        Assert.Equal(["08h00", $"{tour.Return.Hour:D2}h{tour.Return.Minute:D2}"], cut.FindAll(".optim-time").Select(item => item.TextContent));
        Assert.Equal(data.Farms.Count, cut.FindAll(".optim-route circle").Count);
        Assert.Equal("true", cut.Find(".optim-route").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void La_recherche_filtre_les_etapes_par_nom_ou_motif()
    {
        var data = SetupTour();
        var cut = RenderLoaded();

        cut.Find(".search input").Input("erable");

        Assert.Equal([data.Farms[2].Name], cut.FindAll("h2.farm").Select(farm => farm.TextContent));
        Assert.Single(cut.FindAll(".dstop"));
        Assert.Equal(data.Farms[2].Name, cut.Find("h2.detail-title").TextContent);
        Assert.Empty(cut.FindAll(".stop--alert"));

        cut.Find(".search input").Input("zzz");

        Assert.Equal("Aucune visite ne correspond à ce filtre.", cut.Find(".status").TextContent);
        Assert.Empty(cut.FindAll(".dstop"));
    }
}
