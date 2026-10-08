using TourneeVeto.Domain.Herd;

namespace TourneeVeto.Domain.Regie;

/// <summary>Calcule la grille de régie (règles et urgences de PRODUCT.md, « Motifs de la grille de régie »).</summary>
public static class DailyActions
{
    /// <summary>Calcule la grille de régie de <paramref name="cows"/> pour la visite du <paramref name="today"/>.</summary>
    /// <param name="cows">Vaches de l'élevage.</param>
    /// <param name="today">Date de la visite ; jamais lue depuis l'horloge.</param>
    /// <param name="thresholds">Seuils ; <see cref="RegieThresholds.Default"/> si absents ou invalides.</param>
    public static RegieGrid Compute(IEnumerable<Cow> cows, DateOnly today, RegieThresholds? thresholds = null)
    {
        ArgumentNullException.ThrowIfNull(cows);

        var usesDefaultThresholds = thresholds is { IsValid: false };
        var rules = thresholds is { IsValid: true } ? thresholds : RegieThresholds.Default;

        var items = cows
            .Select(cow => Evaluate(cow, today, rules))
            .OfType<RegieItem>()
            .OrderByDescending(item => item.Urgency)
            .ThenBy(item => item.Cow.Id, StringComparer.Ordinal)
            .ToList();

        return new RegieGrid(items, usesDefaultThresholds);
    }

    private static RegieItem? Evaluate(Cow cow, DateOnly today, RegieThresholds rules)
    {
        var motives = new List<RegieMotive>();
        RegieAnomaly? anomaly = null;

        // Une date incohérente ne bloque que les motifs qui en dépendent : le CCS reste toujours évalué.
        if (cow.LastInsemination > today)
        {
            anomaly = RegieAnomaly.InconsistentDate;
        }
        else if (cow.LastInsemination is { } insemination)
        {
            AddInseminationMotives(cow, DaysBetween(insemination, today), rules, motives);
        }
        else if (cow.Status is ReproStatus.Pregnant or ReproStatus.Dry)
        {
            anomaly = RegieAnomaly.MissingInsemination;
        }

        if (cow.LastCalving > today)
        {
            anomaly ??= RegieAnomaly.InconsistentDate;
        }
        else if (cow.LastCalving is { } calving)
        {
            AddCalvingMotives(cow, DaysBetween(calving, today), rules, motives);
        }

        if (cow.LastSccThousands > rules.HighSccThousands)
        {
            motives.Add(new RegieMotive(RegieAction.HighScc, Urgency.Urgent, Days: null));
        }

        if (motives.Count == 0 && anomaly is null)
        {
            return null;
        }

        var ordered = motives.OrderByDescending(motive => motive.Urgency).ThenBy(motive => motive.Action).ToList();
        var urgency = ordered.Count > 0 ? ordered[0].Urgency : Urgency.Warning;
        if (anomaly is not null && urgency < Urgency.Warning)
        {
            // Une donnée à corriger mérite au moins d'être surveillée.
            urgency = Urgency.Warning;
        }

        return new RegieItem(cow, ordered, urgency, anomaly);
    }

    private static void AddInseminationMotives(Cow cow, int daysSinceInsemination, RegieThresholds rules, List<RegieMotive> motives)
    {
        if (cow.Status == ReproStatus.Bred && daysSinceInsemination >= rules.PregnancyCheckFromDays)
        {
            var overdue = daysSinceInsemination > rules.PregnancyCheckToDays;
            motives.Add(new RegieMotive(RegieAction.PregnancyCheck, overdue ? Urgency.Warning : Urgency.Info, daysSinceInsemination, overdue));
        }

        if (cow.Status is not (ReproStatus.Pregnant or ReproStatus.Dry))
        {
            return;
        }

        var daysToCalving = rules.GestationDays - daysSinceInsemination;
        if (daysToCalving <= rules.CalvingSoonDays)
        {
            motives.Add(new RegieMotive(RegieAction.CalvingSoon, Urgency.Warning, daysToCalving));
        }

        // Une génisse n'est pas traite : rien à tarir.
        if (cow.Status == ReproStatus.Pregnant && !cow.IsHeifer && daysToCalving <= rules.DryOffDaysBeforeCalving)
        {
            motives.Add(new RegieMotive(RegieAction.DryOff, Urgency.Ok, daysToCalving));
        }
    }

    private static void AddCalvingMotives(Cow cow, int daysSinceCalving, RegieThresholds rules, List<RegieMotive> motives)
    {
        if (daysSinceCalving >= rules.PostCalvingFromDays && daysSinceCalving <= rules.PostCalvingToDays)
        {
            motives.Add(new RegieMotive(RegieAction.PostCalvingCheck, Urgency.Info, daysSinceCalving));
        }

        if (cow.Status == ReproStatus.Open && daysSinceCalving > rules.OpenAfterCalvingDays)
        {
            motives.Add(new RegieMotive(RegieAction.NotInseminated, Urgency.Warning, daysSinceCalving));
        }
    }

    private static int DaysBetween(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber;
}
