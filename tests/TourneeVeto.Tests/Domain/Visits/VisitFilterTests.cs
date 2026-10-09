using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Tests.Domain.Visits;

/// <summary>Filtres de la tournée (poste) : Toutes, En cours, En attente, Urgence.</summary>
public class VisitFilterTests
{
    [Theory]
    [InlineData(VisitFilter.All, VisitProgress.ToDo, 0, true)]
    [InlineData(VisitFilter.All, VisitProgress.InProgress, 2, true)]
    [InlineData(VisitFilter.InProgress, VisitProgress.InProgress, 0, true)]
    [InlineData(VisitFilter.InProgress, VisitProgress.ToDo, 3, false)]
    [InlineData(VisitFilter.Waiting, VisitProgress.ToDo, 0, true)]
    [InlineData(VisitFilter.Waiting, VisitProgress.InProgress, 0, false)]
    [InlineData(VisitFilter.Urgent, VisitProgress.ToDo, 1, true)]
    [InlineData(VisitFilter.Urgent, VisitProgress.InProgress, 0, false)]
    public void Une_visite_correspond_ou_non_au_filtre(VisitFilter filter, VisitProgress progress, int urgent, bool expected) =>
        Assert.Equal(expected, VisitFilters.Matches(filter, progress, urgent));
}
