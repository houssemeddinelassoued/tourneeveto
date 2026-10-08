namespace TourneeVeto.Domain.Biosecurity;

/// <summary>Réponse à une question du questionnaire de biosécurité simplifié.</summary>
public enum Answer
{
    Yes,
    Partial,
    No,

    /// <summary>Sans objet : exclue du calcul du score de la rubrique.</summary>
    NotApplicable,
}

/// <summary>Question du questionnaire de biosécurité simplifié (contenu fictif, non officiel proAction).</summary>
/// <param name="Id">Identifiant de la question.</param>
/// <param name="Section">Rubrique de rattachement (ex. « Introduction d'animaux »).</param>
/// <param name="Text">Libellé de la question.</param>
/// <param name="Weight">Poids dans le score de la rubrique, de 1 à 3.</param>
/// <param name="IsCritical">Point critique, à signaler dans le rapport quelle que soit la note.</param>
public sealed record BiosecurityQuestion(string Id, string Section, string Text, int Weight, bool IsCritical)
{
    public const int MinWeight = 1;
    public const int MaxWeight = 3;

    /// <summary>Poids validé à la construction et lors d'une copie avec <c>with</c>.</summary>
    public int Weight { get; init => field = ValidateWeight(value); } = ValidateWeight(Weight);

    private static int ValidateWeight(int weight) =>
        weight is >= MinWeight and <= MaxWeight
            ? weight
            : throw new ArgumentOutOfRangeException(
                nameof(Weight), weight, $"Le poids doit être compris entre {MinWeight} et {MaxWeight}.");
}
