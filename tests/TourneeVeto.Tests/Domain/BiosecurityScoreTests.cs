using TourneeVeto.Domain.Biosecurity;

namespace TourneeVeto.Tests.Domain;

/// <summary>
/// Score de biosécurité par section (epic 8, story 8.2) : Oui = 100 % du poids, Partiel = 50 %, Non = 0 ;
/// Sans objet et absence de réponse sortent du calcul ; arrondi au plus proche (AwayFromZero) ;
/// Low ≥ 80, Moderate 50 à 79, High &lt; 50 ; une question critique à Non rend la section High.
/// </summary>
public class BiosecurityScoreTests
{
    private const string Section = "Visiteurs et véhicules";

    private static BiosecurityQuestion Question(string id, string section = Section, int weight = 1, bool critical = false) =>
        new(id, section, $"Question {id}", weight, critical);

    /// <summary>Section de questions de poids 1 : <paramref name="yes"/> Oui, <paramref name="partial"/> Partiel, <paramref name="no"/> Non.</summary>
    private static (List<BiosecurityQuestion> Questions, Dictionary<string, Answer> Answers) Section(int yes, int partial, int no)
    {
        var answers = Enumerable.Repeat(Answer.Yes, yes)
            .Concat(Enumerable.Repeat(Answer.Partial, partial))
            .Concat(Enumerable.Repeat(Answer.No, no))
            .Select((answer, index) => (Id: $"Q{index:D3}", Answer: answer))
            .ToList();
        return ([.. answers.Select(item => Question(item.Id))], answers.ToDictionary(item => item.Id, item => item.Answer));
    }

    private static SectionResult SingleSection(IReadOnlyList<BiosecurityQuestion> questions, IReadOnlyDictionary<string, Answer> answers) =>
        Assert.Single(BiosecurityScore.Compute(questions, answers).Sections);

    // --- Seuils de niveau et arrondi ---

    [Theory]
    [InlineData(5, 0, 0, 100, RiskLevel.Low)]
    [InlineData(4, 0, 1, 80, RiskLevel.Low)]
    [InlineData(159, 0, 41, 80, RiskLevel.Low)]        // 79,5 arrondi à 80
    [InlineData(79, 0, 21, 79, RiskLevel.Moderate)]
    [InlineData(157, 0, 43, 79, RiskLevel.Moderate)]   // 78,5 arrondi à 79 (AwayFromZero, et non 78 au pair)
    [InlineData(1, 0, 1, 50, RiskLevel.Moderate)]
    [InlineData(0, 1, 0, 50, RiskLevel.Moderate)]      // Partiel seul = 50 %
    [InlineData(49, 0, 51, 49, RiskLevel.High)]
    [InlineData(0, 0, 3, 0, RiskLevel.High)]
    public void Niveau_de_risque_selon_le_score(int yes, int partial, int no, int expectedScore, RiskLevel expectedLevel)
    {
        var (questions, answers) = Section(yes, partial, no);

        var section = SingleSection(questions, answers);

        Assert.Equal(expectedScore, section.Score);
        Assert.Equal(expectedLevel, section.Level);
    }

    // --- Poids et réponses exclues ---

    [Fact]
    public void Score_tient_compte_du_poids_des_questions()
    {
        BiosecurityQuestion[] questions = [Question("Q1", weight: 3), Question("Q2", weight: 1)];
        var answers = new Dictionary<string, Answer> { ["Q1"] = Answer.Yes, ["Q2"] = Answer.No };

        var section = SingleSection(questions, answers);

        Assert.Equal(75, section.Score);
        Assert.Equal(RiskLevel.Moderate, section.Level);
    }

    [Fact]
    public void Partiel_vaut_la_moitie_du_poids()
    {
        BiosecurityQuestion[] questions = [Question("Q1", weight: 2), Question("Q2", weight: 2)];
        var answers = new Dictionary<string, Answer> { ["Q1"] = Answer.Partial, ["Q2"] = Answer.Yes };

        Assert.Equal(75, SingleSection(questions, answers).Score);
    }

    [Fact]
    public void Sans_objet_et_absence_de_reponse_sortent_du_calcul()
    {
        BiosecurityQuestion[] questions = [Question("Q1", weight: 1), Question("Q2", weight: 3), Question("Q3", weight: 2)];
        var answers = new Dictionary<string, Answer> { ["Q1"] = Answer.Yes, ["Q2"] = Answer.NotApplicable };

        var section = SingleSection(questions, answers);

        Assert.Equal(100, section.Score);
        Assert.Equal(RiskLevel.Low, section.Level);
    }

    // --- Sections non évaluées et questionnaire vide ---

    [Fact]
    public void Section_entierement_sans_objet_n_est_pas_evaluee()
    {
        BiosecurityQuestion[] questions = [Question("Q1", critical: true), Question("Q2", weight: 3)];
        var answers = new Dictionary<string, Answer> { ["Q1"] = Answer.NotApplicable, ["Q2"] = Answer.NotApplicable };

        var section = SingleSection(questions, answers);

        Assert.Null(section.Score);
        Assert.Equal(RiskLevel.NotEvaluated, section.Level);
    }

    [Fact]
    public void Section_sans_aucune_reponse_n_est_pas_evaluee()
    {
        var section = SingleSection([Question("Q1"), Question("Q2")], new Dictionary<string, Answer>());

        Assert.Null(section.Score);
        Assert.Equal(RiskLevel.NotEvaluated, section.Level);
    }

    [Fact]
    public void Questionnaire_vide_donne_un_resultat_vide()
    {
        var result = BiosecurityScore.Compute([], new Dictionary<string, Answer>());

        Assert.Empty(result.Sections);
        Assert.Empty(result.PriorityPractices);
    }

    // --- Questions critiques ---

    [Fact]
    public void Question_critique_a_Non_rend_la_section_High_quel_que_soit_le_score()
    {
        var (questions, answers) = Section(yes: 9, partial: 0, no: 0);
        questions.Add(Question("CRIT", critical: true));
        answers["CRIT"] = Answer.No;

        var section = SingleSection(questions, answers);

        Assert.Equal(90, section.Score);
        Assert.Equal(RiskLevel.High, section.Level);
    }

    [Fact]
    public void Question_critique_a_Partiel_n_a_pas_d_effet_critique()
    {
        var (questions, answers) = Section(yes: 9, partial: 0, no: 0);
        questions.Add(Question("CRIT", critical: true));
        answers["CRIT"] = Answer.Partial;

        var section = SingleSection(questions, answers);

        Assert.Equal(95, section.Score);
        Assert.Equal(RiskLevel.Low, section.Level);
    }

    // --- Plusieurs sections ---

    [Fact]
    public void Chaque_section_est_calculee_separement_dans_l_ordre_du_questionnaire()
    {
        BiosecurityQuestion[] questions =
        [
            Question("B1", section: "Vêlage et veaux"),
            Question("A1", section: "Introduction d'animaux"),
            Question("B2", section: "Vêlage et veaux"),
        ];
        var answers = new Dictionary<string, Answer> { ["B1"] = Answer.Yes, ["A1"] = Answer.No, ["B2"] = Answer.No };

        var sections = BiosecurityScore.Compute(questions, answers).Sections;

        Assert.Equal(["Vêlage et veaux", "Introduction d'animaux"], sections.Select(section => section.Section));
        Assert.Equal([50, 0], sections.Select(section => section.Score));
        Assert.Equal([RiskLevel.Moderate, RiskLevel.High], sections.Select(section => section.Level));
    }

    // --- Pratiques prioritaires ---

    [Fact]
    public void Pratiques_prioritaires_par_poids_puis_criticite_puis_section_trois_au_plus()
    {
        BiosecurityQuestion[] questions =
        [
            Question("A1", section: "A", weight: 2),
            Question("A2", section: "A", weight: 2, critical: true),
            Question("A3", section: "A", weight: 3),
            Question("A4", section: "A", weight: 1),
            Question("B1", section: "B", weight: 3),
            Question("B2", section: "B", weight: 2),
        ];
        var answers = new Dictionary<string, Answer>
        {
            ["A1"] = Answer.No,
            ["A2"] = Answer.Partial,
            ["A3"] = Answer.Yes,            // Oui : jamais prioritaire, même de poids 3
            ["A4"] = Answer.No,
            ["B1"] = Answer.Partial,
            ["B2"] = Answer.No,
        };

        var practices = BiosecurityScore.Compute(questions, answers).PriorityPractices;

        // B1 (poids 3) ; A2 (poids 2, critique) ; A1 (poids 2, section A avant B2 de la section B).
        Assert.Equal(["B1", "A2", "A1"], practices.Select(question => question.Id));
    }

    [Fact]
    public void Sans_objet_et_absence_de_reponse_ne_sont_pas_des_pratiques_prioritaires()
    {
        BiosecurityQuestion[] questions = [Question("Q1", weight: 3), Question("Q2", weight: 3), Question("Q3", weight: 1)];
        var answers = new Dictionary<string, Answer> { ["Q1"] = Answer.NotApplicable, ["Q3"] = Answer.No };

        var practices = BiosecurityScore.Compute(questions, answers).PriorityPractices;

        Assert.Equal(["Q3"], practices.Select(question => question.Id));
    }

    [Fact]
    public void Egalite_parfaite_departagee_par_l_identifiant()
    {
        BiosecurityQuestion[] questions = [Question("c"), Question("a"), Question("d"), Question("b")];
        var answers = questions.ToDictionary(question => question.Id, _ => Answer.No);

        var practices = BiosecurityScore.Compute(questions, answers).PriorityPractices;

        Assert.Equal(["a", "b", "c"], practices.Select(question => question.Id));
    }
}
