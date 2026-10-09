using TourneeVeto.Domain.Biosecurity;

namespace TourneeVeto.Domain.Showcase;

/// <summary>État d'une rubrique dans la liste « Sections d'audit » (maquette web/biosecurite.png).</summary>
public enum SectionStatus
{
    /// <summary>Aucune réponse saisie.</summary>
    ToAudit,

    /// <summary>Des réponses évaluables, mais la rubrique n'est ni complète ni conforme.</summary>
    InProgress,

    /// <summary>Niveau de risque faible et toutes les questions de la rubrique renseignées.</summary>
    Compliant,

    /// <summary>Niveau de risque élevé (score inférieur à 50 ou point critique à Non).</summary>
    Vigilance,

    /// <summary>Réponses saisies mais toutes Sans objet : aucune note possible.</summary>
    NotEvaluated,
}

/// <summary>Fiche fictive d'une rubrique : description et pictogramme.</summary>
public sealed record SectionInfo(string Description, string Icon);

/// <summary>Textes fictifs d'un critère : description, observation si Partiel, constat si Non, préconisation.</summary>
public sealed record CriterionInfo(string Description, string PartialObservation, string ClinicalFinding, string Recommendation);

/// <summary>Synthèse de l'audit affichée sous l'indice global.</summary>
/// <param name="ValidatedPoints">Questions répondues Oui.</param>
/// <param name="TotalPoints">Questions du questionnaire.</param>
/// <param name="CriticalAlerts">Questions critiques répondues Non.</param>
/// <param name="SafetyThresholdReached">Indice global au moins égal au seuil de sécurité.</param>
/// <param name="SectionsEntered">Rubriques qui ont au moins une réponse.</param>
public sealed record AuditSummary(int ValidatedPoints, int TotalPoints, int CriticalAlerts, bool SafetyThresholdReached, int SectionsEntered);

/// <summary>Carte « photo du périmètre » (illustration fictive).</summary>
public sealed record PerimeterShowcase(string Heading, string Place, DateOnly LastDisinfection);

/// <summary>Données fictives et synthèses du bilan de biosécurité ; le score et les pratiques prioritaires restent ceux de <see cref="BiosecurityScore"/>.</summary>
public static class BiosecurityAudit
{
    /// <summary>Seuil de sécurité de l'indice global : le seuil du niveau « modéré » du score (50).</summary>
    public const int SafetyThreshold = 50;

    private static readonly SectionInfo DefaultSection = new("Contrôle des mesures de biosécurité de la rubrique (fictif).", "shield");

    private static readonly Dictionary<string, SectionInfo> Sections = new(StringComparer.Ordinal)
    {
        ["Introduction d'animaux"] = new("Contrôle des achats, de la quarantaine et du retour des animaux de la ferme.", "cart"),
        ["Visiteurs et véhicules"] = new("Contrôle des flux humains et matériels extérieurs (vétérinaires, inséminateurs, camions, techniciens).", "door"),
        ["Vêlage et veaux"] = new("Contrôle de l'hygiène du local de vêlage, du colostrum et de l'isolement des veaux malades.", "calf"),
        ["Animaux morts et nuisibles"] = new("Contrôle du stockage des animaux morts et de la lutte contre les rongeurs, oiseaux et chats.", "bug"),
        ["Hygiène de la traite"] = new("Contrôle de la désinfection des trayons, de l'ordre de traite et de l'équipement.", "drop"),
    };

    private static readonly CriterionInfo DefaultCriterion = new(
        "Point de contrôle fictif de la rubrique.",
        "Mesure présente mais incomplète (fictif).",
        "Mesure absente lors de la visite (fictif).",
        "Appliquer la mesure et la vérifier au prochain passage (fictif).");

    private static readonly Dictionary<string, CriterionInfo> Criteria = new(StringComparer.Ordinal)
    {
        ["intro-quarantaine"] = new("Local ou enclos dédié, à l'écart du troupeau, avec soins en dernier.", "Quarantaine en place mais moins de 21 jours pour certains achats.", "Aucune quarantaine : les animaux achetés rejoignent directement le troupeau.", "Aménager un enclos de quarantaine de 21 jours minimum avant toute introduction."),
        ["intro-statut"] = new("Attestation sanitaire du vendeur et tests de dépistage avant l'achat.", "Statut demandé au vendeur, mais sans attestation écrite.", "Aucune vérification du statut sanitaire avant l'achat.", "Exiger une attestation sanitaire et un dépistage avant tout achat."),
        ["intro-retour"] = new("Isolement à leur retour des animaux revenus d'une exposition ou d'une pension.", "Isolement effectué, mais de moins de 14 jours.", "Les animaux de retour rejoignent le troupeau sans isolement.", "Isoler tout animal de retour pendant au moins 14 jours."),
        ["visit-registre"] = new("Registre papier ou numérique avec date, motif et provenance des visiteurs.", "Registre tenu, mais des passages manquent.", "Registre vierge depuis plusieurs mois.", "Remettre un registre à l'entrée et le faire signer à chaque visiteur."),
        ["visit-bottes"] = new("Point d'eau, savon bactéricide et équipement de protection propres à l'exploitation.", "Bottes présentes mais manque de couvre-bottes jetables pour les livraisons.", "Aucun équipement propre n'est mis à la disposition des visiteurs.", "Fournir bottes et couvre-bottes propres, avec pédiluve renouvelé."),
        ["visit-camions"] = new("Distance de sécurité ou barrière physique entre le stationnement des camions et les animaux.", "Aire de manœuvre en partie commune avec la zone des animaux.", "Les camions entrent dans la cour des animaux, sans séparation.", "Délimiter une aire de stationnement et de chargement hors de la zone des animaux."),
        ["veau-local"] = new("Nettoyage et nouvelle litière entre deux vêlages.", "Nettoyage fait, mais pas systématiquement entre deux vêlages.", "Litière souillée réutilisée d'un vêlage à l'autre.", "Nettoyer et pailler le local entre chaque vêlage."),
        ["veau-colostrum"] = new("Au moins 4 litres de colostrum de qualité dans les 6 premières heures.", "Colostrum donné, mais parfois après plus de 6 heures.", "Colostrum non contrôlé : plusieurs veaux sans prise suffisante.", "Donner le colostrum dans les 6 heures et en contrôler la qualité."),
        ["veau-malade"] = new("Case d'isolement pour les veaux malades, soignés en dernier.", "Isolement possible mais case commune avec la nurserie.", "Veaux malades gardés avec les autres veaux.", "Séparer les veaux malades et les soigner après les veaux sains."),
        ["mort-stockage"] = new("Aire de stockage couverte, hors du trajet du camion de collecte.", "Aire de stockage présente, mais près du passage du camion.", "Animaux morts laissés à proximité des animaux vivants.", "Déplacer l'aire de stockage en bordure de la propriété."),
        ["mort-rongeurs"] = new("Postes d'appâts contrôlés et plan de lutte écrit.", "Plan de lutte appliqué, mais postes non vérifiés régulièrement.", "Aucun plan de lutte : traces de rongeurs visibles.", "Installer des postes d'appâts et contrôler leur état chaque mois."),
        ["mort-oiseaux"] = new("Aliments couverts et accès limité pour les oiseaux et les chats.", "Aliments couverts en partie seulement.", "Aliments accessibles aux oiseaux et aux chats.", "Couvrir les aliments et fermer les ouvertures du hangar."),
        ["traite-trayons"] = new("Trempage ou pulvérisation des trayons après chaque traite.", "Désinfection faite, mais pas à chaque traite.", "Aucune désinfection des trayons après la traite.", "Désinfecter les trayons après chaque traite avec un produit homologué."),
        ["traite-mammites"] = new("Ordre de traite : vaches saines d'abord, vaches à mammite en dernier.", "Ordre respecté, mais sans griffe dédiée.", "Les vaches à mammite sont traites avec le reste du troupeau.", "Traire les vaches à mammite en dernier ou avec une griffe dédiée."),
        ["traite-controle"] = new("Contrôle annuel de l'équipement de traite par un technicien.", "Contrôle fait, mais datant de plus d'un an.", "Aucun contrôle de l'équipement de traite.", "Planifier un contrôle annuel de l'installation de traite."),
    };

    /// <summary>Fiche fictive d'une rubrique ; une fiche générique pour une rubrique inconnue.</summary>
    public static SectionInfo Section(string section) => Sections.GetValueOrDefault(section, DefaultSection);

    /// <summary>Textes fictifs d'un critère ; des textes génériques pour une question inconnue.</summary>
    public static CriterionInfo Criterion(string questionId) => Criteria.GetValueOrDefault(questionId, DefaultCriterion);

    /// <summary>
    /// État d'une rubrique : aucune réponse = à auditer ; toutes Sans objet = non évaluée ; niveau élevé = point de vigilance ;
    /// risque faible avec toutes les questions renseignées = conforme ; sinon en cours.
    /// </summary>
    public static SectionStatus StatusOf(IReadOnlyList<BiosecurityQuestion> questions, IReadOnlyDictionary<string, Answer> answers, SectionResult section)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentNullException.ThrowIfNull(section);

        var inSection = questions.Where(question => question.Section == section.Section).ToList();
        var answered = inSection.Count(question => answers.ContainsKey(question.Id));
        if (answered == 0)
        {
            return SectionStatus.ToAudit;
        }

        return section.Level switch
        {
            RiskLevel.NotEvaluated => SectionStatus.NotEvaluated,
            RiskLevel.High => SectionStatus.Vigilance,
            RiskLevel.Low when answered == inSection.Count => SectionStatus.Compliant,
            _ => SectionStatus.InProgress,
        };
    }

    /// <summary>Points validés (Oui), alertes critiques (point critique à Non) et seuil de sécurité de l'indice global.</summary>
    public static AuditSummary Summarize(IReadOnlyList<BiosecurityQuestion> questions, IReadOnlyDictionary<string, Answer> answers, BiosecurityResult result)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentNullException.ThrowIfNull(result);

        var validated = questions.Count(question => answers.TryGetValue(question.Id, out var answer) && answer == Answer.Yes);
        var alerts = questions.Count(question => question.IsCritical && answers.TryGetValue(question.Id, out var answer) && answer == Answer.No);
        var entered = questions.Where(question => answers.ContainsKey(question.Id)).Select(question => question.Section).Distinct().Count();
        return new AuditSummary(validated, questions.Count, alerts, result.OverallScore >= SafetyThreshold, entered);
    }

    /// <summary>Numéro d'agrément fictif dérivé de l'identifiant d'élevage : « 76-BIO-2026-001 ».</summary>
    public static string ApprovalNumber(string farmId, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(farmId);
        var digits = new string([.. farmId.Where(char.IsDigit)]);
        var number = int.TryParse(digits, out var value) ? value : 0;
        return $"76-BIO-{today.Year}-{number:D3}";
    }

    /// <summary>Carte du périmètre : dernier passage de désinfection trois jours avant la date donnée.</summary>
    public static PerimeterShowcase Perimeter(DateOnly today) => new("Périmètre extérieur", "Bâtiment principal · Sas ouest", today.AddDays(-3));
}
