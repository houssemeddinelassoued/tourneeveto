namespace TourneeVeto.Domain.Visits;

/// <summary>Élevage (ferme) suivi par le vétérinaire. Données fictives uniquement.</summary>
/// <param name="Id">Identifiant de l'élevage (ex. « F001 »).</param>
/// <param name="Name">Nom de la ferme.</param>
/// <param name="Municipality">Municipalité (fictive).</param>
/// <param name="CowCount">Effectif : nombre de vaches et génisses actives.</param>
public sealed record Farm(string Id, string Name, string Municipality, int CowCount);

/// <summary>Visite planifiée du vétérinaire dans un élevage.</summary>
/// <param name="Id">Identifiant unique de la visite.</param>
/// <param name="FarmId">Identifiant de l'élevage visité.</param>
/// <param name="Date">Date de la visite.</param>
/// <param name="Reason">Motif de la visite (ex. « Suivi de reproduction »).</param>
/// <param name="Notes">Notes libres du vétérinaire.</param>
/// <param name="PhotoIds">Identifiants des photos stockées dans IndexedDB (ADR 0001).</param>
public sealed record Visit(
    Guid Id,
    string FarmId,
    DateOnly Date,
    string Reason,
    string Notes,
    IReadOnlyList<Guid> PhotoIds)
{
    /// <summary>Copie défensive : la liste reste immuable même si l'appelant modifie la sienne.</summary>
    public IReadOnlyList<Guid> PhotoIds { get; init; } = [.. PhotoIds];
}
