using System.Text.Json;
using TourneeVeto.Domain.Biosecurity;

namespace TourneeVeto.Ui.Biosecurity;

/// <summary>
/// Questionnaire de biosécurité simplifié et fictif (questionnaire.json, ressource incorporée) : disponible hors ligne,
/// sans requête réseau, identique dans les hôtes Web, WPF et MAUI.
/// </summary>
public static class BiosecurityQuestionnaire
{
    private static readonly Lazy<IReadOnlyList<BiosecurityQuestion>> Questions = new(Load);

    public static IReadOnlyList<BiosecurityQuestion> Default => Questions.Value;

    private static IReadOnlyList<BiosecurityQuestion> Load()
    {
        using var stream = typeof(BiosecurityQuestionnaire).Assembly.GetManifestResourceStream("TourneeVeto.Ui.Biosecurity.questionnaire.json")
            ?? throw new InvalidOperationException("Ressource questionnaire.json introuvable.");
        var document = JsonSerializer.Deserialize<QuestionnaireFile>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Questionnaire de biosécurité vide.");
        return document.Questions;
    }

    private sealed record QuestionnaireFile(IReadOnlyList<BiosecurityQuestion> Questions);
}
