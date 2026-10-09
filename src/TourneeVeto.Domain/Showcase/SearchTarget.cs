using System.Globalization;
using System.Text;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Domain.Showcase;

/// <summary>Destination de la recherche globale : la grille de régie d'un élevage, éventuellement filtrée.</summary>
/// <param name="FarmId">Élevage à ouvrir.</param>
/// <param name="Query">Filtre à appliquer dans la grille ; <c>null</c> pour afficher toute la grille.</param>
public sealed record SearchTarget(string FarmId, string? Query)
{
    /// <summary>
    /// Résout la saisie de la barre de recherche (N° d'identification ou élevage) : un nom d'élevage ouvre sa grille entière,
    /// un numéro ou un nom de vache ouvre la grille de son élevage filtrée, tout autre texte filtre l'élevage courant.
    /// </summary>
    public static SearchTarget Resolve(string? query, IReadOnlyList<Farm> farms, IReadOnlyList<Cow> cows, string? currentFarmId)
    {
        ArgumentNullException.ThrowIfNull(farms);
        ArgumentNullException.ThrowIfNull(cows);

        var fallback = farms.Any(farm => farm.Id == currentFarmId) ? currentFarmId! : farms.FirstOrDefault()?.Id ?? currentFarmId ?? string.Empty;
        var needle = Normalize(query);
        if (needle.Length == 0)
        {
            return new SearchTarget(fallback, null);
        }

        var farm = farms.FirstOrDefault(candidate => Normalize(candidate.Name).Contains(needle, StringComparison.Ordinal)
            || Normalize(candidate.Municipality).Contains(needle, StringComparison.Ordinal));
        if (farm is not null)
        {
            return new SearchTarget(farm.Id, null);
        }

        var text = query!.Trim();
        var cow = cows.FirstOrDefault(candidate => candidate.FarmId == fallback && Matches(candidate, needle))
            ?? cows.FirstOrDefault(candidate => Matches(candidate, needle));
        return new SearchTarget(cow?.FarmId ?? fallback, text);
    }

    private static bool Matches(Cow cow, string needle) =>
        Normalize(cow.Id).Contains(needle, StringComparison.Ordinal) || Normalize(cow.Name).Contains(needle, StringComparison.Ordinal);

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var character in text.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}
