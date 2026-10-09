namespace TourneeVeto.Domain.Visits;

/// <summary>Une recommandation distincte du vétérinaire pour le producteur (story 9.2).</summary>
public sealed record Recommendation(Guid Id, string Text);

/// <summary>
/// Recommandations d'une visite, dans l'ordre de saisie (stories 9.1 et 9.2). Les opérations sont immuables : elles renvoient une nouvelle instance.
/// Règles POC : le texte est rogné aux extrémités, accents et retours à la ligne internes conservés ; un texte vide, fait d'espaces
/// ou de plus de <see cref="MaxTextLength"/> caractères est refusé, comme tout ajout au-delà de <see cref="MaxItems"/> (rien n'est créé ni modifié) ; la numérotation 1..n suit l'ordre.
/// L'identifiant et l'instant sont fournis par l'appelant (domaine pur).
/// </summary>
public sealed record VisitRecommendations(Guid VisitId, IReadOnlyList<Recommendation> Items, DateTimeOffset UpdatedAt)
{
    /// <summary>Longueur maximale d'une recommandation, après rognage (assez pour un paragraphe, borne le rapport imprimé).</summary>
    public const int MaxTextLength = 1000;

    /// <summary>Nombre maximal de recommandations par visite (borne le rapport imprimé et le stockage).</summary>
    public const int MaxItems = 20;

    public static VisitRecommendations Empty(Guid visitId, DateTimeOffset now) => new(visitId, [], now);

    /// <summary>Vrai si <see cref="MaxItems"/> est atteint : plus aucun ajout n'est accepté.</summary>
    public bool IsFull() => Items.Count >= MaxItems;

    /// <summary>Recommandations avec leur numéro d'affichage, de 1 à n.</summary>
    public IEnumerable<(int Number, Recommendation Recommendation)> Numbered() =>
        Items.Select((item, index) => (index + 1, item));

    /// <summary>Ajoute en fin de liste ; renvoie <c>false</c> et <paramref name="result"/> inchangé si le texte est refusé.</summary>
    public bool TryAdd(string? text, Guid id, DateTimeOffset now, out VisitRecommendations result)
    {
        if (IsFull() || !TryNormalize(text, out var normalized))
        {
            result = this;
            return false;
        }

        result = this with { Items = [.. Items, new Recommendation(id, normalized)], UpdatedAt = now };
        return true;
    }

    /// <summary>Remplace le texte en gardant la place ; <c>false</c> si le texte est refusé ou la recommandation inconnue.</summary>
    public bool TryUpdate(Guid id, string? text, DateTimeOffset now, out VisitRecommendations result)
    {
        var index = IndexOf(id);
        if (index < 0 || !TryNormalize(text, out var normalized))
        {
            result = this;
            return false;
        }

        var items = Items.ToArray();
        items[index] = items[index] with { Text = normalized };
        result = this with { Items = items, UpdatedAt = now };
        return true;
    }

    /// <summary>Retire la recommandation ; sans effet (même instance) si elle est inconnue.</summary>
    public VisitRecommendations Remove(Guid id, DateTimeOffset now) =>
        IndexOf(id) < 0 ? this : this with { Items = [.. Items.Where(item => item.Id != id)], UpdatedAt = now };

    /// <summary>Texte rogné sans caractères de contrôle ni de format Unicode (sauf retour à la ligne et tabulation), ou <c>false</c> s'il est vide, fait d'espaces ou trop long.</summary>
    public static bool TryNormalize(string? text, out string normalized)
    {
        normalized = Clean(text);
        return normalized.Length > 0 && normalized.Length <= MaxTextLength;
    }

    /// <summary>Vrai si le texte nettoyé dépasse <see cref="MaxTextLength"/> caractères.</summary>
    public static bool IsTooLong(string? text) => Clean(text).Length > MaxTextLength;

    /// <summary>
    /// Assainit des recommandations relues du stockage (données non fiables) : liste absente = vide, textes renormalisés,
    /// textes invalides, identifiants vides ou en double retirés, au plus <see cref="MaxItems"/> conservées.
    /// </summary>
    public static VisitRecommendations Sanitize(VisitRecommendations loaded)
    {
        var items = new List<Recommendation>();
        var seen = new HashSet<Guid>();
        foreach (var item in loaded.Items ?? [])
        {
            if (item is not null && item.Id != Guid.Empty && seen.Add(item.Id) && TryNormalize(item.Text, out var text))
            {
                items.Add(item with { Text = text });
            }
        }

        return loaded with { Items = [.. items.Take(MaxItems)] };
    }

    private static string Clean(string? text)
    {
        if (text is null)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value is '\n' or '\r' or '\t'
                || (!System.Text.Rune.IsControl(rune) && System.Text.Rune.GetUnicodeCategory(rune) != System.Globalization.UnicodeCategory.Format))
            {
                builder.Append(rune.ToString());
            }
        }

        return builder.ToString().Trim();
    }

    private int IndexOf(Guid id)
    {
        for (var index = 0; index < Items.Count; index++)
        {
            if (Items[index].Id == id)
            {
                return index;
            }
        }

        return -1;
    }
}
