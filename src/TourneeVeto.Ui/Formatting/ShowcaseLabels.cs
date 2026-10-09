using System.Globalization;

namespace TourneeVeto.Ui.Formatting;

/// <summary>Libellés français (fr-CA) de l'accueil : heures, distances, durées et dates.</summary>
public static class ShowcaseLabels
{
    private static readonly CultureInfo FrenchCanada = CultureInfo.GetCultureInfo("fr-CA");

    /// <summary>Heure à la française : « 08h30 ».</summary>
    public static string Time(TimeOnly time) => $"{time.Hour:D2}h{time.Minute:D2}";

    /// <summary>Distance avec une décimale : « 9,2 km ».</summary>
    public static string Km(double km) => $"{km.ToString("0.0", FrenchCanada)} km";

    /// <summary>Durée : « 45 min », « 1h » ou « 1h30 ».</summary>
    public static string Duration(int minutes) => minutes switch
    {
        < 60 => $"{minutes} min",
        _ when minutes % 60 == 0 => $"{minutes / 60}h",
        _ => $"{minutes / 60}h{minutes % 60:D2}",
    };

    /// <summary>Date longue avec majuscule initiale : « Jeudi 8 octobre 2026 ».</summary>
    public static string LongDate(DateOnly date)
    {
        var label = date.ToString("dddd d MMMM yyyy", FrenchCanada);
        return label.Length == 0 ? label : char.ToUpper(label[0], FrenchCanada) + label[1..];
    }

    /// <summary>Date numérique : « 12/09/2026 » ; un tiret si elle est absente.</summary>
    public static string ShortDate(DateOnly? date) => date is { } value ? value.ToString("dd'/'MM'/'yyyy", FrenchCanada) : "—";

    /// <summary>Date courte sans l'année : « jeudi 8 octobre ».</summary>
    public static string DayMonth(DateOnly date) => date.ToString("dddd d MMMM", FrenchCanada);

    /// <summary>Mois et année avec majuscule : « Octobre 2026 ».</summary>
    public static string MonthYear(DateOnly date)
    {
        var label = date.ToString("MMMM yyyy", FrenchCanada);
        return label.Length == 0 ? label : char.ToUpper(label[0], FrenchCanada) + label[1..];
    }

    /// <summary>Âge à la française : « 4 ans 8 m. », « 7 m. » ou « 12 j » pour un veau.</summary>
    public static string Age(DateOnly birth, DateOnly today)
    {
        var months = (today.Year - birth.Year) * 12 + today.Month - birth.Month - (today.Day < birth.Day ? 1 : 0);
        return months switch
        {
            < 1 => $"{Math.Max(0, today.DayNumber - birth.DayNumber)} j",
            < 12 => $"{months} m.",
            _ => $"{months / 12} {(months < 24 ? "an" : "ans")}{(months % 12 == 0 ? string.Empty : $" {months % 12} m.")}",
        };
    }

    /// <summary>« 1 élevage » ou « 3 élevages ».</summary>
    public static string Count(int count, string singular, string plural) => $"{count} {(count > 1 ? plural : singular)}";
}
