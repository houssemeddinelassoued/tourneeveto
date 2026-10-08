using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using static TourneeVeto.Tests.Domain.Regie.RegieTestCows;

namespace TourneeVeto.Tests.Domain.Regie;

/// <summary>Story #27 et PRODUCT.md : une ligne par vache, urgence la plus élevée, tri de la grille.</summary>
public class DailyActionsTests
{
    [Fact]
    public void Vache_a_plusieurs_motifs_n_apparait_qu_une_fois_avec_l_urgence_la_plus_elevee()
    {
        var cow = Neutral() with { Status = ReproStatus.Open, LastCalving = DaysAgo(25), LastInsemination = null, LastSccThousands = 300 };

        var item = Assert.Single(Grid(cow).Items);

        Assert.Equal(Urgency.Urgent, item.Urgency);
        Assert.Equal([RegieAction.HighScc, RegieAction.PostCalvingCheck], item.Motives.Select(motive => motive.Action));
    }

    [Fact]
    public void Vache_sans_motif_ni_anomalie_n_est_pas_dans_la_grille()
    {
        Assert.Empty(Grid(Neutral()).Items);
    }

    [Fact]
    public void Grille_est_triee_par_urgence_puis_par_numero()
    {
        var dryOff = Neutral("9003") with { LastInsemination = InseminationForCalvingIn(50) };
        var highScc = Neutral("9002") with { LastSccThousands = 500 };
        var pregnancyCheckB = Neutral("9005") with { Status = ReproStatus.Bred, LastInsemination = DaysAgo(31), LastCalving = DaysAgo(120) };
        var pregnancyCheckA = Neutral("9004") with { Status = ReproStatus.Bred, LastInsemination = DaysAgo(40), LastCalving = DaysAgo(120) };

        var grid = Grid(dryOff, highScc, pregnancyCheckB, pregnancyCheckA);

        Assert.Equal(["9002", "9004", "9005", "9003"], grid.Items.Select(item => item.Cow.Id));
    }

    [Fact]
    public void La_date_de_la_visite_est_un_parametre()
    {
        var cow = Neutral() with { Status = ReproStatus.Bred, LastInsemination = DaysAgo(29), LastCalving = DaysAgo(120) };

        Assert.Empty(DailyActions.Compute([cow], Today).Items);
        Assert.Single(DailyActions.Compute([cow], Today.AddDays(1)).Items);
    }

    [Fact]
    public void Jeu_de_demo_sans_vache_n_a_pas_de_grille()
    {
        Assert.Empty(DailyActions.Compute([], Today).Items);
        Assert.NotEmpty(DailyActions.Compute(DemoData.Generate(Today, seed: 42).Cows, Today).Items);
    }
}
