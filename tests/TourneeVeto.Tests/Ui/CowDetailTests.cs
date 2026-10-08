using System.Text.RegularExpressions;
using Bunit;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Ui.Components;

namespace TourneeVeto.Tests.Ui;

/// <summary>Fiche de la vache sélectionnée : identité, urgence, motifs et repères calculés par le domaine.</summary>
public class CowDetailTests : BunitContext
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static Cow DemoCow() => DemoData.Generate(Today, seed: 42).Cows[0];

    [Fact]
    public void Affiche_l_identite_l_urgence_et_tous_les_motifs()
    {
        var cow = DemoCow() with { Id = "4812", Name = "Bella" };
        var item = new RegieItem(
            cow,
            [new RegieMotive(RegieAction.HighScc, Urgency.Urgent, Days: null), new RegieMotive(RegieAction.PostCalvingCheck, Urgency.Info, Days: 25)],
            Urgency.Urgent,
            Anomaly: null);

        var cut = Render<CowDetail>(parameters => parameters.Add(p => p.Item, item));

        Assert.Equal("N° 4812 Bella", Regex.Replace(cut.Find("h2").TextContent.Trim(), @"\s+", " "));
        Assert.Equal("Urgent", cut.Find(".urgency").TextContent);
        Assert.Equal(["CCS élevé", "Post-vêlage"], cut.FindAll(".motive-name").Select(name => name.TextContent));
        Assert.Equal(["Vêlage J+25"], cut.FindAll(".motive-due").Select(due => due.TextContent));
    }

    [Fact]
    public void Anomalie_sans_motif_invite_a_corriger_la_donnee()
    {
        var item = new RegieItem(DemoCow(), [], Urgency.Warning, RegieAnomaly.MissingInsemination);

        var cut = Render<CowDetail>(parameters => parameters.Add(p => p.Item, item));

        Assert.Equal("Anomalie : Date d'IA manquante", cut.Find(".anomaly").TextContent);
        Assert.Equal("Aucun motif calculé : corriger la donnée signalée.", cut.Find(".empty").TextContent);
    }

    [Fact]
    public void Dates_en_francais_et_absences_explicites()
    {
        var heifer = DemoCow() with { Lactation = 0, BornOn = new DateOnly(2025, 3, 14), LastCalving = null, LastInsemination = null, LastSccThousands = null };
        var item = new RegieItem(heifer, [], Urgency.Warning, RegieAnomaly.InconsistentDate);

        var cut = Render<CowDetail>(parameters => parameters.Add(p => p.Item, item));

        Assert.Equal(
            ["Génisse", "14 mars 2025", "—", "—", "Aucun contrôle"],
            cut.FindAll(".fact dd").Skip(1).Select(value => value.TextContent));
    }
}
