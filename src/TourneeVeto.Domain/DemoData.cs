using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Domain;

/// <summary>Jeu de données de démonstration : élevages, vaches et visites du jour fictifs.</summary>
public sealed record DemoDataSet(IReadOnlyList<Farm> Farms, IReadOnlyList<Cow> Cows, IReadOnlyList<Visit> Visits);

/// <summary>
/// Génère un jeu de données entièrement fictif pour la démonstration du POC.
/// Toutes les dates sont relatives à <c>today</c>, pour que la grille de régie ne soit jamais vide,
/// quel que soit le jour de la démo. Le résultat est déterministe pour un même couple (<c>today</c>, <c>seed</c>).
/// </summary>
public static class DemoData
{
    public const int CowCount = 60;
    public const int CalvingsDueWithin14Days = 2;
    public const int PregnancyChecksDue = 3;
    public const int DryOffsDue = 2;
    public const int HighSccCows = 4;
    public const int Heifers = 5;
    public const int VisitsToday = 3;

    // Répartition des autres vaches, qui ne déclenchent aucune des actions ci-dessus.
    private const int MidPregnancyCows = 22;
    private const int FreshOpenCows = 14;
    private const int BredHeifers = 2;

    // Paramètres alignés sur les règles simplifiées du glossaire (PRODUCT.md).
    private const int GestationDays = 280;
    private const int HighSccThresholdThousands = 200;

    private static readonly (string Id, string Name, string Municipality, int CowCount, string VisitReason)[] FarmTemplates =
    [
        ("F001", "Ferme du Rang Fictif", "Saint-Exemple-des-Prés", 25, "Suivi de reproduction"),
        ("F002", "Ferme Laitière Démo", "Val-Démonstration", 20, "Suivi de reproduction et biosécurité"),
        ("F003", "Ferme de l'Érable Imaginaire", "Lac-Imaginaire", 15, "Contrôle de la qualité du lait"),
    ];

    private static readonly string[] CowNames =
    [
        "Abeille", "Bambou", "Bijou", "Biscotte", "Blanchette", "Bouton", "Brioche", "Câline", "Cannelle",
        "Capucine", "Caramel", "Cerise", "Chipie", "Clochette", "Coquine", "Cristal", "Dahlia", "Domino",
        "Douceur", "Duchesse", "Églantine", "Étoile", "Fanfare", "Farandole", "Flocon", "Fleurette",
        "Framboise", "Frimousse", "Gaufrette", "Grenadine", "Griotte", "Guimauve", "Iris", "Jasmine",
        "Jonquille", "Lavande", "Libellule", "Lilas", "Luciole", "Mandarine", "Marguerite", "Mélodie",
        "Mimosa", "Mirabelle", "Muscade", "Noisette", "Nougat", "Olive", "Paprika", "Pâquerette", "Perle",
        "Pervenche", "Pistache", "Plume", "Praline", "Prune", "Réglisse", "Rosette", "Sésame", "Tulipe",
        "Vanille", "Violette",
    ];

    /// <summary>
    /// Génère 3 élevages, 60 vaches et la tournée du jour (une visite par élevage), avec des dates relatives à <paramref name="today"/>.
    /// </summary>
    /// <param name="today">Date de référence de la démo (en général, la date du jour).</param>
    /// <param name="seed">Graine du générateur aléatoire.</param>
    public static DemoDataSet Generate(DateOnly today, int seed)
    {
        var random = new Random(seed);
        var generator = new DraftGenerator(today, random);
        var drafts = new List<CowDraft>(CowCount);

        AddMany(drafts, CalvingsDueWithin14Days, _ => generator.CalvingSoon());
        AddMany(drafts, PregnancyChecksDue, _ => generator.PregnancyCheckDue());
        AddMany(drafts, DryOffsDue, _ => generator.DryOffDue());
        AddMany(drafts, Heifers, i => generator.Heifer(bred: i < BredHeifers));
        AddMany(drafts, MidPregnancyCows, _ => generator.MidPregnancy());
        // Les CCS élevés sont portés par des vaches fraîches vêlées, sans autre action en cours.
        AddMany(drafts, FreshOpenCows, i => generator.FreshOpen(highScc: i < HighSccCows));
        AddMany(drafts, CowCount - drafts.Count, _ => generator.RecentlyBred());

        var shuffledDrafts = drafts.ToArray();
        random.Shuffle(shuffledDrafts);
        var names = CowNames.ToArray();
        random.Shuffle(names);

        var farms = new List<Farm>(FarmTemplates.Length);
        var cows = new List<Cow>(CowCount);
        var next = 0;
        for (var farmIndex = 0; farmIndex < FarmTemplates.Length; farmIndex++)
        {
            var template = FarmTemplates[farmIndex];
            farms.Add(new Farm(template.Id, template.Name, template.Municipality, template.CowCount));

            for (var number = 1; number <= template.CowCount; number++, next++)
            {
                var draft = shuffledDrafts[next];
                cows.Add(new Cow(
                    Id: $"{farmIndex + 1}{number:D3}",
                    FarmId: template.Id,
                    Name: names[next],
                    BornOn: draft.BornOn,
                    Lactation: draft.Lactation,
                    LastCalving: draft.LastCalving,
                    LastInsemination: draft.LastInsemination,
                    Status: draft.Status,
                    LastSccThousands: draft.LastSccThousands));
            }
        }

        // Générées après les vaches : ajouter les visites n'a pas changé le troupeau produit pour une graine donnée.
        var visits = FarmTemplates
            .Select(template => new Visit(NextGuid(random), template.Id, today, template.VisitReason, Notes: string.Empty, PhotoIds: []))
            .ToList();

        return new DemoDataSet(farms, cows, visits);
    }

    private static Guid NextGuid(Random random)
    {
        Span<byte> bytes = stackalloc byte[16];
        random.NextBytes(bytes);
        return new Guid(bytes);
    }

    private static void AddMany(List<CowDraft> drafts, int count, Func<int, CowDraft> create)
    {
        for (var i = 0; i < count; i++)
        {
            drafts.Add(create(i));
        }
    }

    private sealed record CowDraft(
        DateOnly BornOn,
        int Lactation,
        DateOnly? LastCalving,
        DateOnly? LastInsemination,
        ReproStatus Status,
        int? LastSccThousands);

    private sealed class DraftGenerator(DateOnly today, Random random)
    {
        /// <summary>Tarie, vêlage prévu dans 1 à 14 jours.</summary>
        public CowDraft CalvingSoon() => Pregnancy(daysToCalving: random.Next(1, 15), ReproStatus.Dry);

        /// <summary>Gestante, vêlage prévu dans 45 à 60 jours : à tarir.</summary>
        public CowDraft DryOffDue() => Pregnancy(daysToCalving: random.Next(45, 61), ReproStatus.Pregnant);

        /// <summary>Gestante, vêlage prévu dans plus de 60 jours : aucune action.</summary>
        public CowDraft MidPregnancy() => Pregnancy(daysToCalving: random.Next(75, 231), ReproStatus.Pregnant);

        /// <summary>Inséminée il y a 30 à 45 jours, sans diagnostic : diagnostic de gestation à faire.</summary>
        public CowDraft PregnancyCheckDue() => Bred(daysSinceInsemination: random.Next(30, 46));

        /// <summary>Inséminée il y a 3 à 25 jours : trop tôt pour le diagnostic de gestation.</summary>
        public CowDraft RecentlyBred() => Bred(daysSinceInsemination: random.Next(3, 26));

        /// <summary>Vêlée il y a 5 à 55 jours, pas encore inséminée depuis.</summary>
        public CowDraft FreshOpen(bool highScc)
        {
            var lastCalving = today.AddDays(-random.Next(5, 56));
            // Dernière insémination : celle qui a donné ce vêlage.
            return Lactating(lastCalving, lastCalving.AddDays(-GestationDays), ReproStatus.Open, highScc);
        }

        /// <summary>Génisse de 12 à 20 mois ; inséminée il y a 5 à 25 jours si <paramref name="bred"/>.</summary>
        public CowDraft Heifer(bool bred)
        {
            var bornOn = today.AddMonths(-random.Next(bred ? 15 : 12, 21)).AddDays(-random.Next(0, 28));
            DateOnly? insemination = bred ? today.AddDays(-random.Next(5, 26)) : null;
            var status = bred ? ReproStatus.Bred : ReproStatus.Open;
            return new CowDraft(bornOn, Lactation: 0, LastCalving: null, insemination, status, LastSccThousands: null);
        }

        private CowDraft Pregnancy(int daysToCalving, ReproStatus status)
        {
            var insemination = today.AddDays(daysToCalving - GestationDays);
            var lastCalving = insemination.AddDays(-random.Next(60, 121));
            return Lactating(lastCalving, insemination, status, highScc: false);
        }

        private CowDraft Bred(int daysSinceInsemination)
        {
            var insemination = today.AddDays(-daysSinceInsemination);
            var lastCalving = insemination.AddDays(-random.Next(50, 101));
            return Lactating(lastCalving, insemination, ReproStatus.Bred, highScc: false);
        }

        private CowDraft Lactating(DateOnly lastCalving, DateOnly lastInsemination, ReproStatus status, bool highScc)
        {
            var lactation = random.Next(1, 6);
            // Premier vêlage vers 24 mois, puis environ un vêlage tous les 13 mois.
            var bornOn = lastCalving.AddMonths(-(24 + (lactation - 1) * 13)).AddDays(-random.Next(0, 60));
            var scc = highScc
                ? random.Next(HighSccThresholdThousands + 10, 901)
                : random.Next(40, 181);
            return new CowDraft(bornOn, lactation, lastCalving, lastInsemination, status, scc);
        }
    }
}
