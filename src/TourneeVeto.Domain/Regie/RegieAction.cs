namespace TourneeVeto.Domain.Regie;

/// <summary>Action attendue sur une vache lors de la visite (motif de la grille de régie, voir PRODUCT.md).</summary>
public enum RegieAction
{
    /// <summary>Vêlage prévu prochainement : surveillance.</summary>
    CalvingSoon,

    /// <summary>Diagnostic de gestation à faire.</summary>
    PregnancyCheck,

    /// <summary>Tarissement à faire.</summary>
    DryOff,

    /// <summary>CCS individuel élevé : suspicion de mammite.</summary>
    HighScc,

    /// <summary>Examen post-vêlage.</summary>
    PostCalvingCheck,

    /// <summary>Vache non inséminée après le délai fixé.</summary>
    NotInseminated,
}

/// <summary>Niveau d'urgence d'une action de régie, du plus calme au plus urgent.</summary>
public enum Urgency
{
    /// <summary>Rien d'anormal (ex. tarissement planifié).</summary>
    Ok,

    /// <summary>Action courante de la visite.</summary>
    Info,

    /// <summary>À surveiller ou à faire bientôt.</summary>
    Warning,

    /// <summary>À traiter en priorité.</summary>
    Urgent,
}
