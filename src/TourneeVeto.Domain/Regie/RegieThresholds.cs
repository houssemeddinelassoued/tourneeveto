namespace TourneeVeto.Domain.Regie;

/// <summary>Seuils des règles de régie, en jours sauf le CCS ; valeurs par défaut de PRODUCT.md (story #28).</summary>
public sealed record RegieThresholds
{
    public static RegieThresholds Default { get; } = new();

    /// <summary>Vêlage prévu = insémination + ce nombre de jours.</summary>
    public int GestationDays { get; init; } = 280;

    /// <summary>Vêlage prévu signalé dans les N jours qui précèdent (date dépassée comprise).</summary>
    public int CalvingSoonDays { get; init; } = 14;

    /// <summary>Tarissement à faire quand le vêlage prévu est dans N jours ou moins.</summary>
    public int DryOffDaysBeforeCalving { get; init; } = 60;

    /// <summary>Début de la fenêtre du diagnostic de gestation, en jours après l'insémination.</summary>
    public int PregnancyCheckFromDays { get; init; } = 30;

    /// <summary>Fin de la fenêtre ; au-delà, le DG est en retard.</summary>
    public int PregnancyCheckToDays { get; init; } = 45;

    public int PostCalvingFromDays { get; init; } = 21;

    public int PostCalvingToDays { get; init; } = 35;

    /// <summary>Vache vide signalée plus de N jours après le vêlage.</summary>
    public int OpenAfterCalvingDays { get; init; } = 60;

    /// <summary>CCS élevé au-delà de cette valeur, en milliers de cellules/mL.</summary>
    public int HighSccThousands { get; init; } = 200;

    /// <summary>Faux si un seuil est négatif ou nul, ou si une fenêtre se termine avant de commencer.</summary>
    public bool IsValid =>
        GestationDays > 0
        && CalvingSoonDays >= 0
        && DryOffDaysBeforeCalving > 0 && DryOffDaysBeforeCalving < GestationDays
        && PregnancyCheckFromDays > 0 && PregnancyCheckFromDays <= PregnancyCheckToDays
        && PostCalvingFromDays > 0 && PostCalvingFromDays <= PostCalvingToDays
        && OpenAfterCalvingDays > 0
        && HighSccThousands > 0;
}
