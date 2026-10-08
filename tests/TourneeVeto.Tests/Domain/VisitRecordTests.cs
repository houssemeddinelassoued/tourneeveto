using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Tests.Domain;

/// <summary>Epic 7 : résultats proposés par motif, saisie corrigeable, note limitée, avancement de la visite.</summary>
public class VisitRecordTests
{
    private static readonly Guid VisitId = Guid.Parse("4a3f8c1e-2b7d-4e9a-8f10-6c5d4e3b2a19");
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 9, 15, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(RegieAction.PregnancyCheck, new[] { ResultOutcome.Positive, ResultOutcome.Negative, ResultOutcome.Doubtful })]
    [InlineData(RegieAction.DryOff, new[] { ResultOutcome.Done, ResultOutcome.Postponed })]
    [InlineData(RegieAction.CalvingSoon, new[] { ResultOutcome.Done, ResultOutcome.Postponed })]
    [InlineData(RegieAction.HighScc, new[] { ResultOutcome.Positive, ResultOutcome.Negative })]
    [InlineData(RegieAction.PostCalvingCheck, new[] { ResultOutcome.Normal, ResultOutcome.Abnormal })]
    [InlineData(RegieAction.NotInseminated, new[] { ResultOutcome.Done })]
    public void Chaque_motif_propose_ses_resultats(RegieAction action, ResultOutcome[] expected)
    {
        Assert.Equal(expected, ResultOptions.For(action));
    }

    [Fact]
    public void Un_resultat_saisi_est_enregistre_pour_son_motif()
    {
        var record = CowVisitRecord.Empty(VisitId, "1001").WithResult(RegieAction.PregnancyCheck, ResultOutcome.Positive, Now);

        Assert.Equal(ResultOutcome.Positive, record.ResultFor(RegieAction.PregnancyCheck));
        Assert.Null(record.ResultFor(RegieAction.DryOff));
        Assert.Equal(Now, record.UpdatedAt);
    }

    [Fact]
    public void Une_correction_ne_garde_que_le_dernier_resultat()
    {
        var record = CowVisitRecord.Empty(VisitId, "1001")
            .WithResult(RegieAction.PregnancyCheck, ResultOutcome.Positive, Now)
            .WithResult(RegieAction.PregnancyCheck, ResultOutcome.Negative, Now.AddMinutes(1));

        Assert.Equal(ResultOutcome.Negative, Assert.Single(record.Results).Outcome);
    }

    [Fact]
    public void Un_resultat_hors_des_choix_du_motif_est_refuse()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CowVisitRecord.Empty(VisitId, "1001").WithResult(RegieAction.DryOff, ResultOutcome.Positive, Now));
    }

    [Fact]
    public void La_note_est_conservee_a_l_identique()
    {
        var record = CowVisitRecord.Empty(VisitId, "1001").WithNote("Boiterie AP gauche,\nrevoir dans 15 j", Now);

        Assert.Equal("Boiterie AP gauche,\nrevoir dans 15 j", record.Note);
    }

    [Fact]
    public void Une_note_de_plus_de_2000_caracteres_est_refusee()
    {
        var record = CowVisitRecord.Empty(VisitId, "1001");

        Assert.Equal(2000, record.WithNote(new string('a', CowVisitRecord.MaxNoteLength), Now).Note.Length);
        Assert.Throws<ArgumentOutOfRangeException>(() => record.WithNote(new string('a', CowVisitRecord.MaxNoteLength + 1), Now));
    }

    [Fact]
    public void Visite_sans_saisie_est_a_faire_puis_en_cours_des_le_premier_resultat()
    {
        Assert.Equal(VisitProgress.ToDo, VisitProgresses.Of([]));
        Assert.Equal(VisitProgress.ToDo, VisitProgresses.Of([CowVisitRecord.Empty(VisitId, "1001")]));
        Assert.Equal(VisitProgress.InProgress, VisitProgresses.Of([CowVisitRecord.Empty(VisitId, "1001").WithNote("Vue", Now)]));
        Assert.Equal(VisitProgress.InProgress, VisitProgresses.Of(
            [CowVisitRecord.Empty(VisitId, "1001").WithResult(RegieAction.HighScc, ResultOutcome.Negative, Now)]));
    }
}
