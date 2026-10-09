using TourneeVeto.Ui.Formatting;

namespace TourneeVeto.Tests.Ui;

/// <summary>Libellés de l'accueil : heures, distances et durées à la française.</summary>
public class ShowcaseLabelsTests
{
    [Theory]
    [InlineData(8, 30, "08h30")]
    [InlineData(14, 5, "14h05")]
    public void Heure_a_la_francaise(int hour, int minute, string expected) =>
        Assert.Equal(expected, ShowcaseLabels.Time(new TimeOnly(hour, minute)));

    [Fact]
    public void Distance_avec_virgule_decimale() => Assert.Equal("9,2 km", ShowcaseLabels.Km(9.2));

    [Theory]
    [InlineData(45, "45 min")]
    [InlineData(60, "1h")]
    [InlineData(90, "1h30")]
    [InlineData(65, "1h05")]
    public void Duree_lisible(int minutes, string expected) => Assert.Equal(expected, ShowcaseLabels.Duration(minutes));

    [Fact]
    public void Date_longue_avec_majuscule() => Assert.Equal("Jeudi 8 octobre 2026", ShowcaseLabels.LongDate(new DateOnly(2026, 10, 8)));

    [Theory]
    [InlineData(1, "1 élevage")]
    [InlineData(3, "3 élevages")]
    public void Pluriel(int count, string expected) => Assert.Equal(expected, ShowcaseLabels.Count(count, "élevage", "élevages"));

    [Fact]
    public void Date_courte_numerique_ou_tiret() =>
        Assert.Equal(["12/09/2026", "—"], [ShowcaseLabels.ShortDate(new DateOnly(2026, 9, 12)), ShowcaseLabels.ShortDate(null)]);

    [Theory]
    [InlineData("2022-02-01", "4 ans 8 m.")]
    [InlineData("2026-03-15", "6 m.")]
    [InlineData("2024-10-08", "2 ans")]
    [InlineData("2025-10-08", "1 an")]
    [InlineData("2026-10-01", "7 j")]
    public void Age_a_la_francaise(string born, string expected) =>
        Assert.Equal(expected, ShowcaseLabels.Age(DateOnly.Parse(born, System.Globalization.CultureInfo.InvariantCulture), new DateOnly(2026, 10, 8)));
}
