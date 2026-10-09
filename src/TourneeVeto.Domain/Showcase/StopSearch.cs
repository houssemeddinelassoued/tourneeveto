using TourneeVeto.Domain.Regie;

namespace TourneeVeto.Domain.Showcase;

/// <summary>Recherche d'une étape de la tournée par élevage, éleveur, municipalité ou motif (fonction pure).</summary>
public static class StopSearch
{
    /// <summary>Vrai si <paramref name="query"/> (sans tenir compte de la casse ni des accents) figure dans l'étape ; toujours vrai si elle est vide.</summary>
    public static bool Matches(FarmShowcase stop, string? query)
    {
        ArgumentNullException.ThrowIfNull(stop);
        var needle = CowSearch.Normalize(query);
        if (needle.Length == 0)
        {
            return true;
        }

        return new[] { stop.FarmName, stop.Municipality, stop.Farmer, stop.Reason, stop.Identifier, stop.Protocol }
            .Concat(stop.Tags)
            .Any(field => CowSearch.Normalize(field).Contains(needle, StringComparison.Ordinal));
    }
}
