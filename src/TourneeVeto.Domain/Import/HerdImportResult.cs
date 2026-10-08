using TourneeVeto.Domain.Herd;

namespace TourneeVeto.Domain.Import;

/// <summary>Erreur d'une ligne du fichier ; <see cref="Line"/> est le numéro de ligne dans le fichier, en-tête compris.</summary>
/// <param name="Line">Numéro de ligne (1 = en-tête).</param>
/// <param name="Column">Colonne en cause (« en-tête » pour la ligne 1) ; <c>null</c> si toute la ligne est en cause.</param>
/// <param name="Message">Message destiné au vétérinaire.</param>
public sealed record HerdImportError(int Line, string? Column, string Message)
{
    /// <summary>Forme affichée dans l'aperçu : « ligne 12, date_velage : date invalide ».</summary>
    public override string ToString() => Column is null ? $"ligne {Line} : {Message}" : $"ligne {Line}, {Column} : {Message}";
}

/// <summary>Résultat de l'analyse : vaches valides et erreurs ; une ligne en erreur n'empêche pas d'importer les autres.</summary>
public sealed record HerdImportResult(IReadOnlyList<Cow> Cows, IReadOnlyList<HerdImportError> Errors);
