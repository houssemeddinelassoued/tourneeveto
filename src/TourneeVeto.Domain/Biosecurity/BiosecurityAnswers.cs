namespace TourneeVeto.Domain.Biosecurity;

/// <summary>Réponses au bilan de biosécurité d'une visite, par identifiant de question (story 8.1, reprise).</summary>
public sealed record BiosecurityAnswers(Guid VisitId, IReadOnlyDictionary<string, Answer> Answers, DateTimeOffset UpdatedAt);
