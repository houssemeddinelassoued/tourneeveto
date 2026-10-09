using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Domain.Showcase;

/// <summary>Construit la journée de démonstration : déterministe, sans horloge ni hasard, entièrement fictive sauf ce qui vient de DemoData.</summary>
public static class ShowcaseBuilder
{
    private static readonly TimeOnly[] ArrivalTimes = [new(8, 30), new(11, 0), new(14, 15)];

    private static readonly double[] Distances = [9.2, 14.6, 11.4, 7.8];
    private static readonly int[] TravelMinutes = [18, 24, 16, 12];
    private static readonly int[] VisitMinutes = [90, 75, 60, 45];

    private static readonly string[] Farmers = ["Jean Démo", "Marie Exemple", "Luc Fictif", "Odile Imaginaire"];
    private static readonly string[] Streets = ["Rang des Trembles (fictif)", "Chemin du Lac Imaginaire", "Route des Prés Démo", "Rang de l'Érablière fictive"];
    private static readonly string[] Breeds = ["Holstein", "Holstein et Jersiaise", "Ayrshire", "Holstein croisée"];

    private static readonly string[] Instructions =
    [
        "Commencer par les vaches à tester, puis passer à la salle de traite.",
        "Prévoir le questionnaire de biosécurité avant d'entrer dans l'étable.",
        "Échantillons de lait à rapporter à la clinique avant 17 h.",
        "Vérifier le registre des traitements avec l'éleveur.",
    ];

    private static readonly string[] YardAccess =
    [
        "Portail côté nord ; cour gravelée, stationnement devant l'étable.",
        "Entrée par le chemin du lac ; laisser le véhicule près du hangar, désinfecter les bottes.",
        "Barrière à ouvrir au fond de l'allée ; cour boueuse par temps humide.",
        "Accès direct par la route ; stationnement dans la cour, côté laiterie.",
    ];

    /// <summary>Construit la journée à partir d'un jeu de démonstration.</summary>
    public static ShowcaseDay Build(DemoDataSet data, DateOnly today, RegieThresholds? thresholds = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Build(data.Farms, data.Visits, data.Cows, today, thresholds);
    }

    /// <summary>Construit la journée : une étape par élevage visité à la date <paramref name="today"/>, classées par identifiant d'élevage.</summary>
    public static ShowcaseDay Build(IReadOnlyList<Farm> farms, IReadOnlyList<Visit> visits, IReadOnlyList<Cow> cows, DateOnly today, RegieThresholds? thresholds = null)
    {
        ArgumentNullException.ThrowIfNull(farms);
        ArgumentNullException.ThrowIfNull(visits);
        ArgumentNullException.ThrowIfNull(cows);

        var rules = thresholds is { IsValid: true } ? thresholds : RegieThresholds.Default;
        var stops = visits
            .Where(visit => visit.Date == today)
            .GroupBy(visit => visit.FarmId)
            .Select(group => (Farm: farms.FirstOrDefault(farm => farm.Id == group.Key), Visit: group.First()))
            .Where(stop => stop.Farm is not null)
            .OrderBy(stop => stop.Farm!.Id, StringComparer.Ordinal)
            .ToList();

        var stages = stops.Select((stop, index) => BuildFarm(stop.Farm!, stop.Visit.Reason, index, today)).ToList();
        var grids = stops.Select(stop => (stop.Farm, Grid: DailyActions.Compute(cows.Where(cow => cow.FarmId == stop.Farm!.Id), today, rules))).ToList();

        var returnKm = stages.Count == 0 ? 0 : 8.6;
        var totalKm = stages.Sum(stage => stage.DistanceKm) + returnKm;
        var departure = new TimeOnly(8, 0);
        var lastEnd = stages.Count == 0 ? departure : stages[^1].ArrivalTime.AddMinutes(stages[^1].VisitMinutes);
        var tour = new TourShowcase(totalKm, returnKm, "Clinique", departure, stages.Count == 0 ? departure : lastEnd.AddMinutes(25), new WeatherShowcase(11, "Couvert, routes humides"));

        var subjects = grids
            .SelectMany(entry => entry.Grid.Items.Where(item => item.Urgency == Urgency.Urgent).Select(item => ToSubject(item, entry.Farm!)))
            .OrderByDescending(subject => subject.Scc)
            .ThenBy(subject => subject.Subject.CowId, StringComparer.Ordinal)
            .Select(subject => subject.Subject)
            .ToList();

        var items = grids.SelectMany(entry => entry.Grid.Items.Select(item => (entry.Farm, Item: item))).ToList();
        int CountOf(RegieAction action) => items.Count(entry => entry.Item.Motives.Any(motive => motive.Action == action));
        var firstScc = items.FirstOrDefault(entry => entry.Item.Motives.Any(motive => motive.Action == RegieAction.HighScc));
        var indicators = new ShowcaseIndicators(
            stages.Count,
            CountOf(RegieAction.PregnancyCheck),
            CountOf(RegieAction.CalvingSoon),
            rules.CalvingSoonDays,
            CountOf(RegieAction.HighScc),
            firstScc.Farm?.Name,
            items.Count,
            stages.Count(stage => stage.Tags.Any(tag => tag.Contains("biosécurité", StringComparison.OrdinalIgnoreCase))));

        var alertFarm = stages.Count == 0 ? null : stages[^1];
        var alert = new UrgentAlert(
            alertFarm?.FarmId ?? string.Empty,
            alertFarm?.FarmName ?? string.Empty,
            "Dystocie bovine",
            "Génisse en travail depuis plus de 3 h, suspicion de torsion utérine. Éleveur fictif au téléphone.",
            new TimeOnly(16, 45),
            alertFarm?.Phone ?? "555-0199");

        return new ShowcaseDay(DemoVeterinarian, stages, tour, Equipment, Pharmacy, subjects, alert, indicators, new TimeOnly(7, 45));
    }

    /// <summary>
    /// Profil fictif d'un élevage, identique à celui de la tournée s'il est visité à la date <paramref name="today"/> ;
    /// un élevage hors tournée reçoit un rang déterministe après les étapes. <c>null</c> si l'élevage est inconnu.
    /// </summary>
    public static FarmShowcase? ProfileOf(IReadOnlyList<Farm> farms, IReadOnlyList<Visit> visits, string farmId, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(farms);
        ArgumentNullException.ThrowIfNull(visits);

        var farm = farms.FirstOrDefault(candidate => candidate.Id == farmId);
        if (farm is null)
        {
            return null;
        }

        var todays = visits.Where(visit => visit.Date == today).ToList();
        var visited = todays.Select(visit => visit.FarmId).Distinct().Where(id => farms.Any(candidate => candidate.Id == id)).Order(StringComparer.Ordinal).ToList();
        var index = visited.IndexOf(farmId);
        if (index < 0)
        {
            index = visited.Count + farms.Select(candidate => candidate.Id).Except(visited).Order(StringComparer.Ordinal).ToList().IndexOf(farmId);
        }

        return BuildFarm(farm, todays.FirstOrDefault(visit => visit.FarmId == farmId)?.Reason ?? "Consultation hors tournée", index, today);
    }

    /// <summary>Profil fictif du praticien, disponible avant même le chargement des données.</summary>
    public static VeterinarianProfile DemoVeterinarian { get; } = new(
        "Dre Camille Exemple", "CE", "Praticien ruminants", "Bocage Démo", "N° d'ordre 00000 (fictif)", "Cabinet Ruminants Exemple");

    private static IReadOnlyList<EquipmentItem> Equipment { get; } =
    [
        new("Échographe portable", "100 % prêt", true),
        new("Prostaglandine F2α", "8 flacons", true),
        new("Kit obstétrical (vêlage difficile)", "Complet", true),
        new("Trousse de prélèvement stérile", "Réassort : 2 kits", false),
    ];

    private static PharmacyStock Pharmacy { get; } = new(
        "Pharmacie embarquée : véhicule 01 (fictif)",
        "Antibiotiques et prostaglandines complets · anti-inflammatoire : 2 flacons à réassortir",
        "Stock OK",
        Ok: true);

    private static readonly string[] Milking = ["Salle de traite 2x8 + robot d'appoint", "Stabulation entravée, lactoduc", "Robot de traite, stabulation libre", "Salle de traite 2x6"];

    private static readonly string[] MotiveDetails =
    [
        "Échographie + synchronisation des chaleurs",
        "Bilan des flux sanitaires et de la quarantaine",
        "Bactériologie du lait de tank et CCS élevés",
        "Registre des traitements et suivi du troupeau",
    ];

    private static readonly string[] Footers = ["Robot synchronisé", "Bilan réglementaire", "Délai d'attente lait actif", "Dossier prêt à signer"];

    private static readonly (double Liters, int Scc, string Note, int Delta)[] Telemetries =
    [
        (34.2, 148, "3 alertes de pic d'activité détectées ce matin (candidates à l'IA).", 4),
        (31.8, 172, "Rumination stable sur 7 jours, aucune alerte.", -1),
        (29.5, 236, "2 vaches en baisse de rumination à surveiller.", -3),
        (33.0, 161, "Activité normale, une chaleur probable cette semaine.", 2),
    ];

    private static FarmShowcase BuildFarm(Farm farm, string reason, int index, DateOnly today)
    {
        var telemetry = Telemetries[index % Telemetries.Length];
        var arrival = index < ArrivalTimes.Length ? ArrivalTimes[index] : ArrivalTimes[^1].AddMinutes(150 * (index - ArrivalTimes.Length + 1));
        return new FarmShowcase(
            farm.Id,
            index + 1,
            farm.Name,
            farm.Municipality,
            Farmers[index % Farmers.Length],
            $"555-01{index + 1:D2}",
            $"{12 + index * 17} {Streets[index % Streets.Length]}, {farm.Municipality}",
            arrival,
            Distances[index % Distances.Length],
            TravelMinutes[index % TravelMinutes.Length],
            VisitMinutes[index % VisitMinutes.Length],
            Breeds[index % Breeds.Length],
            farm.CowCount,
            TagsOf(reason),
            Instructions[index % Instructions.Length],
            YardAccess[index % YardAccess.Length],
            reason,
            $"QC-{farm.Id}-{5000 + index * 137}",
            $"{farm.CowCount} {Breeds[index % Breeds.Length]} en lactation · {Milking[index % Milking.Length]}",
            MotiveDetails[index % MotiveDetails.Length],
            Footers[index % Footers.Length],
            ProtocolOf(reason),
            [
                new HistoryEntry("Bilan reproduction", $"{72 + index * 3} % gestation", "Très bon résultat suite au dernier protocole de synchronisation (fictif).", today.AddDays(-26)),
                new HistoryEntry("Statut vaccinal du cheptel", "Conforme", "Vaccin BVD à jour, sérologies IBR indemnes (fictif).", today.AddDays(-118)),
            ],
            new TelemetryShowcase(telemetry.Liters, telemetry.Scc, telemetry.Note, telemetry.Delta));
    }

    private static string ProtocolOf(string reason)
    {
        if (reason.Contains("reproduction", StringComparison.OrdinalIgnoreCase))
        {
            return "Échographie Repro & Synchronisation PGF2α";
        }

        if (reason.Contains("biosécurité", StringComparison.OrdinalIgnoreCase))
        {
            return "Audit biosécurité & bilan sanitaire";
        }

        return reason.Contains("lait", StringComparison.OrdinalIgnoreCase) ? "Contrôle CCS & prélèvements de lait" : reason;
    }

    private static IReadOnlyList<string> TagsOf(string reason)
    {
        var tags = new List<string>();
        if (reason.Contains("reproduction", StringComparison.OrdinalIgnoreCase))
        {
            tags.Add("Suivi de reproduction");
            tags.Add("DG échographiques");
        }

        if (reason.Contains("biosécurité", StringComparison.OrdinalIgnoreCase))
        {
            tags.Add("Audit biosécurité");
        }

        if (reason.Contains("lait", StringComparison.OrdinalIgnoreCase))
        {
            tags.Add("Qualité du lait");
            tags.Add("CCS et mammites");
        }

        if (tags.Count == 0)
        {
            tags.Add(reason);
        }

        return tags;
    }

    private static (WatchedSubject Subject, int Scc) ToSubject(RegieItem item, Farm farm)
    {
        var scc = item.Cow.LastSccThousands ?? 0;
        var subject = new WatchedSubject(
            item.Cow.Id,
            item.Cow.Name,
            farm.Id,
            farm.Name,
            "Mammite subclinique",
            $"{farm.Name} · {scc} 000 cellules/mL au dernier contrôle laitier.",
            "Prélèvement stérile prévu");
        return (subject, scc);
    }
}
