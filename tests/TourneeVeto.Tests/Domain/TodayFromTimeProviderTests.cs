using Microsoft.Extensions.Time.Testing;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using static TourneeVeto.Tests.Domain.Regie.RegieTestCows;

namespace TourneeVeto.Tests.Domain;

/// <summary>
/// « Aujourd'hui » vient du TimeProvider (heure locale, comme Home.razor) puis est passé au domaine en DateOnly :
/// minuit, fuseau horaire et changements d'heure ne doivent jamais décaler le nombre de jours.
/// </summary>
public class TodayFromTimeProviderTests
{
    private static readonly TimeZoneInfo Toronto = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");

    private static DateOnly TodayOf(TimeProvider provider) => DateOnly.FromDateTime(provider.GetLocalNow().DateTime);

    private static int? DaysSinceInsemination(DateOnly insemination, DateOnly today)
    {
        var cow = Neutral() with { Status = ReproStatus.Bred, LastInsemination = insemination, LastCalving = null };
        return Motive(DailyActions.Compute([cow], today), RegieAction.PregnancyCheck)?.Days;
    }

    [Fact]
    public void A_23h59_on_est_encore_la_veille_et_a_0h01_on_est_le_lendemain()
    {
        var provider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 23, 59, 0, TimeSpan.Zero));
        var before = TodayOf(provider);

        provider.Advance(TimeSpan.FromMinutes(2));
        var after = TodayOf(provider);

        Assert.Equal(new DateOnly(2026, 10, 8), before);
        Assert.Equal(new DateOnly(2026, 10, 9), after);
        Assert.Equal(30, DaysSinceInsemination(new DateOnly(2026, 9, 8), before));
        Assert.Equal(31, DaysSinceInsemination(new DateOnly(2026, 9, 8), after));
    }

    [Fact]
    public void Meme_instant_UTC_donne_un_jour_different_a_Toronto_et_en_UTC()
    {
        var instant = new DateTimeOffset(2026, 10, 9, 2, 0, 0, TimeSpan.Zero);
        var utc = new FakeTimeProvider(instant);
        var toronto = new FakeTimeProvider(instant);
        toronto.SetLocalTimeZone(Toronto);
        var insemination = new DateOnly(2026, 8, 24);

        Assert.Equal(new DateOnly(2026, 10, 9), TodayOf(utc));
        Assert.Equal(new DateOnly(2026, 10, 8), TodayOf(toronto));
        Assert.Equal(46, DaysSinceInsemination(insemination, TodayOf(utc)));
        Assert.Equal(45, DaysSinceInsemination(insemination, TodayOf(toronto)));

        // 45 jours : encore dans la fenêtre du DG ; 46 jours : en retard.
        var utcMotive = Motive(DailyActions.Compute([Neutral() with { Status = ReproStatus.Bred, LastInsemination = insemination, LastCalving = null }], TodayOf(utc)), RegieAction.PregnancyCheck);
        var torontoMotive = Motive(DailyActions.Compute([Neutral() with { Status = ReproStatus.Bred, LastInsemination = insemination, LastCalving = null }], TodayOf(toronto)), RegieAction.PregnancyCheck);
        Assert.True(utcMotive?.IsOverdue);
        Assert.False(torontoMotive?.IsOverdue);
    }

    [Fact]
    public void Passage_a_l_heure_d_ete_le_8_mars_2026_ne_decale_pas_les_jours()
    {
        var provider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 8, 5, 30, 0, TimeSpan.Zero));
        provider.SetLocalTimeZone(Toronto);
        var insemination = new DateOnly(2026, 2, 6);
        var beforeDay = TodayOf(provider);

        // La journée du 8 mars ne dure que 23 h : 23 h plus tard, il est déjà 0 h 30 le 9 mars.
        provider.Advance(TimeSpan.FromHours(23));
        var nextDay = TodayOf(provider);

        Assert.Equal(new DateOnly(2026, 3, 8), beforeDay);
        Assert.Equal(new DateOnly(2026, 3, 9), nextDay);
        Assert.Equal(1, nextDay.DayNumber - beforeDay.DayNumber);
        Assert.Equal(30, DaysSinceInsemination(insemination, beforeDay));
        Assert.Equal(31, DaysSinceInsemination(insemination, nextDay));
    }

    [Fact]
    public void Retour_a_l_heure_normale_le_1er_novembre_2026_ne_decale_pas_les_jours()
    {
        var provider = new FakeTimeProvider(new DateTimeOffset(2026, 11, 1, 4, 30, 0, TimeSpan.Zero));
        provider.SetLocalTimeZone(Toronto);
        var insemination = new DateOnly(2026, 10, 1);
        var beforeDay = TodayOf(provider);

        // La journée du 1er novembre dure 25 h : 24 h plus tard, on est encore le 1er novembre.
        provider.Advance(TimeSpan.FromHours(24));
        var sameDay = TodayOf(provider);
        provider.Advance(TimeSpan.FromHours(1));
        var nextDay = TodayOf(provider);

        Assert.Equal(new DateOnly(2026, 11, 1), beforeDay);
        Assert.Equal(beforeDay, sameDay);
        Assert.Equal(new DateOnly(2026, 11, 2), nextDay);
        Assert.Equal(31, DaysSinceInsemination(insemination, sameDay));
        Assert.Equal(32, DaysSinceInsemination(insemination, nextDay));
    }
}
