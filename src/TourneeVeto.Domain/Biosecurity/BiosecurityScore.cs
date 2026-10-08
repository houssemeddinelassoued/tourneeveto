namespace TourneeVeto.Domain.Biosecurity;

/// <summary>Niveau de risque d'une section du bilan de biosécurité.</summary>
public enum RiskLevel
{
    /// <summary>Score de 80 ou plus, sans question critique à Non.</summary>
    Low,

    /// <summary>Score de 50 à 79, sans question critique à Non.</summary>
    Moderate,

    /// <summary>Score inférieur à 50, ou au moins une question critique à Non.</summary>
    High,

    /// <summary>Aucune réponse évaluable (tout Sans objet ou sans réponse).</summary>
    NotEvaluated,
}

/// <summary>Résultat d'une section ; <see cref="Score"/> sur 100, <c>null</c> si la section n'est pas évaluée.</summary>
public sealed record SectionResult(string Section, int? Score, RiskLevel Level);

/// <summary>Bilan de biosécurité : sections dans l'ordre du questionnaire et 3 pratiques prioritaires au plus.</summary>
public sealed record BiosecurityResult(IReadOnlyList<SectionResult> Sections, IReadOnlyList<BiosecurityQuestion> PriorityPractices);

/// <summary>Calcul du score de biosécurité (fonction pure, sans dépendance).</summary>
public static class BiosecurityScore
{
    public const int MaxPriorityPractices = 3;

    /// <summary>
    /// Oui = 100 % du poids, Partiel = 50 %, Non = 0 ; Sans objet et absence de réponse sortent du calcul.
    /// Score = points obtenus / points possibles × 100, arrondi au plus proche (AwayFromZero).
    /// </summary>
    public static BiosecurityResult Compute(IReadOnlyList<BiosecurityQuestion> questions, IReadOnlyDictionary<string, Answer> answers)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(answers);

        // GroupBy conserve l'ordre de première apparition des sections.
        var sections = questions
            .GroupBy(question => question.Section)
            .Select(group => ScoreSection(group.Key, group, answers))
            .ToList();
        var sectionOrder = sections.Select((section, index) => (section.Section, index)).ToDictionary(item => item.Section, item => item.index);

        var priorityPractices = questions
            .Where(question => answers.TryGetValue(question.Id, out var answer) && answer is Answer.No or Answer.Partial)
            .OrderByDescending(question => question.Weight)
            .ThenByDescending(question => question.IsCritical)
            .ThenBy(question => sectionOrder[question.Section])
            .ThenBy(question => question.Id, StringComparer.Ordinal)
            .Take(MaxPriorityPractices)
            .ToList();

        return new BiosecurityResult(sections, priorityPractices);
    }

    private static SectionResult ScoreSection(string section, IEnumerable<BiosecurityQuestion> questions, IReadOnlyDictionary<string, Answer> answers)
    {
        decimal earned = 0;
        decimal possible = 0;
        var criticalNo = false;

        foreach (var question in questions)
        {
            if (!answers.TryGetValue(question.Id, out var answer) || answer == Answer.NotApplicable)
            {
                continue;
            }

            possible += question.Weight;
            earned += answer switch
            {
                Answer.Yes => question.Weight,
                Answer.Partial => question.Weight / 2m,
                _ => 0m,
            };
            criticalNo |= question.IsCritical && answer == Answer.No;
        }

        if (possible == 0)
        {
            return new SectionResult(section, Score: null, RiskLevel.NotEvaluated);
        }

        var score = (int)Math.Round(earned / possible * 100, MidpointRounding.AwayFromZero);
        var level = criticalNo ? RiskLevel.High
            : score >= 80 ? RiskLevel.Low
            : score >= 50 ? RiskLevel.Moderate
            : RiskLevel.High;
        return new SectionResult(section, score, level);
    }
}
