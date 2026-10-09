using System.Globalization;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;

namespace TourneeVeto.Domain.Showcase;

// Fiche fictive d'une vache de la grille de régie (maquettes design/stitch/*/regie.png).
// Tout est déterministe, dérivé de la vache réelle (DemoData), de son motif principal et de la date du jour.

/// <summary>Mesure compacte d'une carte vache (ex. « T° rectale », « 38,1 °C »).</summary>
/// <param name="Label">Intitulé de la mesure.</param>
/// <param name="Value">Valeur déjà formatée.</param>
/// <param name="Alert">Vrai si la valeur est à signaler en rouge.</param>
public sealed record CowMeasure(string Label, string Value, bool Alert = false);

/// <summary>Tuile d'information de la fiche (stade physiologique, bilan CCS, mesure de surveillance…).</summary>
/// <param name="Label">Intitulé en capitales.</param>
/// <param name="Value">Valeur principale.</param>
/// <param name="Note">Précision sous la valeur.</param>
/// <param name="Alert">Vrai si la tuile est à signaler en rouge.</param>
public sealed record InfoTile(string Label, string Value, string Note, bool Alert = false);

/// <summary>Bloc de surveillance propre au motif principal.</summary>
/// <param name="Title">Titre (ex. « Surveillance périnatale &amp; pré-vêlage »).</param>
/// <param name="Subtitle">Sous-titre.</param>
/// <param name="Badge">Pastille d'échéance (ex. « VÊLAGE SOUS 48 H »).</param>
/// <param name="Tiles">Trois tuiles de mesures fictives.</param>
public sealed record SurveillanceBlock(string Title, string Subtitle, string Badge, IReadOnlyList<InfoTile> Tiles);

/// <summary>CCS d'un mois, en milliers de cellules/mL.</summary>
/// <param name="Month">Premier jour du mois.</param>
/// <param name="Thousands">CCS du mois.</param>
public sealed record SccPoint(DateOnly Month, int Thousands);

/// <summary>Intervention sanitaire passée (fictive).</summary>
/// <param name="Date">Date de l'intervention.</param>
/// <param name="Diagnosis">Diagnostic ou motif.</param>
/// <param name="Molecule">Molécule et posologie.</param>
/// <param name="WaitingTimes">Temps d'attente lait et viande.</param>
/// <param name="Status">Statut (Clôturé, Résolu, Surveillé).</param>
public sealed record Intervention(DateOnly Date, string Diagnosis, string Molecule, string WaitingTimes, string Status);

/// <summary>Données fictives d'une vache : identité, mesures, surveillance, courbe CCS et interventions.</summary>
/// <param name="NationalId">Identifiant national fictif (« CA QC 0894 4812 »).</param>
/// <param name="Sire">Père (fictif).</param>
/// <param name="Isu">Indice synthétique d'utilité fictif.</param>
/// <param name="ProductionKg">Production laitière actuelle en kg/j ; <c>null</c> pour une vache tarie ou une génisse.</param>
/// <param name="ProductionDeltaKg">Écart avec la moyenne du troupeau, en kg/j.</param>
/// <param name="WeightKg">Poids estimé.</param>
/// <param name="Statement">Phrase de statut du motif, affichée dans l'encadré de la carte.</param>
/// <param name="Measures">Trois mesures de la carte compacte.</param>
/// <param name="Stage">Tuile du stade physiologique.</param>
/// <param name="LastCalving">Tuile du dernier vêlage.</param>
/// <param name="SccBalance">Tuile du bilan CCS.</param>
/// <param name="Delivery">Tuile du statut.</param>
/// <param name="Surveillance">Bloc de surveillance selon le motif.</param>
/// <param name="SccTrend">Six mois de CCS, le dernier étant le dernier contrôle réel.</param>
/// <param name="SccAlertThousands">Seuil d'alerte du CCS.</param>
/// <param name="Interventions">Les trois dernières interventions.</param>
public sealed record CowShowcase(
    string NationalId,
    string Sire,
    int Isu,
    double? ProductionKg,
    double ProductionDeltaKg,
    int WeightKg,
    string Statement,
    IReadOnlyList<CowMeasure> Measures,
    InfoTile Stage,
    InfoTile LastCalving,
    InfoTile SccBalance,
    InfoTile Delivery,
    SurveillanceBlock Surveillance,
    IReadOnlyList<SccPoint> SccTrend,
    int SccAlertThousands,
    IReadOnlyList<Intervention> Interventions);

/// <summary>Construit la fiche fictive d'une vache à partir de sa ligne de régie.</summary>
public static class CowShowcaseBuilder
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-CA");

    private static readonly string[] Sires = ["ORIGINAL DÉMO", "VALLON FICTIF", "BORÉAL EXEMPLE", "NORDIK IMAGINAIRE", "ÉRABLE DÉMO", "SAGUENAY FICTIF"];

    private static readonly double[] TrendFactors = [0.55, 0.7, 0.5, 0.85, 0.65, 0.9, 0.6];

    /// <summary>Construit la fiche ; <paramref name="thresholds"/> fixe le seuil d'alerte du CCS (valeurs par défaut sinon).</summary>
    public static CowShowcase Build(RegieItem item, DateOnly today, RegieThresholds? thresholds = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        var rules = thresholds is { IsValid: true } ? thresholds : RegieThresholds.Default;
        var cow = item.Cow;
        var seed = Seed(cow);
        var motive = item.Motives.FirstOrDefault();
        var action = motive?.Action;
        var days = motive?.Days;
        var scc = cow.LastSccThousands;
        var alertScc = scc is int value && value > rules.HighSccThousands;

        double? production = cow.Status == ReproStatus.Dry || cow.IsHeifer ? null : 22.0 + seed % 140 / 10.0;
        var productionText = production is double kg ? $"{kg.ToString("0.0", French)} kg/j" : "—";

        var farmCode = 800 + (Digits(cow.FarmId) * 47 % 190);
        var daysSinceCalving = cow.LastCalving is DateOnly calved ? today.DayNumber - calved.DayNumber : (int?)null;

        var stage = new InfoTile(
            "Stade physiologique",
            action == RegieAction.CalvingSoon ? "Fin de gestation" : StageOf(cow.Status),
            cow.LastInsemination is DateOnly bred && cow.Status is ReproStatus.Bred or ReproStatus.Pregnant or ReproStatus.Dry
                ? $"{today.DayNumber - bred.DayNumber} j depuis l'IA"
                : "Aucune IA en cours");
        var lastCalving = new InfoTile(
            "Dernier vêlage",
            daysSinceCalving is int since ? $"{since} jours" : "—",
            cow.LastCalving is DateOnly date ? $"Le {date.ToString("dd'/'MM'/'yyyy", French)}" : "Génisse, jamais vêlée");
        var sccBalance = new InfoTile(
            "Bilan CCS cumulé",
            scc is int last ? $"{last}k sp/mL" : "Aucun contrôle",
            alertScc ? "Quartier à surveiller" : "Quartiers sains",
            alertScc);
        var delivery = new InfoTile(
            "Statut",
            item.Urgency switch { Urgency.Urgent => "Prioritaire", Urgency.Warning => "À surveiller", Urgency.Ok => "Normal", _ => "À faire" },
            item.Anomaly is null ? "Dossier complet" : "Donnée à corriger",
            item.Urgency == Urgency.Urgent);

        return new CowShowcase(
            $"CA QC {farmCode:D4} {cow.Id}",
            Sires[seed % Sires.Length],
            100 + seed % 60,
            production,
            (seed / 7 % 40 - 10) / 10.0,
            cow.IsHeifer ? 380 + seed % 90 : 600 + seed % 120,
            StatementOf(action),
            MeasuresOf(action, days, cow, seed, rules, productionText),
            stage,
            lastCalving,
            sccBalance,
            delivery,
            SurveillanceOf(action, days, cow, seed, rules),
            Trend(scc, seed, today, rules),
            rules.HighSccThousands,
            Interventions(cow, seed, today));
    }

    private static CowMeasure[] MeasuresOf(RegieAction? action, int? days, Cow cow, int seed, RegieThresholds rules, string production)
    {
        var scc = cow.LastSccThousands;
        return action switch
        {
            RegieAction.CalvingSoon =>
            [
                new("T° rectale", $"{Decimal(38.0 + seed % 8 / 10.0)} °C", Alert: seed % 3 == 0),
                new("Jours gest.", days is int left ? $"{rules.GestationDays - left} j" : "—"),
                new("Logette", $"Paillée #{1 + seed % 9:D2}"),
            ],
            RegieAction.PregnancyCheck =>
            [
                new("Dernière IA", cow.LastInsemination is DateOnly bred ? bred.ToString("dd'/'MM", French) : "—"),
                new("Prod. du jour", production),
                new("Chaleurs", "Négatives"),
            ],
            RegieAction.HighScc =>
            [
                new("Cellules", scc is int value ? $"{value}k sp/mL" : "—", Alert: true),
                new("Conductivité", $"{Decimal(6.0 + seed % 20 / 10.0)} mS/cm"),
                new("Chute lait", $"-{Decimal(1.0 + seed % 40 / 10.0)} kg/j"),
            ],
            RegieAction.DryOff =>
            [
                new("Dernier CCS", scc is int value ? $"{value}k sp/mL" : "—"),
                new("Production", production),
                new("Stratégie", "Obturateur seul"),
            ],
            RegieAction.PostCalvingCheck =>
            [
                new("Post-vêlage", days is int since ? $"{since} j" : "—"),
                new("Prod. du jour", production),
                new("Utérus", "À palper"),
            ],
            _ =>
            [
                new("Dernier vêlage", cow.LastCalving is DateOnly calved ? calved.ToString("dd'/'MM", French) : "—"),
                new("Prod. du jour", production),
                new("Statut", StageOf(cow.Status)),
            ],
        };
    }

    private static SurveillanceBlock SurveillanceOf(RegieAction? action, int? days, Cow cow, int seed, RegieThresholds rules)
    {
        var temp = 38.0 + seed % 8 / 10.0;
        var scc = cow.LastSccThousands;
        return action switch
        {
            RegieAction.CalvingSoon => new SurveillanceBlock(
                "Surveillance périnatale & pré-vêlage",
                "Signes cliniques imminents détectés par capteur thermique et oculaire",
                days switch { null => "VÊLAGE PRÉVU", < 0 => "VÊLAGE DÉPASSÉ", <= 2 => "VÊLAGE SOUS 48 H", var left => $"VÊLAGE DANS {left} J" },
                [
                    new("T° rectale J-0", $"{Decimal(temp)} °C", $"Chute thermique de {Decimal(0.3 + seed % 6 / 10.0)} °C confirmée.", Alert: true),
                    new("Logette & environnement", $"Logette #{1 + seed % 9:D2}", "Isolement validé hier. Paillage sain, désinfection à la chaux réalisée."),
                    new("Réfractomètre Brix", $"{22 + seed % 6} % Brix", "Colostrum prélevé en test. Kit tétine prêt."),
                ]),
            RegieAction.PregnancyCheck => new SurveillanceBlock(
                "Suivi de reproduction & diagnostic de gestation",
                "Fenêtre d'échographie ouverte depuis l'insémination",
                days is int since ? $"IA J+{since}" : "DG À FAIRE",
                [
                    new("Jours depuis l'IA", days is int elapsed ? $"{elapsed} j" : "—", $"Fenêtre du DG : {rules.PregnancyCheckFromDays} à {rules.PregnancyCheckToDays} jours."),
                    new("Échographe", "Sonde prête", "Batterie à 100 %, gel de contact disponible."),
                    new("Semence", "Sexée", "Paillette fictive du taureau de la dernière IA."),
                ]),
            RegieAction.HighScc => new SurveillanceBlock(
                "Surveillance de la santé mammaire",
                "Suspicion de mammite détectée au contrôle laitier et au robot de traite",
                "CCS ÉLEVÉ",
                [
                    new("Cellules", scc is int value ? $"{value}k sp/mL" : "—", $"Seuil d'alerte à {rules.HighSccThousands}k sp/mL.", Alert: true),
                    new("Conductivité", $"{Decimal(6.0 + seed % 20 / 10.0)} mS/cm", "Mesure du robot de ce matin."),
                    new("Quartier suspect", seed % 2 == 0 ? "Arrière gauche" : "Avant droit", "Test CMT californien à réaliser."),
                ]),
            RegieAction.DryOff => new SurveillanceBlock(
                "Préparation au tarissement",
                "Vêlage prévu dans la fenêtre de tarissement",
                days is int left ? (left >= 0 ? $"VÊLAGE J-{left}" : $"VÊLAGE J+{-left}") : "TARISSEMENT",
                [
                    new("Dernier CCS", scc is int value ? $"{value}k sp/mL" : "—", "Contrôle de fin de lactation."),
                    new("État corporel", $"{(2.75 + seed % 5 / 4.0).ToString("0.00", French)} / 5", "Note d'état corporel fictive."),
                    new("Produit", "Obturateur interne", "Antibiotique hors-lait selon le protocole."),
                ]),
            RegieAction.PostCalvingCheck => new SurveillanceBlock(
                "Suivi post-vêlage",
                "Contrôle de l'involution utérine et de l'état général",
                days is int since ? $"VÊLAGE J+{since}" : "POST-VÊLAGE",
                [
                    new("Écoulements", "Normaux", "Aucun signe de métrite."),
                    new("Appétit", "Bon", "Consommation de la ration stable."),
                    new("T° rectale", $"{Decimal(temp)} °C", "Dans la plage normale."),
                ]),
            _ => new SurveillanceBlock(
                action == RegieAction.NotInseminated ? "Reprise de la reproduction" : "Donnée à vérifier",
                action == RegieAction.NotInseminated ? "Vache vide : chaleurs à repérer" : "Corriger la donnée signalée avant toute décision",
                action == RegieAction.NotInseminated ? "VACHE VIDE" : "ANOMALIE",
                [
                    new("Dernier vêlage", cow.LastCalving is DateOnly calved ? calved.ToString("dd'/'MM'/'yyyy", French) : "—", "Date issue du dossier."),
                    new("Activité", "Normale", "Podomètre : aucune alerte de chaleur."),
                    new("Protocole", "À décider", "Synchronisation PGF2α possible."),
                ]),
        };
    }

    private static string StatementOf(RegieAction? action) => action switch
    {
        RegieAction.CalvingSoon => "Surveillance colostrum prête et transfert en logette paillée dédiée effectué hier soir.",
        RegieAction.PregnancyCheck => "À échographier aujourd'hui (semence sexée).",
        RegieAction.DryOff => "Antibiotique hors-lait ou obturateur interne à injecter après la dernière traite.",
        RegieAction.HighScc => "Test CMT californien à réaliser impérativement. Conductivité du lait anormale relevée ce matin au robot.",
        RegieAction.PostCalvingCheck => "Contrôle de l'involution utérine et de l'état général.",
        RegieAction.NotInseminated => "Vache vide : chaleurs à repérer et protocole de synchronisation à décider.",
        _ => "Donnée à corriger avant toute décision.",
    };

    private static string StageOf(ReproStatus status) => status switch
    {
        ReproStatus.Open => "Vache vide",
        ReproStatus.Bred => "Inséminée",
        ReproStatus.Pregnant => "Gestante",
        _ => "Tarie",
    };

    // Cinq mois antérieurs déduits du dernier CCS réel (le dernier point est ce CCS) ; sans contrôle, repli sous le seuil.
    private static List<SccPoint> Trend(int? last, int seed, DateOnly today, RegieThresholds rules)
    {
        var reference = last ?? Math.Min(90, rules.HighSccThousands - 1);
        var month = new DateOnly(today.Year, today.Month, 1);
        var points = new List<SccPoint>();
        for (var back = 5; back >= 1; back--)
        {
            var value = Math.Max(30, (int)Math.Round(reference * TrendFactors[(seed + back) % TrendFactors.Length]));
            points.Add(new SccPoint(month.AddMonths(-back), value));
        }

        points.Add(new SccPoint(month, reference));
        return points;
    }

    private static Intervention[] Interventions(Cow cow, int seed, DateOnly today) =>
    [
        new(today.AddDays(-(40 + seed % 20)), $"Tarissement L{Math.Max(1, cow.Lactation)} (sélectif)", "Obturateur intra-mammaire bismuth", "Lait : 0 j · Viande : 0 j", "Clôturé"),
        new(today.AddDays(-(150 + seed % 30)), "Boiterie membre postérieur droit", "Kétoprofène 3 mg/kg IV (AINS)", "Lait : 0 h · Viande : 4 j", "Résolu"),
        new(today.AddDays(-(300 + seed % 40)), "Vêlage : rétention placentaire", "Ocytocine + décollement manuel", "Lait : 0 j · Viande : 0 j", "Surveillé"),
    ];

    private static string Decimal(double value) => value.ToString("0.0", French);

    private static int Seed(Cow cow) => cow.Id.Aggregate(17, (hash, character) => unchecked(hash * 31 + character)) & 0x7fffffff;

    private static int Digits(string text) => int.TryParse(new string([.. text.Where(char.IsDigit)]), out var value) ? value : 0;
}
