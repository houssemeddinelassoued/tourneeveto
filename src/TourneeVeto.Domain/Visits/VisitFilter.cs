namespace TourneeVeto.Domain.Visits;

/// <summary>Filtre de la liste des visites de la tournée.</summary>
public enum VisitFilter
{
    All,
    InProgress,
    Waiting,
    Urgent,
}

public static class VisitFilters
{
    /// <summary>Vrai si une visite d'avancement <paramref name="progress"/> et de <paramref name="urgentCount"/> urgences correspond au filtre.</summary>
    public static bool Matches(VisitFilter filter, VisitProgress progress, int urgentCount) => filter switch
    {
        VisitFilter.InProgress => progress == VisitProgress.InProgress,
        VisitFilter.Waiting => progress == VisitProgress.ToDo,
        VisitFilter.Urgent => urgentCount > 0,
        _ => true,
    };
}
