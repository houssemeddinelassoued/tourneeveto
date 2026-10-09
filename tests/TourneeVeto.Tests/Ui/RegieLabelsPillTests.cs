using TourneeVeto.Domain;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Ui.Formatting;

namespace TourneeVeto.Tests.Ui;

/// <summary>Pastille d'échéance des cartes de la grille de régie.</summary>
public class RegieLabelsPillTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static readonly TourneeVeto.Domain.Herd.Cow Cow = DemoData.Generate(Today, seed: 42).Cows[0] with { LastSccThousands = 850 };

    [Fact]
    public void Le_CCS_eleve_affiche_le_dernier_comptage()
    {
        Assert.Equal("850k sp/mL", RegieLabels.Pill(new RegieMotive(RegieAction.HighScc, Urgency.Urgent, Days: null), Cow));
    }

    [Fact]
    public void Les_autres_motifs_gardent_leur_echeance()
    {
        Assert.Equal("IA J+35", RegieLabels.Pill(new RegieMotive(RegieAction.PregnancyCheck, Urgency.Info, Days: 35), Cow));
    }

    [Fact]
    public void Sans_motif_pas_de_pastille()
    {
        Assert.Null(RegieLabels.Pill(null, Cow));
    }
}
