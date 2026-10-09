using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui.Formatting;

namespace TourneeVeto.Tests.Ui;

/// <summary>Libellés de la page Tournée : états des étapes, statuts du poste et lignes du protocole.</summary>
public class TourLabelsTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    [Theory]
    [InlineData(VisitProgress.InProgress, false, "En cours", "state--progress")]
    [InlineData(VisitProgress.InProgress, true, "En cours", "state--progress")]
    [InlineData(VisitProgress.ToDo, true, "Suivante", "state--next")]
    [InlineData(VisitProgress.ToDo, false, "En attente", "state--waiting")]
    public void Etat_d_une_etape(VisitProgress progress, bool isNext, string label, string cssClass)
    {
        Assert.Equal(label, TourLabels.State(progress, isNext));
        Assert.Equal(cssClass, TourLabels.StateClass(progress, isNext));
    }

    [Theory]
    [InlineData(VisitProgress.InProgress, 2, "Sur place", "state--progress")]
    [InlineData(VisitProgress.ToDo, 1, "Priorité haute", "state--urgent")]
    [InlineData(VisitProgress.ToDo, 0, "En attente", "state--waiting")]
    public void Statut_d_une_etape_sur_poste(VisitProgress progress, int urgent, string label, string cssClass)
    {
        Assert.Equal(label, TourLabels.DeskStatus(progress, urgent));
        Assert.Equal(cssClass, TourLabels.DeskStatusClass(progress, urgent));
    }

    [Fact]
    public void Ligne_de_protocole_pour_une_vache_a_diagnostic_de_gestation()
    {
        var cow = new Cow("4812", "F001", "Norma", new DateOnly(2021, 3, 1), 2, new DateOnly(2026, 1, 10), Today.AddDays(-35), ReproStatus.Bred, 120);
        var item = DailyActions.Compute([cow], Today).Items.Single();

        Assert.Equal("IA J+35", TourLabels.Stage(item));
        Assert.Equal("DG", TourLabels.Planned(item));
        Assert.Equal("Saisir DG", TourLabels.QuickAction(item));
    }

    [Fact]
    public void Ligne_de_protocole_sans_diagnostic_de_gestation()
    {
        var cow = new Cow("4813", "F001", "Olympe", new DateOnly(2021, 3, 1), 2, new DateOnly(2026, 1, 10), null, ReproStatus.Open, 900);
        var item = DailyActions.Compute([cow], Today).Items.Single();

        Assert.Equal("Voir la fiche", TourLabels.QuickAction(item));
    }
}
