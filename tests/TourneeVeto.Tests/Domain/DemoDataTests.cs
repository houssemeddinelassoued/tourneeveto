using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;

namespace TourneeVeto.Tests.Domain;

/// <summary>Jeu de démonstration (story 2.1 et 2.2) : quantités exactes, dates relatives au jour, déterminisme.</summary>
public class DemoDataTests
{
    private const int Seed = 42;

    public static TheoryData<DateOnly> Days => [new(2026, 10, 8), new(2028, 2, 29), new(2026, 12, 31), new(2027, 1, 1)];

    [Theory]
    [MemberData(nameof(Days))]
    public void Contient_les_quantites_annoncees_quel_que_soit_le_jour(DateOnly today)
    {
        var data = DemoData.Generate(today, Seed);

        int DaysUntil(DateOnly date) => date.DayNumber - today.DayNumber;
        DateOnly? ExpectedCalving(Cow cow) =>
            cow.Status is ReproStatus.Pregnant or ReproStatus.Dry && cow.LastInsemination is { } insemination ? insemination.AddDays(280) : null;

        Assert.Equal(DemoData.CowCount, data.Cows.Count);
        Assert.Equal(DemoData.CalvingsDueWithin14Days, data.Cows.Count(cow => ExpectedCalving(cow) is { } calving && DaysUntil(calving) is >= 0 and <= 14));
        Assert.Equal(DemoData.PregnancyChecksDue, data.Cows.Count(cow => cow.Status == ReproStatus.Bred && cow.LastInsemination is { } insemination && -DaysUntil(insemination) is >= 30 and <= 45));
        Assert.Equal(DemoData.DryOffsDue, data.Cows.Count(cow => cow.Status == ReproStatus.Pregnant && ExpectedCalving(cow) is { } calving && DaysUntil(calving) <= 60));
        Assert.Equal(DemoData.HighSccCows, data.Cows.Count(cow => cow.LastSccThousands > 200));
        Assert.Equal(DemoData.Heifers, data.Cows.Count(cow => cow.IsHeifer));
    }

    [Fact]
    public void Effectif_de_chaque_ferme_correspond_a_ses_vaches()
    {
        var data = DemoData.Generate(new DateOnly(2026, 10, 8), Seed);

        Assert.All(data.Farms, farm => Assert.Equal(farm.CowCount, data.Cows.Count(cow => cow.FarmId == farm.Id)));
        Assert.Equal(data.Cows.Count, data.Cows.Select(cow => cow.Id).Distinct().Count());
    }

    [Fact]
    public void Tournee_du_jour_compte_une_visite_par_ferme_a_la_date_donnee()
    {
        var today = new DateOnly(2026, 10, 8);

        var data = DemoData.Generate(today, Seed);

        Assert.Equal(DemoData.VisitsToday, data.Visits.Count);
        Assert.All(data.Visits, visit => Assert.Equal(today, visit.Date));
        Assert.Equal(data.Farms.Select(farm => farm.Id), data.Visits.Select(visit => visit.FarmId));
        Assert.Equal(data.Visits.Count, data.Visits.Select(visit => visit.Id).Distinct().Count());
    }

    [Fact]
    public void Meme_jour_et_meme_graine_donnent_le_meme_jeu()
    {
        var today = new DateOnly(2026, 10, 8);

        var first = DemoData.Generate(today, Seed);
        var second = DemoData.Generate(today, Seed);

        Assert.Equal(first.Cows, second.Cows);
        Assert.Equal(first.Farms, second.Farms);
        Assert.Equal(first.Visits.Select(visit => visit.Id), second.Visits.Select(visit => visit.Id));
    }
}
