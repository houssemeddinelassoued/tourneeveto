using System.Text.RegularExpressions;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Showcase;

namespace TourneeVeto.Tests.Domain.Showcase;

/// <summary>Modèle fictif d'enrichissement des écrans (accueil, tournée, régie, rapport), dérivé de DemoData et de la date.</summary>
public class ShowcaseTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static readonly DemoDataSet Data = DemoData.Generate(Today, 2026);

    private static ShowcaseDay Build() => ShowcaseBuilder.Build(Data, Today);

    [Fact]
    public void Est_deterministe_pour_une_meme_date()
    {
        Assert.Equivalent(Build(), Build(), strict: true);
    }

    [Fact]
    public void Le_veterinaire_est_manifestement_fictif()
    {
        var vet = Build().Veterinarian;

        Assert.Equal("Dre Camille Exemple", vet.Name);
        Assert.Equal("CE", vet.Initials);
        Assert.Equal("Praticien ruminants", vet.Title);
        Assert.False(string.IsNullOrWhiteSpace(vet.Sector));
        Assert.Contains("fictif", vet.OrderNumber, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Une_etape_par_elevage_visite_dans_l_ordre_des_identifiants()
    {
        var farms = Build().Farms;

        Assert.Equal(Data.Farms.Select(farm => farm.Id), farms.Select(farm => farm.FarmId));
        Assert.Equal([1, 2, 3], farms.Select(farm => farm.Order));
        Assert.Equal(Data.Farms.Select(farm => farm.CowCount), farms.Select(farm => farm.HerdSize));
    }

    [Fact]
    public void Les_heures_de_passage_se_suivent_et_commencent_a_8h30()
    {
        var times = Build().Farms.Select(farm => farm.ArrivalTime).ToList();

        Assert.Equal(new TimeOnly(8, 30), times[0]);
        Assert.Equal(new TimeOnly(11, 0), times[1]);
        Assert.Equal(new TimeOnly(14, 15), times[2]);
        Assert.Equal(times.OrderBy(time => time), times);
    }

    [Fact]
    public void Les_telephones_sont_fictifs_et_les_coordonnees_renseignees()
    {
        Assert.All(Build().Farms, farm =>
        {
            Assert.Matches(new Regex(@"^555-01\d\d$"), farm.Phone);
            Assert.False(string.IsNullOrWhiteSpace(farm.Farmer));
            Assert.False(string.IsNullOrWhiteSpace(farm.Address));
            Assert.False(string.IsNullOrWhiteSpace(farm.Breed));
            Assert.False(string.IsNullOrWhiteSpace(farm.Instruction));
            Assert.False(string.IsNullOrWhiteSpace(farm.YardAccess));
            Assert.NotEmpty(farm.Tags);
            Assert.True(farm.DistanceKm > 0);
            Assert.True(farm.TravelMinutes > 0);
            Assert.True(farm.VisitMinutes > 0);
        });
    }

    [Fact]
    public void Les_motifs_reprennent_le_motif_de_la_visite()
    {
        var farms = Build().Farms;

        Assert.Contains(farms[0].Tags, tag => tag.Contains("reproduction", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(farms[1].Tags, tag => tag.Contains("biosécurité", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(farms[2].Tags, tag => tag.Contains("lait", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Le_kilometrage_total_est_la_somme_des_trajets_et_du_retour()
    {
        var showcase = Build();

        Assert.Equal(showcase.Farms.Sum(farm => farm.DistanceKm) + showcase.Tour.ReturnDistanceKm, showcase.Tour.TotalKm, precision: 5);
        Assert.True(showcase.Tour.Departure < showcase.Farms[0].ArrivalTime);
        Assert.True(showcase.Tour.Return > showcase.Farms[^1].ArrivalTime);
        Assert.False(string.IsNullOrWhiteSpace(showcase.Tour.Weather.Conditions));
    }

    [Fact]
    public void Le_materiel_et_la_pharmacie_sont_fournis()
    {
        var showcase = Build();

        Assert.True(showcase.Equipment.Count >= 4);
        Assert.Contains(showcase.Equipment, item => item.Name.Contains("chographe"));
        Assert.Contains(showcase.Equipment, item => item.Status.Contains("flacon"));
        Assert.False(string.IsNullOrWhiteSpace(showcase.Pharmacy.Summary));
        Assert.False(string.IsNullOrWhiteSpace(showcase.Pharmacy.Status));
    }

    [Fact]
    public void Les_sujets_sous_surveillance_sont_les_vraies_vaches_urgentes()
    {
        var showcase = Build();
        var expected = Data.Farms
            .SelectMany(farm => DailyActions.Compute(Data.Cows.Where(cow => cow.FarmId == farm.Id), Today).Items)
            .Where(item => item.Urgency == Urgency.Urgent)
            .Select(item => item.Cow.Id)
            .Order();

        Assert.NotEmpty(showcase.Subjects);
        Assert.Equal(expected, showcase.Subjects.Select(subject => subject.CowId).Order());
        Assert.All(showcase.Subjects, subject =>
        {
            var cow = Data.Cows.Single(candidate => candidate.Id == subject.CowId);
            Assert.Equal(cow.FarmId, subject.FarmId);
            Assert.Equal(cow.Name, subject.CowName);
            Assert.Contains(cow.LastSccThousands!.Value.ToString(), subject.Detail);
        });
    }

    [Fact]
    public void L_urgence_signalee_est_fictive_et_rattachee_a_un_elevage_de_la_tournee()
    {
        var showcase = Build();

        Assert.Contains(Data.Farms, farm => farm.Id == showcase.Alert.FarmId);
        Assert.Matches(new Regex(@"^555-01\d\d$"), showcase.Alert.Phone);
        Assert.False(string.IsNullOrWhiteSpace(showcase.Alert.Title));
        Assert.True(showcase.Alert.Time > showcase.Farms[0].ArrivalTime);
    }

    [Fact]
    public void Les_indicateurs_viennent_de_la_regie()
    {
        var showcase = Build();
        var items = Data.Farms
            .SelectMany(farm => DailyActions.Compute(Data.Cows.Where(cow => cow.FarmId == farm.Id), Today).Items)
            .ToList();
        int Count(RegieAction action) => items.Count(item => item.Motives.Any(motive => motive.Action == action));

        Assert.Equal(Data.Visits.Count, showcase.Indicators.VisitsPlanned);
        Assert.Equal(Count(RegieAction.PregnancyCheck), showcase.Indicators.PregnancyChecks);
        Assert.Equal(Count(RegieAction.CalvingSoon), showcase.Indicators.CalvingsImminent);
        Assert.Equal(Count(RegieAction.HighScc), showcase.Indicators.SccAlerts);
        Assert.Equal(items.Count, showcase.Indicators.RegieRows);
        Assert.NotNull(showcase.Indicators.SccFarmName);
    }

    [Fact]
    public void Les_barres_de_progression_restent_entre_0_et_100()
    {
        var indicators = Build().Indicators;

        Assert.Equal((int)Math.Round(100.0 * indicators.PregnancyChecks / indicators.RegieRows), indicators.ReproSharePercent);
        Assert.InRange(indicators.VigilanceSharePercent, 1, 100);
        Assert.Equal(0, indicators.TourProgressPercent);
        Assert.Equal(67, indicators.DossiersReadyPercent);
        Assert.Equal(0, ShowcaseBuilder.Build(Data.Farms, [], Data.Cows, Today).Indicators.DossiersReadyPercent);
    }

    [Fact]
    public void Sans_visite_la_tournee_est_vide_sans_planter()
    {
        var showcase = ShowcaseBuilder.Build(Data.Farms, [], Data.Cows, Today);

        Assert.Empty(showcase.Farms);
        Assert.Equal(0, showcase.Indicators.VisitsPlanned);
        Assert.Equal(0, showcase.Tour.TotalKm);
        Assert.Empty(showcase.Subjects);
    }

    [Fact]
    public void Aucun_nom_des_maquettes_n_est_repris()
    {
        var showcase = Build();
        var text = string.Join(' ', showcase.Farms.SelectMany(farm => new[] { farm.Farmer, farm.Address, farm.Instruction }))
            + showcase.Veterinarian.Name + showcase.Alert.Description;

        Assert.DoesNotContain("GAEC", text);
        Assert.DoesNotContain("Morel", text);
        Assert.DoesNotContain("Laurent", text);
    }

    [Fact]
    public void La_recherche_sans_resultat_reste_sur_l_elevage_courant_et_filtre()
    {
        var target = SearchTarget.Resolve("zzz-introuvable", Data.Farms, Data.Cows, "F003");

        Assert.Equal("F003", target.FarmId);
        Assert.Equal("zzz-introuvable", target.Query);
    }

    [Fact]
    public void La_recherche_par_numero_de_vache_mene_a_son_elevage()
    {
        var cow = Data.Cows.First(candidate => candidate.FarmId == "F002");

        var target = SearchTarget.Resolve(cow.Id, Data.Farms, Data.Cows, "F001");

        Assert.Equal("F002", target.FarmId);
        Assert.Equal(cow.Id, target.Query);
    }

    [Fact]
    public void La_recherche_par_nom_d_elevage_ouvre_la_grille_sans_filtre()
    {
        var target = SearchTarget.Resolve("laitière démo", Data.Farms, Data.Cows, "F001");

        Assert.Equal("F002", target.FarmId);
        Assert.Null(target.Query);
    }

    [Fact]
    public void La_recherche_vide_ne_filtre_rien()
    {
        var target = SearchTarget.Resolve("  ", Data.Farms, Data.Cows, null);

        Assert.Equal("F001", target.FarmId);
        Assert.Null(target.Query);
    }
}
