using Bunit;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Showcase;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui.Components;

namespace TourneeVeto.Tests.Ui;

/// <summary>Fiche de la vache sélectionnée : identité, tuiles, surveillance, actes, courbe CCS et note.</summary>
public class CowDetailTests : BunitContext
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static Cow DemoCow() => DemoData.Generate(Today, seed: 42).Cows[0];

    private static RegieItem CalvingItem()
    {
        var cow = DemoCow() with { Id = "4812", Name = "Bella", Lactation = 3, LastSccThousands = 148 };
        return new RegieItem(
            cow,
            [new RegieMotive(RegieAction.CalvingSoon, Urgency.Warning, Days: 2), new RegieMotive(RegieAction.PostCalvingCheck, Urgency.Info, Days: 25)],
            Urgency.Warning,
            Anomaly: null);
    }

    private IRenderedComponent<CowDetail> RenderDetail(RegieItem item, Action<ComponentParameterCollectionBuilder<CowDetail>>? more = null) =>
        Render<CowDetail>(parameters =>
        {
            parameters.Add(p => p.Item, item).Add(p => p.Show, CowShowcaseBuilder.Build(item, Today)).Add(p => p.Today, Today);
            more?.Invoke(parameters);
        });

    [Fact]
    public void Affiche_l_identite_l_identifiant_national_et_l_urgence()
    {
        var item = CalvingItem();

        var cut = RenderDetail(item);

        Assert.Equal("Bella", cut.Find("h2").TextContent);
        Assert.Equal("4812", cut.Find(".badge-value").TextContent);
        Assert.Equal(CowShowcaseBuilder.Build(item, Today).NationalId, cut.Find(".chip--id").TextContent);
        Assert.Equal("L3 en cours", cut.Find(".chip--ok").TextContent);
        Assert.Equal("À surveiller", cut.Find(".chip--urgency").TextContent);
    }

    [Fact]
    public void Affiche_les_motifs_et_leurs_echeances()
    {
        var cut = RenderDetail(CalvingItem());

        Assert.Equal(["Vêlage prévu", "Post-vêlage"], cut.FindAll(".motive-name").Select(name => name.TextContent));
        Assert.Equal(["Vêlage J-2", "Vêlage J+25"], cut.FindAll(".motive-due").Select(due => due.TextContent));
    }

    [Fact]
    public void Anomalie_sans_motif_invite_a_corriger_la_donnee()
    {
        var cut = RenderDetail(new RegieItem(DemoCow(), [], Urgency.Warning, RegieAnomaly.MissingInsemination));

        Assert.Equal("Anomalie : Date d'IA manquante", cut.Find(".anomaly").TextContent);
        Assert.Contains("Aucun motif calculé : corriger la donnée signalée.", cut.FindAll(".empty").Select(empty => empty.TextContent));
        Assert.Empty(cut.FindAll(".act"));
    }

    [Fact]
    public void Quatre_tuiles_d_identite_puis_trois_tuiles_de_surveillance()
    {
        var cut = RenderDetail(CalvingItem());

        var labels = cut.FindAll(".tile-label").Select(label => label.TextContent).ToList();

        Assert.Equal(7, labels.Count);
        Assert.Equal(["Stade physiologique", "Dernier vêlage", "Bilan CCS cumulé", "Statut"], labels.Take(4));
        Assert.Equal("Surveillance périnatale & pré-vêlage", cut.Find(".watch-title").TextContent);
        Assert.Equal("VÊLAGE SOUS 48 H", cut.Find(".watch-badge").TextContent);
    }

    [Fact]
    public void Dates_en_francais_et_absences_explicites()
    {
        var heifer = DemoCow() with { Lactation = 0, BornOn = new DateOnly(2025, 3, 14), LastCalving = null, LastInsemination = null, LastSccThousands = null };

        var cut = RenderDetail(new RegieItem(heifer, [], Urgency.Warning, RegieAnomaly.InconsistentDate));

        Assert.Equal("Génisse", cut.Find(".chip--ok").TextContent);
        Assert.Contains("Née le 14/03/2025 (1 an 6 m.)", cut.Find(".line").TextContent, StringComparison.Ordinal);
        Assert.Contains("Aucun contrôle", cut.FindAll(".tile-value").Select(value => value.TextContent));
        Assert.Equal("—", cut.Find(".production-value").TextContent.Replace(" kg/j", string.Empty).Trim());
    }

    [Fact]
    public void Actes_immediats_sont_les_resultats_reels_avec_le_choix_marque()
    {
        ResultOutcome? chosen = null;

        var cut = RenderDetail(CalvingItem(), parameters => parameters
            .Add(p => p.Outcomes, ResultOptions.For(RegieAction.CalvingSoon))
            .Add(p => p.Outcome, ResultOutcome.Done)
            .Add(p => p.OnOutcome, (ResultOutcome outcome) => chosen = outcome));

        var buttons = cut.FindAll(".act");
        Assert.Equal(ResultOptions.For(RegieAction.CalvingSoon).Count, buttons.Count);
        Assert.Contains("Vêlée", buttons.Select(button => button.TextContent.Trim()));
        Assert.Single(buttons, button => button.GetAttribute("aria-pressed") == "true");

        buttons[^1].Click();

        Assert.Equal(ResultOptions.For(RegieAction.CalvingSoon)[^1], chosen);
    }

    [Fact]
    public void Sans_visite_aucun_acte_n_est_propose()
    {
        var cut = RenderDetail(CalvingItem());

        Assert.Empty(cut.FindAll(".act"));
        Assert.Contains("Aucun acte à saisir", cut.Markup);
    }

    [Fact]
    public void La_courbe_CCS_a_six_points_un_seuil_en_pointilles_et_un_resume_pour_les_lecteurs_d_ecran()
    {
        var cut = RenderDetail(CalvingItem());

        Assert.Equal(6, cut.FindAll("svg[role=img] circle").Count);
        Assert.NotNull(cut.Find("line.threshold").GetAttribute("stroke-dasharray"));
        Assert.Contains("seuil d'alerte à 200k", cut.Find("svg[role=img]").GetAttribute("aria-label"));
        Assert.Equal(3, cut.FindAll("table.interventions tbody tr").Count);
    }

    [Fact]
    public void Enregistrer_note_envoie_l_observation_saisie()
    {
        string? saved = null;
        var cut = RenderDetail(CalvingItem(), parameters => parameters
            .Add(p => p.Note, "Boiterie")
            .Add(p => p.OnNoteChanged, (string note) => saved = note));

        Assert.Equal("Ajouter une observation", cut.Find("label.sr[for$=note]").TextContent);
        Assert.Equal("Boiterie", cut.Find("input.note-input").GetAttribute("value"));

        cut.Find("input.note-input").Input("Ligament relâché");
        cut.Find("form.note").Submit();

        Assert.Equal("Ligament relâché", saved);
    }

    [Fact]
    public void Sans_abonne_le_champ_d_observation_n_est_pas_affiche()
    {
        var cut = RenderDetail(CalvingItem());

        Assert.Empty(cut.FindAll("form.note"));
    }

    [Fact]
    public void La_dictee_vocale_est_simulee()
    {
        var simulations = 0;
        var cut = RenderDetail(CalvingItem(), parameters => parameters
            .Add(p => p.OnNoteChanged, (string _) => { })
            .Add(p => p.OnSimulate, () => simulations++));

        cut.Find(".note-mic").Click();

        Assert.Equal(1, simulations);
        Assert.Equal("Dictée vocale (simulée)", cut.Find(".note-mic").GetAttribute("aria-label"));
    }
}
