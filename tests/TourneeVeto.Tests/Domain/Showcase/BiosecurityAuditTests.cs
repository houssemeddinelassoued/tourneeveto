using TourneeVeto.Domain.Biosecurity;
using TourneeVeto.Domain.Showcase;

namespace TourneeVeto.Tests.Domain.Showcase;

/// <summary>États de rubrique, synthèse de l'audit et textes fictifs du bilan de biosécurité.</summary>
public class BiosecurityAuditTests
{
    private static BiosecurityQuestion Q(string id, string section, bool critical = false) => new(id, section, $"Q {id}", 1, critical);

    private static readonly BiosecurityQuestion[] Questions = [Q("A", "R1"), Q("B", "R1"), Q("C", "R2", critical: true), Q("D", "R2")];

    private static SectionStatus StatusOf(string section, Dictionary<string, Answer> answers)
    {
        var result = BiosecurityScore.Compute(Questions, answers);
        return BiosecurityAudit.StatusOf(Questions, answers, result.Sections.First(candidate => candidate.Section == section));
    }

    [Fact]
    public void Sans_reponse_la_rubrique_est_a_auditer()
    {
        Assert.Equal(SectionStatus.ToAudit, StatusOf("R1", []));
    }

    [Fact]
    public void Toutes_les_reponses_Sans_objet_donnent_une_rubrique_non_evaluee()
    {
        Assert.Equal(SectionStatus.NotEvaluated, StatusOf("R1", new() { ["A"] = Answer.NotApplicable, ["B"] = Answer.NotApplicable }));
    }

    [Fact]
    public void Risque_faible_et_rubrique_complete_est_conforme()
    {
        Assert.Equal(SectionStatus.Compliant, StatusOf("R1", new() { ["A"] = Answer.Yes, ["B"] = Answer.Yes }));
    }

    [Fact]
    public void Risque_faible_mais_rubrique_incomplete_est_en_cours()
    {
        Assert.Equal(SectionStatus.InProgress, StatusOf("R1", new() { ["A"] = Answer.Yes }));
    }

    [Fact]
    public void Risque_modere_est_en_cours()
    {
        Assert.Equal(SectionStatus.InProgress, StatusOf("R1", new() { ["A"] = Answer.Yes, ["B"] = Answer.No }));
    }

    [Fact]
    public void Point_critique_a_Non_donne_un_point_de_vigilance()
    {
        Assert.Equal(SectionStatus.Vigilance, StatusOf("R2", new() { ["C"] = Answer.No, ["D"] = Answer.Yes }));
    }

    [Fact]
    public void La_synthese_compte_les_points_valides_les_alertes_critiques_et_le_seuil_non_atteint()
    {
        var answers = new Dictionary<string, Answer> { ["A"] = Answer.Yes, ["B"] = Answer.Partial, ["C"] = Answer.No };
        var result = BiosecurityScore.Compute(Questions, answers);

        var summary = BiosecurityAudit.Summarize(Questions, answers, result);

        Assert.Equal(1, summary.ValidatedPoints);
        Assert.Equal(4, summary.TotalPoints);
        Assert.Equal(1, summary.CriticalAlerts);
        Assert.Equal(2, summary.SectionsEntered);
        Assert.False(summary.SafetyThresholdReached);   // R1 = 75, R2 = 0 : indice 38, sous le seuil de 50
    }

    [Fact]
    public void Indice_global_au_dessus_de_50_atteint_le_seuil_de_securite()
    {
        var answers = new Dictionary<string, Answer> { ["A"] = Answer.Yes, ["B"] = Answer.Yes };

        var summary = BiosecurityAudit.Summarize(Questions, answers, BiosecurityScore.Compute(Questions, answers));

        Assert.True(summary.SafetyThresholdReached);
    }

    [Fact]
    public void Sans_reponse_le_seuil_de_securite_n_est_pas_atteint()
    {
        var result = BiosecurityScore.Compute(Questions, new Dictionary<string, Answer>());

        var summary = BiosecurityAudit.Summarize(Questions, new Dictionary<string, Answer>(), result);

        Assert.False(summary.SafetyThresholdReached);
        Assert.Equal(0, summary.ValidatedPoints);
        Assert.Equal(0, summary.CriticalAlerts);
    }

    [Fact]
    public void Numero_d_agrement_fictif_derive_de_l_identifiant_de_la_ferme()
    {
        Assert.Equal("76-BIO-2026-007", BiosecurityAudit.ApprovalNumber("F007", new DateOnly(2026, 10, 8)));
        Assert.Equal("76-BIO-2026-000", BiosecurityAudit.ApprovalNumber("X", new DateOnly(2026, 10, 8)));
    }

    [Fact]
    public void Dernier_passage_de_desinfection_trois_jours_avant_aujourd_hui()
    {
        Assert.Equal(new DateOnly(2026, 10, 5), BiosecurityAudit.Perimeter(new DateOnly(2026, 10, 8)).LastDisinfection);
    }
}
