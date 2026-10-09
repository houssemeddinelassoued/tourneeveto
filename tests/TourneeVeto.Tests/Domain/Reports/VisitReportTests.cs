using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Reports;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Tests.Domain.Regie;

namespace TourneeVeto.Tests.Domain.Reports;

/// <summary>Contenu du rapport de visite (epic 10, story 10.1) : grille, saisies, bilan de biosécurité et recommandations.</summary>
public class VisitReportTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 8, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid VisitId = Guid.NewGuid();

    private static RegieGrid TwoCowGrid() => RegieTestCows.Grid(
        RegieTestCows.Neutral("9001") with { Status = ReproStatus.Bred, LastInsemination = RegieTestCows.DaysAgo(40) },
        RegieTestCows.Neutral("9002") with { LastSccThousands = 900 });

    private static BiosecurityResult Bio(params (string Id, string Section, int Weight, bool Critical, Answer Answer)[] items)
    {
        var questions = items.Select(item => new BiosecurityQuestion(item.Id, item.Section, $"Pratique {item.Id}", item.Weight, item.Critical)).ToList();
        return BiosecurityScore.Compute(questions, items.ToDictionary(item => item.Id, item => item.Answer));
    }

    [Fact]
    public void Rapport_complet_contient_ferme_date_vaches_resultats_rubriques_et_3_recommandations()
    {
        var grid = TwoCowGrid();
        var records = new[]
        {
            CowVisitRecord.Empty(VisitId, "9001").WithResult(RegieAction.PregnancyCheck, ResultOutcome.Positive, At).WithNote("Gestante", At),
        };
        var bio = Bio(("A", "Rubrique 1", 3, false, Answer.No), ("B", "Rubrique 1", 2, false, Answer.Partial), ("C", "Rubrique 2", 1, false, Answer.No), ("D", "Rubrique 2", 1, false, Answer.Yes));

        var report = VisitReports.Build("GAEC des Trois Chênes", new DateOnly(2026, 10, 8), grid, records, bio);

        Assert.Equal("GAEC des Trois Chênes", report.FarmName);
        Assert.Equal(new DateOnly(2026, 10, 8), report.Date);
        Assert.Equal(grid.Items.Select(item => item.Cow.Id), report.Lines.Select(line => line.Cow.Id));
        var seen = report.Lines.Single(line => line.Cow.Id == "9001");
        Assert.Equal(new ReportResult(RegieAction.PregnancyCheck, ResultOutcome.Positive), Assert.Single(seen.Results));
        Assert.Equal("Gestante", seen.Note);
        Assert.NotNull(report.Biosecurity);
        Assert.Equal(2, report.Biosecurity.Sections.Count);
        Assert.Equal(3, report.Biosecurity.PriorityPractices.Count);
        Assert.True(report.HasBiosecurity);
        Assert.Equal("Données fictives — règles simplifiées", VisitReport.Disclaimer);
    }

    [Fact]
    public void Vache_de_la_grille_sans_resultat_est_non_vue()
    {
        var report = VisitReports.Build("Ferme", new DateOnly(2026, 10, 8), TwoCowGrid(), [], biosecurity: null);

        Assert.All(report.Lines, line =>
        {
            Assert.False(line.IsSeen);
            Assert.All(line.Results, result => Assert.Null(result.Outcome));
        });
        Assert.Equal((0, 2), (report.SeenCount, report.NotSeenCount));
    }

    [Fact]
    public void Vache_avec_une_note_sans_resultat_reste_non_vue_mais_garde_sa_note()
    {
        var records = new[] { CowVisitRecord.Empty(VisitId, "9002").WithNote("Boiterie", At) };

        var report = VisitReports.Build("Ferme", new DateOnly(2026, 10, 8), TwoCowGrid(), records, null);

        var line = report.Lines.Single(l => l.Cow.Id == "9002");
        Assert.False(line.IsSeen);
        Assert.Equal("Boiterie", line.Note);
    }

    [Fact]
    public void Saisie_d_une_vache_absente_de_la_grille_est_ignoree()
    {
        var records = new[] { CowVisitRecord.Empty(VisitId, "0000").WithNote("Inconnue", At) };

        var report = VisitReports.Build("Ferme", new DateOnly(2026, 10, 8), TwoCowGrid(), records, null);

        Assert.Equal(2, report.Lines.Count);
    }

    [Fact]
    public void Sans_bilan_de_biosecurite_le_rapport_le_signale_et_le_reste_est_complet()
    {
        var report = VisitReports.Build("Ferme", new DateOnly(2026, 10, 8), TwoCowGrid(), [], biosecurity: null);

        Assert.False(report.HasBiosecurity);
        Assert.Null(report.Biosecurity);
        Assert.Equal(2, report.Lines.Count);
    }

    [Fact]
    public void Bilan_sans_aucune_reponse_est_considere_non_realise()
    {
        var empty = BiosecurityScore.Compute([new BiosecurityQuestion("A", "R", "Q", 1, false)], new Dictionary<string, Answer>());

        var report = VisitReports.Build("Ferme", new DateOnly(2026, 10, 8), TwoCowGrid(), [], empty);

        Assert.False(report.HasBiosecurity);
    }

    [Fact]
    public void Grille_vide_donne_un_rapport_sans_ligne()
    {
        var report = VisitReports.Build("Ferme", new DateOnly(2026, 10, 8), RegieTestCows.Grid(), [], null);

        Assert.Empty(report.Lines);
    }
}
