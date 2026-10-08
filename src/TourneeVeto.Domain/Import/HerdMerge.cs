using TourneeVeto.Domain.Herd;

namespace TourneeVeto.Domain.Import;

/// <summary>Manière d'appliquer un import au troupeau existant.</summary>
public enum ImportMode
{
    /// <summary>Met à jour les vaches existantes, ajoute les nouvelles, garde les autres.</summary>
    Merge,

    /// <summary>Ne garde que les vaches du fichier.</summary>
    Replace,
}

/// <summary>Troupeau après import (trié par numéro) et décompte des changements, pour le résumé affiché.</summary>
public sealed record HerdMergeResult(IReadOnlyList<Cow> Herd, int Added, int Updated, int Unchanged, int Removed);

/// <summary>Applique un import au troupeau d'un élevage, sans doublon (une vache = un numéro).</summary>
public static class HerdMerge
{
    public static HerdMergeResult Merge(IReadOnlyList<Cow> existing, IReadOnlyList<Cow> imported, ImportMode mode)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(imported);
        if (existing.Concat(imported).Select(cow => cow.FarmId).Distinct(StringComparer.Ordinal).Skip(1).Any())
        {
            throw new ArgumentException("Toutes les vaches doivent appartenir au même élevage.", nameof(imported));
        }

        var current = existing.ToDictionary(cow => cow.Id, StringComparer.Ordinal);
        var herd = mode == ImportMode.Merge
            ? new Dictionary<string, Cow>(current, StringComparer.Ordinal)
            : new Dictionary<string, Cow>(StringComparer.Ordinal);
        int added = 0, updated = 0, unchanged = 0;

        foreach (var cow in imported)
        {
            if (!current.TryGetValue(cow.Id, out var before))
            {
                added++;
            }
            else if (before == cow)
            {
                unchanged++;
            }
            else
            {
                updated++;
            }

            herd[cow.Id] = cow;
        }

        var removed = mode == ImportMode.Replace ? current.Keys.Count(id => !herd.ContainsKey(id)) : 0;
        var sorted = herd.Values.OrderBy(cow => cow.Id, StringComparer.Ordinal).ToList();
        return new HerdMergeResult(sorted, added, updated, unchanged, removed);
    }
}
