using TourneeVeto.Domain.Biosecurity;

namespace TourneeVeto.Tests.Domain;

/// <summary>Indice global de biosécurité : moyenne arrondie des rubriques évaluées, niveau, et questions renseignées.</summary>
public class BiosecurityOverallTests
{
    private static BiosecurityQuestion Q(string id, string section, bool critical = false) => new(id, section, $"Q {id}", 1, critical);

    [Fact]
    public void Indice_global_est_la_moyenne_arrondie_des_rubriques_evaluees()
    {
        var questions = new[] { Q("A", "R1"), Q("B", "R2"), Q("C", "R3") };
        var answers = new Dictionary<string, Answer> { ["A"] = Answer.Yes, ["B"] = Answer.Partial };   // 100 et 50 ; R3 non évaluée

        var result = BiosecurityScore.Compute(questions, answers);

        Assert.Equal(75, result.OverallScore);
        Assert.Equal(RiskLevel.Moderate, result.OverallLevel);
        Assert.Equal(2, result.AnsweredCount);
        Assert.Equal(3, result.QuestionCount);
    }

    [Fact]
    public void Aucune_reponse_donne_un_indice_non_evalue()
    {
        var result = BiosecurityScore.Compute([Q("A", "R1")], new Dictionary<string, Answer>());

        Assert.Null(result.OverallScore);
        Assert.Equal(RiskLevel.NotEvaluated, result.OverallLevel);
        Assert.Equal(0, result.AnsweredCount);
    }

    [Fact]
    public void Une_rubrique_a_risque_eleve_rend_l_indice_global_a_risque_eleve()
    {
        var questions = new[] { Q("A", "R1"), Q("B", "R1"), Q("C", "R1"), Q("D", "R2", critical: true) };
        var answers = new Dictionary<string, Answer> { ["A"] = Answer.Yes, ["B"] = Answer.Yes, ["C"] = Answer.Yes, ["D"] = Answer.No };

        var result = BiosecurityScore.Compute(questions, answers);

        Assert.Equal(50, result.OverallScore);
        Assert.Equal(RiskLevel.High, result.OverallLevel);
    }

    [Fact]
    public void Un_indice_de_80_ou_plus_sans_rubrique_a_risque_est_faible()
    {
        var result = BiosecurityScore.Compute([Q("A", "R1")], new Dictionary<string, Answer> { ["A"] = Answer.Yes });

        Assert.Equal((100, RiskLevel.Low), (result.OverallScore, result.OverallLevel));
    }

    [Fact]
    public void Reponse_Sans_objet_compte_comme_renseignee_et_une_reponse_a_une_question_inconnue_est_ignoree()
    {
        var result = BiosecurityScore.Compute([Q("A", "R1")], new Dictionary<string, Answer> { ["A"] = Answer.NotApplicable, ["Z"] = Answer.Yes });

        Assert.Equal(1, result.AnsweredCount);
        Assert.Null(result.OverallScore);
    }
}
