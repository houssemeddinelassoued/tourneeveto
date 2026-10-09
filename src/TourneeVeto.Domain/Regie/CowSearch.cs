using System.Globalization;
using System.Text;

namespace TourneeVeto.Domain.Regie;

/// <summary>Recherche dans la grille de régie par numéro de boucle ou nom (fonction pure).</summary>
public static class CowSearch
{
    /// <summary>Lignes dont le numéro ou le nom contient <paramref name="query"/> (sans tenir compte de la casse ni des accents) ; tout si la recherche est vide.</summary>
    public static IEnumerable<RegieItem> Filter(IEnumerable<RegieItem> items, string? query)
    {
        ArgumentNullException.ThrowIfNull(items);
        var needle = Normalize(query);
        return needle.Length == 0
            ? items
            : items.Where(item => Normalize(item.Cow.Id).Contains(needle, StringComparison.Ordinal) || Normalize(item.Cow.Name).Contains(needle, StringComparison.Ordinal));
    }

    internal static string Normalize(string? text)
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
