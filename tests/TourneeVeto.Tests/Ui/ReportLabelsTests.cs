using TourneeVeto.Domain.Showcase;
using TourneeVeto.Ui.Formatting;

namespace TourneeVeto.Tests.Ui;

public class ReportLabelsTests
{
    [Theory]
    [InlineData(null, "Non saisi")]
    [InlineData(80, "Réussite")]
    [InlineData(55, "Correct")]
    [InlineData(20, "À surveiller")]
    public void Taux_de_gestation_a_une_mention(int? rate, string expected) => Assert.Equal(expected, ReportLabels.RateVerdict(rate));

    [Fact]
    public void Liste_de_vaches_est_accordee()
    {
        Assert.Equal("Vache A1", ReportLabels.CowList(["A1"]));
        Assert.Equal("Vaches A1 et B2", ReportLabels.CowList(["A1", "B2"]));
        Assert.Equal("Vaches A1, B2 et C3", ReportLabels.CowList(["A1", "B2", "C3"]));
    }

    [Fact]
    public void Constat_sans_saisie_donne_un_repli_lisible()
    {
        Assert.Contains("Aucun résultat saisi", ReportLabels.FindingText(new ReportFinding("mammary", "M", 0, "cas", [], 2, 0)));
        Assert.Contains("Aucune vache", ReportLabels.FindingText(new ReportFinding("mammary", "M", 0, "cas", [], 0, 0)));
        Assert.Equal("Vaches A et B.", ReportLabels.FindingText(new ReportFinding("mammary", "M", 2, "cas", ["A", "B"], 3, 3)));
    }

    [Fact]
    public void Titre_des_pratiques_compte_et_accorde()
    {
        Assert.Equal("Pratiques prioritaires prescrites", ReportLabels.PracticesTitle(0));
        Assert.Equal("1 Pratique prioritaire prescrite", ReportLabels.PracticesTitle(1));
        Assert.Equal("3 Pratiques prioritaires prescrites", ReportLabels.PracticesTitle(3));
    }

    [Fact]
    public void Heure_a_deux_chiffres() => Assert.Equal("09:05", ReportLabels.Clock(new TimeOnly(9, 5)));
}
