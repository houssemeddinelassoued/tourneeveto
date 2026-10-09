using TourneeVeto.Domain;
using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Reports;
using TourneeVeto.Domain.Showcase;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Tests.Domain.Showcase;

/// <summary>Enrichissement fictif du rapport : constats groupés, taux de reproduction, échéances, empreinte et historique.</summary>
public class ReportShowcaseTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);
    private static readonly DateTimeOffset At = new(2026, 10, 8, 7, 0, 0, TimeSpan.Zero);
    private static readonly DemoDataSet Data = DemoData.Generate(Today, TourneeVeto.Ui.Data.DemoDataSeeder.Seed);

    private static VisitReport Report(IEnumerable<CowVisitRecord>? records = null, BiosecurityResult? bio = null)
    {
        var farm = Data.Farms[0];
        var cows = Data.Cows.Where(cow => cow.FarmId == farm.Id);
        return VisitReports.Build(farm.Name, Today, DailyActions.Compute(cows, Today), records ?? [], bio);
    }

    private static IEnumerable<string> CowsWith(VisitReport report, RegieAction action) =>
        report.Lines.Where(line => line.Results.Any(result => result.Action == action)).Select(line => line.Cow.Id);

    private static CowVisitRecord Entered(VisitReport report, string cowId, RegieAction action, ResultOutcome outcome) =>
        CowVisitRecord.Empty(Data.Visits[0].Id, cowId).WithResult(action, outcome, At);

    [Fact]
    public void Reference_suit_le_format_SYN_annee_mois_jour()
    {
        Assert.Equal("SYN-2026-1008", ReportShowcaseBuilder.Reference(Today));
    }

    [Fact]
    public void Sans_resultat_saisi_le_taux_est_absent_et_les_constats_vides()
    {
        var report = Report();

        var repro = ReportShowcaseBuilder.Reproduction(report);

        Assert.False(repro.HasData);
        Assert.Null(repro.RatePercent);
        Assert.Equal(CowsWith(report, RegieAction.PregnancyCheck).Count(), repro.Planned);
        Assert.All(ReportShowcaseBuilder.Findings(report), finding => Assert.False(finding.HasData));
    }

    [Fact]
    public void Taux_de_gestation_compte_les_positifs_parmi_les_diagnostics_saisis()
    {
        var report = Report();
        var ids = CowsWith(report, RegieAction.PregnancyCheck).Take(3).ToList();
        Assert.Equal(3, ids.Count);
        var records = new[]
        {
            Entered(report, ids[0], RegieAction.PregnancyCheck, ResultOutcome.Positive),
            Entered(report, ids[1], RegieAction.PregnancyCheck, ResultOutcome.Positive),
            Entered(report, ids[2], RegieAction.PregnancyCheck, ResultOutcome.Negative),
        };

        var repro = ReportShowcaseBuilder.Reproduction(Report(records));

        Assert.Equal((3, 2, 1, 67), (repro.Seen, repro.Pregnant, repro.Empty, repro.RatePercent));
    }

    [Fact]
    public void Mammites_comptent_les_CMT_positifs()
    {
        var report = Report();
        var ids = CowsWith(report, RegieAction.HighScc).Take(2).ToList();
        Assert.Equal(2, ids.Count);
        var records = new[]
        {
            Entered(report, ids[0], RegieAction.HighScc, ResultOutcome.Positive),
            Entered(report, ids[1], RegieAction.HighScc, ResultOutcome.Negative),
        };

        var finding = ReportShowcaseBuilder.Findings(Report(records)).Single(candidate => candidate.Kind == "mammary");

        Assert.True(finding.HasData);
        Assert.Equal(1, finding.Count);
        Assert.Equal([ids[0]], finding.CowIds);
    }

    [Fact]
    public void Echeances_un_point_critique_est_immediat_les_autres_sous_7_ou_15_jours()
    {
        var questions = new[]
        {
            new BiosecurityQuestion("A", "R1", "Q A", 1, true),
            new BiosecurityQuestion("B", "R1", "Q B", 1, false),
            new BiosecurityQuestion("C", "R2", "Q C", 1, false),
            new BiosecurityQuestion("D", "R2", "Q D", 1, false),
        };
        var bio = BiosecurityScore.Compute(questions, questions.ToDictionary(question => question.Id, _ => Answer.No));

        var schedule = ReportShowcaseBuilder.Practices(Report(bio: bio));

        Assert.Equal([1, 2, 3], schedule.Select(item => item.Rank));
        Assert.Equal(["48 heures", "7 jours", "15 jours"], schedule.Select(item => item.Deadline));
        Assert.True(schedule[0].Immediate);
        Assert.False(schedule[1].Immediate);
    }

    [Fact]
    public void Sans_bilan_aucune_pratique_prioritaire()
    {
        Assert.Empty(ReportShowcaseBuilder.Practices(Report()));
    }

    [Fact]
    public void Empreinte_est_un_sha256_stable_qui_change_avec_le_contenu()
    {
        var report = Report();
        var cowId = CowsWith(report, RegieAction.PregnancyCheck).First();

        var first = ReportShowcaseBuilder.Fingerprint(report);
        var changed = ReportShowcaseBuilder.Fingerprint(Report([Entered(report, cowId, RegieAction.PregnancyCheck, ResultOutcome.Positive)]));

        Assert.Equal(64, first.Length);
        Assert.Matches("^[0-9a-f]{64}$", first);
        Assert.Equal(first, ReportShowcaseBuilder.Fingerprint(Report()));
        Assert.NotEqual(first, changed);
    }

    [Fact]
    public void Empreinte_de_repli_est_deterministe_et_de_meme_forme()
    {
        var derived = ReportShowcaseBuilder.Derived("contenu");

        Assert.Matches("^[0-9a-f]{64}$", derived);
        Assert.Equal(derived, ReportShowcaseBuilder.Derived("contenu"));
        Assert.NotEqual(derived, ReportShowcaseBuilder.Derived("autre"));
    }

    [Fact]
    public void Historique_donne_trois_visites_passees_d_autres_elevages()
    {
        var past = ReportShowcaseBuilder.PastVisits(Data.Farms, Data.Farms[0].Id, Today);

        Assert.Equal(3, past.Count);
        Assert.All(past, visit => Assert.True(visit.Date < Today));
        Assert.DoesNotContain(Data.Farms[0].Name, past.Select(visit => visit.FarmName));
        Assert.Equal(["Archivé", "Télétransmis", "Signé"], past.Select(visit => visit.Status));
    }

    [Fact]
    public void Build_assemble_photos_signatures_et_empreinte_abregee()
    {
        var report = Report();
        var profile = ShowcaseBuilder.ProfileOf(Data.Farms, Data.Visits, Data.Farms[0].Id, Today)!;

        var showcase = ReportShowcaseBuilder.Build(report, profile, Data.Farms, Today, new TimeOnly(10, 45));

        Assert.Equal(2, showcase.Photos.Count);
        Assert.Equal(profile.ArrivalTime.AddMinutes(25), showcase.Photos[0].Time);
        Assert.Equal(2, showcase.Signatures.Count);
        Assert.Equal(profile.Farmer, showcase.Signatures[0].Name);
        Assert.Equal(new TimeOnly(10, 45), showcase.ValidatedAt);
        Assert.Equal($"{showcase.Fingerprint[..4]}…{showcase.Fingerprint[^4..]}", showcase.ShortFingerprint);
    }

    [Fact]
    public void Filtre_de_l_historique_ignore_la_casse_et_accepte_un_terme_vide()
    {
        Assert.True(ReportShowcaseBuilder.Matches("", "Ferme"));
        Assert.True(ReportShowcaseBuilder.Matches(null, "Ferme"));
        Assert.True(ReportShowcaseBuilder.Matches(" ferme ", "Autre", "La Ferme"));
        Assert.False(ReportShowcaseBuilder.Matches("xyz", "Ferme"));
        Assert.Equal("2026-09", ReportShowcaseBuilder.MonthKey(new DateOnly(2026, 9, 30)));
    }
}
