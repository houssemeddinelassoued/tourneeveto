using TourneeVeto.Domain.Regie;

namespace TourneeVeto.Domain.Visits;

/// <summary>Résultat saisi pour un motif de régie (story 7.1).</summary>
public enum ResultOutcome
{
    /// <summary>Fait (vache tarie, vêlage constaté, observation notée).</summary>
    Done,

    /// <summary>Reporté à une prochaine visite.</summary>
    Postponed,

    /// <summary>Positif (DG gestante, CMT positif).</summary>
    Positive,

    /// <summary>Négatif (DG vide, CMT négatif).</summary>
    Negative,

    /// <summary>Douteux : à revoir.</summary>
    Doubtful,

    /// <summary>Examen normal.</summary>
    Normal,

    /// <summary>Examen anormal.</summary>
    Abnormal,
}

/// <summary>Résultat d'un motif pour une vache.</summary>
public sealed record CowResult(RegieAction Action, ResultOutcome Outcome);

/// <summary>Résultats proposés pour chaque motif : au plus 3 choix, pour saisir en 2 touches au plus (story 7.1).</summary>
public static class ResultOptions
{
    public static IReadOnlyList<ResultOutcome> For(RegieAction action) => action switch
    {
        RegieAction.PregnancyCheck => [ResultOutcome.Positive, ResultOutcome.Negative, ResultOutcome.Doubtful],
        RegieAction.DryOff or RegieAction.CalvingSoon => [ResultOutcome.Done, ResultOutcome.Postponed],
        RegieAction.HighScc => [ResultOutcome.Positive, ResultOutcome.Negative],
        RegieAction.PostCalvingCheck => [ResultOutcome.Normal, ResultOutcome.Abnormal],
        RegieAction.NotInseminated => [ResultOutcome.Done],
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Motif de régie inconnu."),
    };
}

/// <summary>Saisie d'une vache pendant une visite : résultats par motif et note libre (stories 7.1 et 7.2).</summary>
/// <param name="VisitId">Visite concernée.</param>
/// <param name="CowId">Numéro de la vache dans l'élevage visité.</param>
/// <param name="Results">Un résultat au plus par motif ; une correction remplace le précédent.</param>
/// <param name="Note">Note libre, <see cref="MaxNoteLength"/> caractères au plus.</param>
/// <param name="UpdatedAt">Dernière modification (fournie par l'appelant, jamais lue depuis l'horloge).</param>
public sealed record CowVisitRecord(Guid VisitId, string CowId, IReadOnlyList<CowResult> Results, string Note, DateTimeOffset UpdatedAt)
{
    public const int MaxNoteLength = 2000;

    public static CowVisitRecord Empty(Guid visitId, string cowId) => new(visitId, cowId, [], string.Empty, DateTimeOffset.MinValue);

    /// <summary>Vrai si quelque chose a été saisi (résultat ou note).</summary>
    public bool HasEntries => Results.Count > 0 || Note.Length > 0;

    public ResultOutcome? ResultFor(RegieAction action) => Results.FirstOrDefault(result => result.Action == action)?.Outcome;

    public CowVisitRecord WithResult(RegieAction action, ResultOutcome outcome, DateTimeOffset at)
    {
        if (!ResultOptions.For(action).Contains(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome), outcome, $"Résultat non proposé pour le motif {action}.");
        }

        return this with { Results = [.. Results.Where(result => result.Action != action), new CowResult(action, outcome)], UpdatedAt = at };
    }

    public CowVisitRecord WithNote(string note, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(note);
        return note.Length > MaxNoteLength
            ? throw new ArgumentOutOfRangeException(nameof(note), note.Length, $"La note dépasse {MaxNoteLength} caractères.")
            : this with { Note = note, UpdatedAt = at };
    }
}

/// <summary>Avancement d'une visite (story 4.2).</summary>
public enum VisitProgress
{
    ToDo,
    InProgress,
}

public static class VisitProgresses
{
    /// <summary>« En cours » dès qu'une saisie existe pour la visite.</summary>
    public static VisitProgress Of(IEnumerable<CowVisitRecord> records) =>
        records.Any(record => record.HasEntries) ? VisitProgress.InProgress : VisitProgress.ToDo;
}
