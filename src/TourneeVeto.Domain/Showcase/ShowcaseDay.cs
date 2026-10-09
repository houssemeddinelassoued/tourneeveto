namespace TourneeVeto.Domain.Showcase;

// Modèle fictif d'enrichissement des écrans, d'après les maquettes Stitch (design/stitch).
// Tout est déterministe, dérivé de DemoData et de la date du jour (ShowcaseBuilder) ; rien n'est stocké.
// Les écrans suivants (Tournée, Régie, Biosécurité, Rapport) l'étendent en ajoutant des champs ici.

/// <summary>Profil fictif du vétérinaire connecté.</summary>
/// <param name="Name">Nom d'usage (ex. « Dre Camille Exemple »).</param>
/// <param name="Initials">Initiales de l'avatar.</param>
/// <param name="Title">Titre affiché sous le nom.</param>
/// <param name="Sector">Secteur de la tournée (fictif).</param>
/// <param name="OrderNumber">Numéro d'ordre professionnel, manifestement fictif.</param>
/// <param name="ClinicName">Nom du cabinet.</param>
public sealed record VeterinarianProfile(string Name, string Initials, string Title, string Sector, string OrderNumber, string ClinicName);

/// <summary>Un élevage de la tournée du jour, enrichi de données fictives de terrain.</summary>
/// <param name="FarmId">Identifiant de l'élevage (<see cref="Visits.Farm.Id"/>).</param>
/// <param name="Order">Rang dans la tournée, à partir de 1.</param>
/// <param name="FarmName">Nom de l'élevage (réel dans DemoData).</param>
/// <param name="Municipality">Municipalité (réelle dans DemoData).</param>
/// <param name="Farmer">Éleveur (fictif).</param>
/// <param name="Phone">Téléphone fictif, de la forme 555-01xx.</param>
/// <param name="Address">Adresse ou lieu-dit fictif.</param>
/// <param name="ArrivalTime">Heure de passage prévue.</param>
/// <param name="DistanceKm">Distance depuis l'étape précédente (ou depuis la clinique pour la première).</param>
/// <param name="TravelMinutes">Durée estimée du trajet depuis l'étape précédente.</param>
/// <param name="VisitMinutes">Durée estimée de la visite.</param>
/// <param name="Breed">Race dominante du troupeau.</param>
/// <param name="HerdSize">Effectif (réel dans DemoData).</param>
/// <param name="Tags">Étiquettes de motif (dérivées du motif de la visite).</param>
/// <param name="Instruction">Consigne du praticien.</param>
/// <param name="YardAccess">Accès à la cour de ferme.</param>
/// <param name="Reason">Motif de la visite (réel dans DemoData).</param>
/// <param name="Identifier">Identifiant d'élevage fictif (de la forme « QC-F001-5000 »).</param>
/// <param name="Description">Description du troupeau et de l'installation de traite (fictive).</param>
/// <param name="MotiveDetail">Précision du motif, affichée sur la carte compacte.</param>
/// <param name="Footer">Pied de carte (état de la télémesure ou du dossier).</param>
/// <param name="Protocol">Intitulé du protocole actif.</param>
/// <param name="History">Historique récent de l'élevage (fictif).</param>
/// <param name="Telemetry">Télémétrie fictive du robot de traite.</param>
public sealed record FarmShowcase(
    string FarmId,
    int Order,
    string FarmName,
    string Municipality,
    string Farmer,
    string Phone,
    string Address,
    TimeOnly ArrivalTime,
    double DistanceKm,
    int TravelMinutes,
    int VisitMinutes,
    string Breed,
    int HerdSize,
    IReadOnlyList<string> Tags,
    string Instruction,
    string YardAccess,
    string Reason,
    string Identifier,
    string Description,
    string MotiveDetail,
    string Footer,
    string Protocol,
    IReadOnlyList<HistoryEntry> History,
    TelemetryShowcase Telemetry);

/// <summary>Entrée de l'historique récent d'un élevage.</summary>
/// <param name="Title">Intitulé (ex. « Bilan reproduction »).</param>
/// <param name="Badge">Résultat ou état (ex. « 78 % gestation »).</param>
/// <param name="Text">Détail de l'entrée.</param>
/// <param name="Date">Date de l'entrée.</param>
public sealed record HistoryEntry(string Title, string Badge, string Text, DateOnly Date);

/// <summary>Télémétrie fictive du robot de traite d'un élevage.</summary>
/// <param name="ProductionLitersPerDay">Production moyenne par vache, en litres par jour.</param>
/// <param name="SccThousands">CCS moyen du troupeau, en milliers de cellules/mL.</param>
/// <param name="ActivityNote">Lecture de l'activité et de la rumination sur 7 jours.</param>
/// <param name="ActivityDeltaPercent">Écart d'activité par rapport à la moyenne, en pourcentage.</param>
public sealed record TelemetryShowcase(double ProductionLitersPerDay, int SccThousands, string ActivityNote, int ActivityDeltaPercent);

/// <summary>Météo fictive de la journée.</summary>
public sealed record WeatherShowcase(int TemperatureC, string Conditions);

/// <summary>Résumé fictif de la tournée.</summary>
/// <param name="TotalKm">Somme des trajets des étapes et du retour à la clinique.</param>
/// <param name="ReturnDistanceKm">Dernier trajet, de la dernière étape à la clinique.</param>
/// <param name="DepartFrom">Lieu de départ.</param>
/// <param name="Departure">Heure de départ de la clinique.</param>
/// <param name="Return">Heure de retour estimée.</param>
/// <param name="Weather">Météo du jour.</param>
public sealed record TourShowcase(double TotalKm, double ReturnDistanceKm, string DepartFrom, TimeOnly Departure, TimeOnly Return, WeatherShowcase Weather);

/// <summary>Ligne du matériel embarqué dans le véhicule.</summary>
/// <param name="Name">Matériel ou produit.</param>
/// <param name="Status">État ou quantité (ex. « 100 % prêt », « 8 flacons »).</param>
/// <param name="Ready">Vrai si le matériel est prêt (ton positif), faux s'il demande une action.</param>
public sealed record EquipmentItem(string Name, string Status, bool Ready);

/// <summary>Pharmacie embarquée.</summary>
public sealed record PharmacyStock(string VehicleName, string Summary, string Status, bool Ok);

/// <summary>Sujet sous surveillance : une vraie vache urgente de la grille de régie.</summary>
/// <param name="CowId">Numéro de la vache.</param>
/// <param name="CowName">Nom de la vache.</param>
/// <param name="FarmId">Élevage de la vache.</param>
/// <param name="FarmName">Nom de l'élevage.</param>
/// <param name="Tag">Motif court (ex. « Mammite subclinique »).</param>
/// <param name="Detail">Détail chiffré tiré des données réelles de la vache.</param>
/// <param name="PlannedAction">Action prévue (fictive).</param>
public sealed record WatchedSubject(string CowId, string CowName, string FarmId, string FarmName, string Tag, string Detail, string PlannedAction);

/// <summary>Urgence signalée fictive (appel d'un éleveur).</summary>
public sealed record UrgentAlert(string FarmId, string FarmName, string Title, string Description, TimeOnly Time, string Phone);

/// <summary>Chiffres de la journée, dérivés de la grille de régie quand elle les donne.</summary>
/// <param name="VisitsPlanned">Visites prévues.</param>
/// <param name="PregnancyChecks">Vaches avec un diagnostic de gestation à faire.</param>
/// <param name="CalvingsImminent">Vaches au vêlage prévu dans la fenêtre de surveillance.</param>
/// <param name="CalvingWindowDays">Fenêtre de surveillance des vêlages, en jours.</param>
/// <param name="SccAlerts">Vaches au CCS élevé.</param>
/// <param name="SccFarmName">Élevage du premier CCS élevé ; <c>null</c> s'il n'y en a pas.</param>
/// <param name="RegieRows">Lignes de la grille de régie, tous élevages de la tournée confondus.</param>
/// <param name="BiosecurityAudits">Visites qui comportent un audit de biosécurité.</param>
public sealed record ShowcaseIndicators(
    int VisitsPlanned,
    int PregnancyChecks,
    int CalvingsImminent,
    int CalvingWindowDays,
    int SccAlerts,
    string? SccFarmName,
    int RegieRows,
    int BiosecurityAudits)
{
    /// <summary>Avancement du circuit routier : la journée n'a pas commencé (fictif).</summary>
    public int TourProgressPercent => 0;

    /// <summary>Part des lignes de régie qui sont un diagnostic de gestation à faire.</summary>
    public int ReproSharePercent => Percent(PregnancyChecks, RegieRows);

    /// <summary>Part des lignes de régie au CCS élevé.</summary>
    public int VigilanceSharePercent => Percent(SccAlerts, RegieRows);

    /// <summary>Dossiers réglementaires prêts à signer (fictif : deux sur trois dès qu'il y a une visite).</summary>
    public int DossiersReadyPercent => VisitsPlanned == 0 ? 0 : 67;

    private static int Percent(int part, int whole) => whole == 0 ? 0 : (int)Math.Round(100.0 * part / whole);
}

/// <summary>Journée de démonstration complète : profil, étapes, météo, matériel, surveillance et urgence.</summary>
/// <param name="Veterinarian">Profil du vétérinaire.</param>
/// <param name="Farms">Étapes de la tournée, dans l'ordre.</param>
/// <param name="Tour">Résumé de la tournée.</param>
/// <param name="Equipment">Matériel du véhicule.</param>
/// <param name="Pharmacy">Pharmacie embarquée.</param>
/// <param name="Subjects">Toutes les vaches urgentes de la tournée (l'écran n'en montre que les premières).</param>
/// <param name="Alert">Urgence signalée.</param>
/// <param name="Indicators">Indicateurs de la journée.</param>
/// <param name="LastSync">Heure de la dernière synchronisation fictive.</param>
public sealed record ShowcaseDay(
    VeterinarianProfile Veterinarian,
    IReadOnlyList<FarmShowcase> Farms,
    TourShowcase Tour,
    IReadOnlyList<EquipmentItem> Equipment,
    PharmacyStock Pharmacy,
    IReadOnlyList<WatchedSubject> Subjects,
    UrgentAlert Alert,
    ShowcaseIndicators Indicators,
    TimeOnly LastSync);
