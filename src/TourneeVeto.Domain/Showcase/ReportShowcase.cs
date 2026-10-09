using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Reports;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Domain.Showcase;

// Enrichissement fictif du rapport de visite, d'après design/stitch/tablette/rapport.png et web/rapports.png.
// Les constats, scores et pratiques viennent du rapport réel (VisitReport) ; le reste (historique, photos,
// signatures, échéances) est fictif et déterministe.

/// <summary>Bilan de reproduction tiré des diagnostics de gestation saisis.</summary>
/// <param name="Planned">Vaches à contrôler (motif DG dans la grille).</param>
/// <param name="Seen">Diagnostics saisis.</param>
/// <param name="Pregnant">Gestantes (résultat positif).</param>
/// <param name="Empty">Vides (résultat négatif).</param>
/// <param name="RatePercent">Taux de gestation parmi les diagnostics saisis ; <c>null</c> si aucun.</param>
public sealed record ReproductionSummary(int Planned, int Seen, int Pregnant, int Empty, int? RatePercent)
{
    public bool HasData => Seen > 0;
}

/// <summary>Constat clinique regroupé (mammites, vêlages, tarissements) à partir des résultats saisis.</summary>
/// <param name="Kind">Clé : <c>mammary</c>, <c>calving</c> ou <c>dryoff</c>.</param>
/// <param name="Title">Titre du constat.</param>
/// <param name="Count">Nombre de cas constatés.</param>
/// <param name="CountLabel">Unité du nombre (« cas », « vêlage(s) »…).</param>
/// <param name="CowIds">Vaches concernées.</param>
/// <param name="Planned">Vaches prévues pour ce motif.</param>
/// <param name="Entered">Résultats saisis pour ce motif.</param>
public sealed record ReportFinding(string Kind, string Title, int Count, string CountLabel, IReadOnlyList<string> CowIds, int Planned, int Entered)
{
    public bool HasData => Entered > 0;
}

/// <summary>Pratique prioritaire avec son échéance fictive.</summary>
public sealed record PracticeSchedule(int Rank, BiosecurityQuestion Practice, string Deadline, bool Immediate);

/// <summary>Cliché fictif horodaté (illustration locale).</summary>
/// <param name="Kind">Illustration : <c>calving</c> ou <c>udder</c>.</param>
public sealed record ReportPhoto(string Kind, DateOnly Date, TimeOnly Time, string Title, string Caption);

/// <summary>Émargement fictif.</summary>
public sealed record ReportSignature(string Role, string Name, string Detail, TimeOnly SignedAt);

/// <summary>Visite passée fictive de l'historique (poste ≥ 1024 px).</summary>
public sealed record PastVisit(DateOnly Date, string FarmName, string Reason, string Reference, string Status, string Outcome);

/// <summary>Tout ce que le rapport affiche en plus du <see cref="VisitReport"/>.</summary>
/// <param name="Reference">« SYN-AAAA-MMJJ ».</param>
/// <param name="Fingerprint">Empreinte SHA-256 (64 caractères hexadécimaux) du contenu du rapport.</param>
/// <param name="ValidatedAt">Heure de validation hors-ligne.</param>
public sealed record ReportShowcase(
    string Reference,
    string Fingerprint,
    TimeOnly ValidatedAt,
    ReproductionSummary Reproduction,
    IReadOnlyList<ReportFinding> Findings,
    IReadOnlyList<PracticeSchedule> Practices,
    IReadOnlyList<ReportPhoto> Photos,
    IReadOnlyList<ReportSignature> Signatures,
    IReadOnlyList<PastVisit> PastVisits)
{
    /// <summary>Empreinte abrégée : « 8f3b…99a1 ».</summary>
    public string ShortFingerprint => Fingerprint.Length < 8 ? Fingerprint : $"{Fingerprint[..4]}…{Fingerprint[^4..]}";
}

/// <summary>Construit l'enrichissement fictif du rapport (fonction pure : aucune horloge, aucun hasard).</summary>
public static class ReportShowcaseBuilder
{
    private static readonly string[] PastReasons = ["Contrôle post-mammite et qualité du lait", "Suivi de troupeau et écornage", "Audit veaux et pathologie respiratoire"];
    private static readonly string[] PastStatuses = ["Archivé", "Télétransmis", "Signé"];
    private static readonly string[] PastOutcomes = ["Cellules élevées", "R.A.S", "Prescription active"];
    private static readonly int[] PastDaysAgo = [6, 11, 17];

    public static string Reference(DateOnly date) => string.Create(CultureInfo.InvariantCulture, $"SYN-{date.Year}-{date.Month:D2}{date.Day:D2}");

    public static ReportShowcase Build(VisitReport report, FarmShowcase profile, IReadOnlyList<Farm> farms, DateOnly today, TimeOnly now)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(farms);

        var vet = ShowcaseBuilder.DemoVeterinarian;
        return new ReportShowcase(
            Reference(report.Date),
            Fingerprint(report),
            now,
            Reproduction(report),
            Findings(report),
            Practices(report),
            Photos(report.Date, profile.ArrivalTime),
            [
                new ReportSignature("Pour l'éleveur", profile.Farmer, "Signature apposée sur tablette tactile", now.AddMinutes(-4)),
                new ReportSignature("Pour le vétérinaire", vet.Name, $"Clé cryptographique locale validée · {vet.OrderNumber}", now),
            ],
            PastVisits(farms, profile.FarmId, today));
    }

    /// <summary>Taux de gestation parmi les diagnostics saisis (pregnant / seen, arrondi à l'entier).</summary>
    public static ReproductionSummary Reproduction(VisitReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var outcomes = OutcomesOf(report, RegieAction.PregnancyCheck);
        var seen = outcomes.Where(entry => entry.Outcome is not null).ToList();
        var pregnant = seen.Count(entry => entry.Outcome == ResultOutcome.Positive);
        var empty = seen.Count(entry => entry.Outcome == ResultOutcome.Negative);
        int? rate = seen.Count == 0 ? null : (int)Math.Round(pregnant * 100.0 / seen.Count, MidpointRounding.AwayFromZero);
        return new ReproductionSummary(outcomes.Count, seen.Count, pregnant, empty, rate);
    }

    /// <summary>Constats groupés : mammites (CMT positif), vêlages constatés, tarissements faits.</summary>
    public static IReadOnlyList<ReportFinding> Findings(VisitReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return
        [
            Finding(report, "mammary", "Mammites subcliniques", RegieAction.HighScc, ResultOutcome.Positive, "cas"),
            Finding(report, "calving", "Vêlages constatés", RegieAction.CalvingSoon, ResultOutcome.Done, "vêlage(s)"),
            Finding(report, "dryoff", "Tarissements", RegieAction.DryOff, ResultOutcome.Done, "tarie(s)"),
        ];
    }

    /// <summary>Échéances fictives : un point critique est à appliquer sous 48 h, les autres sous 7 ou 15 jours.</summary>
    public static IReadOnlyList<PracticeSchedule> Practices(VisitReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report.Biosecurity?.PriorityPractices
            .Select((practice, index) => new PracticeSchedule(
                index + 1,
                practice,
                practice.IsCritical ? "48 heures" : index < 2 ? "7 jours" : "15 jours",
                practice.IsCritical))
            .ToList() ?? [];
    }

    /// <summary>Deux clichés fictifs horodatés à partir de l'heure d'arrivée.</summary>
    public static IReadOnlyList<ReportPhoto> Photos(DateOnly date, TimeOnly arrival) =>
    [
        new ReportPhoto("calving", date, arrival.AddMinutes(25), "Box de mise-bas n° 1", "Litière saine et sèche, bonne ventilation (illustration fictive)."),
        new ReportPhoto("udder", date, arrival.AddMinutes(50), "Contrôle du sphincter des trayons", "Hyperkératose légère, stade 2 : pommade hydratante conseillée (illustration fictive)."),
    ];

    /// <summary>Clé de mois « 2026-10 » pour le filtre de l'historique.</summary>
    public static string MonthKey(DateOnly date) => string.Create(CultureInfo.InvariantCulture, $"{date.Year}-{date.Month:D2}");

    /// <summary>Vrai si le terme (vide = tout) figure dans l'un des champs, sans tenir compte de la casse.</summary>
    public static bool Matches(string? term, params string[] fields) =>
        string.IsNullOrWhiteSpace(term) || fields.Any(field => field.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Trois visites passées fictives, d'autres élevages que l'élevage courant (cycliques).</summary>
    public static IReadOnlyList<PastVisit> PastVisits(IReadOnlyList<Farm> farms, string currentFarmId, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(farms);
        var names = farms.Where(farm => farm.Id != currentFarmId).OrderBy(farm => farm.Id, StringComparer.Ordinal).Select(farm => farm.Name).ToList();
        if (names.Count == 0)
        {
            names.Add("Élevage voisin (fictif)");
        }

        return Enumerable.Range(0, PastDaysAgo.Length).Select(index =>
        {
            var date = today.AddDays(-PastDaysAgo[index]);
            return new PastVisit(
                date,
                names[index % names.Count],
                PastReasons[index],
                string.Create(CultureInfo.InvariantCulture, $"REF-{date.Year}-{date.Month:D2}{date.Day:D2}"),
                PastStatuses[index],
                PastOutcomes[index]);
        }).ToList();
    }

    /// <summary>
    /// Empreinte SHA-256 du contenu du rapport (ferme, date, résultats, notes, bilan), calculée localement.
    /// <see cref="SHA256"/> est disponible dans Blazor WebAssembly ; sans lui, <see cref="Derived"/> fournit
    /// une empreinte déterministe de même forme (non cryptographique).
    /// </summary>
    public static string Fingerprint(VisitReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var text = Canonical(report);
        try
        {
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        }
        catch (PlatformNotSupportedException)
        {
            return Derived(text);
        }
    }

    /// <summary>Empreinte de repli : 64 caractères hexadécimaux (4 × FNV-1a 64 bits), identique d'un appel à l'autre.</summary>
    public static string Derived(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var builder = new StringBuilder(64);
        for (ulong seed = 0; seed < 4; seed++)
        {
            var hash = 14695981039346656037UL ^ (seed * 0x9E3779B97F4A7C15UL);
            foreach (var character in text)
            {
                hash = (hash ^ character) * 1099511628211UL;
            }

            builder.Append(hash.ToString("x16", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string Canonical(VisitReport report)
    {
        var builder = new StringBuilder();
        builder.Append(report.FarmName).Append('|').Append(report.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        foreach (var line in report.Lines)
        {
            builder.Append('\n').Append(line.Cow.Id).Append('|').Append(line.Note);
            foreach (var result in line.Results)
            {
                builder.Append('|').Append(result.Action).Append('=').Append(result.Outcome?.ToString() ?? "-");
            }
        }

        if (report.Biosecurity is { } bio)
        {
            builder.Append("\nBIO|").Append(bio.OverallScore?.ToString(CultureInfo.InvariantCulture) ?? "-");
            foreach (var section in bio.Sections)
            {
                builder.Append('|').Append(section.Section).Append('=').Append(section.Score?.ToString(CultureInfo.InvariantCulture) ?? "-");
            }

            foreach (var practice in bio.PriorityPractices)
            {
                builder.Append('|').Append(practice.Id);
            }
        }

        return builder.ToString();
    }

    private static List<(string CowId, ResultOutcome? Outcome)> OutcomesOf(VisitReport report, RegieAction action) =>
        report.Lines
            .SelectMany(line => line.Results.Where(result => result.Action == action).Select(result => (line.Cow.Id, result.Outcome)))
            .ToList();

    private static ReportFinding Finding(VisitReport report, string kind, string title, RegieAction action, ResultOutcome counted, string label)
    {
        var outcomes = OutcomesOf(report, action);
        var ids = outcomes.Where(entry => entry.Outcome == counted).Select(entry => entry.CowId).ToList();
        return new ReportFinding(kind, title, ids.Count, label, ids, outcomes.Count, outcomes.Count(entry => entry.Outcome is not null));
    }
}
