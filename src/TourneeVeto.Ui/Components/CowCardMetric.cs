namespace TourneeVeto.Ui.Components;

/// <summary>Indicateur affiché dans la carte d'une vache (ex. « Dernier CCS » / « 112 k cellules/mL »), déjà formaté par l'appelant.</summary>
public sealed record CowCardMetric(string Label, string Value);
