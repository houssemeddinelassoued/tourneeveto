using Microsoft.AspNetCore.Components;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Ui.Data;

/// <summary>
/// Accès aux élevages, aux visites, aux vaches et aux photos stockés sur l'appareil (ADR 0001, skill indexeddb-interop).
/// Toute méthode peut lever <see cref="StorageUnavailableException"/>.
/// </summary>
public interface IVisitRepository
{
    /// <summary>Enregistre le jeu de démonstration en une seule transaction, uniquement si la base est vide.</summary>
    /// <returns><c>true</c> si le jeu a été enregistré, <c>false</c> si la base contenait déjà des données.</returns>
    Task<bool> SeedIfEmptyAsync(DemoDataSet data, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Farm>> GetFarmsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Visit>> GetVisitsByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<Visit?> GetVisitAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveVisitAsync(Visit visit, CancellationToken cancellationToken = default);

    /// <summary>Supprime la visite et ses photos.</summary>
    Task DeleteVisitAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cow>> GetCowsByFarmAsync(string farmId, CancellationToken cancellationToken = default);

    Task SaveCowsAsync(IReadOnlyList<Cow> cows, CancellationToken cancellationToken = default);

    /// <summary>Redimensionne et stocke la photo choisie dans <paramref name="fileInput"/> ; <c>null</c> si aucun fichier.</summary>
    Task<Guid?> AddPhotoFromInputAsync(Guid visitId, ElementReference fileInput, CancellationToken cancellationToken = default);

    Task DeletePhotoAsync(Guid photoId, CancellationToken cancellationToken = default);
}
