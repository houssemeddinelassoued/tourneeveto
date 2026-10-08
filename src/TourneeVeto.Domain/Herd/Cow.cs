namespace TourneeVeto.Domain.Herd;

/// <summary>Statut de reproduction simplifié (voir le glossaire de PRODUCT.md).</summary>
public enum ReproStatus
{
    /// <summary>Vide : non inséminée depuis le dernier vêlage, ou génisse non inséminée.</summary>
    Open,

    /// <summary>Inséminée, gestation non encore confirmée par un diagnostic de gestation.</summary>
    Bred,

    /// <summary>Gestante, confirmée par un diagnostic de gestation.</summary>
    Pregnant,

    /// <summary>Tarie : gestante, traite arrêtée avant le vêlage.</summary>
    Dry,
}

/// <summary>Vache ou génisse d'un élevage. Données fictives uniquement.</summary>
/// <param name="Id">Numéro de la vache dans l'élevage (ex. « 1007 »).</param>
/// <param name="FarmId">Identifiant de l'élevage (<see cref="Visits.Farm.Id"/>), indexé dans IndexedDB (ADR 0001).</param>
/// <param name="Name">Nom d'usage de la vache.</param>
/// <param name="BornOn">Date de naissance.</param>
/// <param name="Lactation">Rang de lactation ; 0 pour une génisse.</param>
/// <param name="LastCalving">Date du dernier vêlage ; <c>null</c> pour une génisse.</param>
/// <param name="LastInsemination">Date de la dernière insémination ; <c>null</c> si aucune.</param>
/// <param name="Status">Statut de reproduction.</param>
/// <param name="LastSccThousands">Dernier CCS individuel, en milliers de cellules/mL ; <c>null</c> si aucun contrôle.</param>
public sealed record Cow(
    string Id,
    string FarmId,
    string Name,
    DateOnly BornOn,
    int Lactation,
    DateOnly? LastCalving,
    DateOnly? LastInsemination,
    ReproStatus Status,
    int? LastSccThousands)
{
    /// <summary>Génisse : femelle qui n'a jamais vêlé.</summary>
    public bool IsHeifer => Lactation == 0;
}
