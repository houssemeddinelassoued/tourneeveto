using Bunit;
using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Regie;
using TourneeVeto.Domain.Visits;
using TourneeVeto.Ui.Components;

namespace TourneeVeto.Tests.Ui;

public class CowCardTests : BunitContext
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    private static Cow DemoCow(int lactation) =>
        DemoData.Generate(Today, seed: 42).Cows.First(cow => !cow.IsHeifer) with { Lactation = lactation };

    [Fact]
    public void Affiche_le_numero_le_nom_et_le_rang_de_lactation()
    {
        var cow = DemoCow(lactation: 3);

        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, cow)
            .Add(p => p.Action, RegieAction.PregnancyCheck)
            .Add(p => p.Urgency, Urgency.Info));

        Assert.Equal(cow.Id, cut.Find(".cow-card__number-value").TextContent);
        Assert.Equal(cow.Name, cut.Find("h3").TextContent.Trim());
        Assert.Equal("3e lact.", cut.Find(".cow-card__lactation").TextContent);
        Assert.Equal("Diagnostic de gestation", cut.Find("h4").TextContent);
    }

    [Fact]
    public void Urgence_est_annoncee_en_texte_et_pas_seulement_par_la_couleur()
    {
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 2))
            .Add(p => p.Action, RegieAction.HighScc)
            .Add(p => p.Urgency, Urgency.Urgent)
            .Add(p => p.DueLabel, "850 k cellules/mL"));

        Assert.Contains("cow-card--urgent", cut.Find("article").ClassList);
        Assert.Equal("Urgent", cut.Find(".cow-card__urgency").TextContent);
        Assert.Equal("850 k cellules/mL", cut.Find(".cow-card__due").TextContent);
    }

    [Fact]
    public void Urgence_calme_reste_lisible_par_les_lecteurs_d_ecran()
    {
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 4))
            .Add(p => p.Action, RegieAction.DryOff)
            .Add(p => p.Urgency, Urgency.Ok));

        Assert.Empty(cut.FindAll(".cow-card__urgency"));
        Assert.Equal("Normal", cut.Find(".cow-card__sr-only").TextContent);
    }

    [Fact]
    public void Boutons_declenchent_l_action_et_l_historique()
    {
        var cow = DemoCow(lactation: 1);
        var actions = 0;
        var histories = 0;

        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, cow)
            .Add(p => p.Action, RegieAction.CalvingSoon)
            .Add(p => p.Urgency, Urgency.Warning)
            .Add(p => p.OnAction, () => actions++)
            .Add(p => p.OnHistory, () => histories++));

        cut.Find(".cow-card__primary").Click();
        cut.Find(".cow-card__history").Click();
        Assert.Empty(cut.FindAll(".cow-card__select"));

        Assert.Equal(1, actions);
        Assert.Equal(1, histories);
        Assert.Equal($"Historique de {cow.Name}", cut.Find(".cow-card__history").GetAttribute("aria-label"));
    }

    [Fact]
    public void Anomalie_seule_devient_le_titre_de_la_carte()
    {
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 2))
            .Add(p => p.Action, null)
            .Add(p => p.Anomaly, RegieAnomaly.MissingInsemination)
            .Add(p => p.Urgency, Urgency.Warning));

        Assert.Equal("Date d'IA manquante", cut.Find("h4").TextContent);
        Assert.Empty(cut.FindAll(".cow-card__anomaly"));
    }

    [Fact]
    public void Anomalie_avec_un_motif_est_affichee_en_texte()
    {
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 3))
            .Add(p => p.Action, RegieAction.HighScc)
            .Add(p => p.Anomaly, RegieAnomaly.InconsistentDate)
            .Add(p => p.Urgency, Urgency.Urgent));

        Assert.Equal("CCS élevé — suspicion de mammite", cut.Find("h4").TextContent);
        Assert.Equal("Anomalie : Date incohérente", cut.Find(".cow-card__anomaly").TextContent);
    }

    [Fact]
    public void Sans_historique_branche_le_bouton_n_est_pas_affiche()
    {
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 2))
            .Add(p => p.Action, RegieAction.DryOff)
            .Add(p => p.Urgency, Urgency.Ok));

        Assert.Empty(cut.FindAll(".cow-card__history"));
    }

    [Fact]
    public void Le_nom_selectionne_la_vache_quand_la_page_le_permet()
    {
        var selections = 0;
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 2))
            .Add(p => p.Action, RegieAction.DryOff)
            .Add(p => p.Urgency, Urgency.Ok)
            .Add(p => p.Selected, true)
            .Add(p => p.OnSelect, () => selections++));

        cut.Find(".cow-card__select").Click();

        Assert.Equal(1, selections);
        Assert.Equal("true", cut.Find(".cow-card__select").GetAttribute("aria-pressed"));
        Assert.Contains("cow-card--selected", cut.Find("article").ClassList);
    }

    [Fact]
    public void Resultats_proposes_en_une_touche_avec_le_choix_marque()
    {
        ResultOutcome? chosen = null;
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 2))
            .Add(p => p.Action, RegieAction.PregnancyCheck)
            .Add(p => p.Urgency, Urgency.Info)
            .Add(p => p.Outcomes, ResultOptions.For(RegieAction.PregnancyCheck))
            .Add(p => p.Outcome, ResultOutcome.Negative)
            .Add(p => p.OnOutcome, (ResultOutcome outcome) => chosen = outcome));

        var buttons = cut.FindAll(".cow-card__outcome");
        Assert.Equal(["Positif", "Négatif", "Douteux"], buttons.Select(button => button.TextContent.Trim()));
        Assert.Equal(["false", "true", "false"], buttons.Select(button => button.GetAttribute("aria-pressed")));
        Assert.Empty(cut.FindAll(".cow-card__primary"));

        buttons[0].Click();

        Assert.Equal(ResultOutcome.Positive, chosen);
    }

    [Fact]
    public void Note_modifiable_avec_libelle_et_compteur()
    {
        string? saved = null;
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 2))
            .Add(p => p.Action, RegieAction.DryOff)
            .Add(p => p.Urgency, Urgency.Ok)
            .Add(p => p.Note, "Boiterie")
            .Add(p => p.OnNoteChanged, (string note) => saved = note));

        Assert.Equal(cut.Find("textarea").Id, cut.Find("label.cow-card__note-label").GetAttribute("for"));
        Assert.Equal("2000", cut.Find("textarea").GetAttribute("maxlength"));
        Assert.Equal("8/2000", cut.Find(".cow-card__note-count").TextContent);

        cut.Find("textarea").Change("Boiterie AP gauche,\nrevoir dans 15 j");

        Assert.Equal("Boiterie AP gauche,\nrevoir dans 15 j", saved);
    }

    [Fact]
    public void Sans_action_branchee_aucun_bouton_inerte_n_est_affiche()
    {
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 2))
            .Add(p => p.Action, RegieAction.DryOff)
            .Add(p => p.Urgency, Urgency.Ok));

        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Indicateurs_sont_rendus_en_liste_de_definitions()
    {
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 2))
            .Add(p => p.Action, RegieAction.DryOff)
            .Add(p => p.Urgency, Urgency.Ok)
            .Add(p => p.Metrics, [new CowCardMetric("Dernier CCS", "112 k cellules/mL"), new CowCardMetric("Lactation", "305 j")]));

        Assert.Equal(["Dernier CCS", "Lactation"], cut.FindAll("dt").Select(dt => dt.TextContent));
        Assert.Equal(["112 k cellules/mL", "305 j"], cut.FindAll("dd").Select(dd => dd.TextContent));
    }

    [Fact]
    public void Identifiant_national_et_statut_du_motif_sont_affiches()
    {
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 3))
            .Add(p => p.Action, RegieAction.CalvingSoon)
            .Add(p => p.Urgency, Urgency.Warning)
            .Add(p => p.NationalId, "CA QC 0894 4812")
            .Add(p => p.Statement, "Colostrum prêt."));

        Assert.Equal("CA QC 0894 4812", cut.Find(".cow-card__national").TextContent);
        Assert.Equal("Statut : Colostrum prêt.", cut.Find(".cow-card__statement").TextContent);
    }

    [Fact]
    public void Sans_identifiant_ni_statut_rien_n_est_affiche()
    {
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 3))
            .Add(p => p.Action, RegieAction.DryOff)
            .Add(p => p.Urgency, Urgency.Ok));

        Assert.Empty(cut.FindAll(".cow-card__national"));
        Assert.Empty(cut.FindAll(".cow-card__statement"));
    }

    [Fact]
    public void Un_indicateur_en_alerte_est_signale()
    {
        var cut = Render<CowCard>(parameters => parameters
            .Add(p => p.Cow, DemoCow(lactation: 2))
            .Add(p => p.Action, RegieAction.HighScc)
            .Add(p => p.Urgency, Urgency.Urgent)
            .Add(p => p.Metrics, [new CowCardMetric("Cellules", "850k sp/mL", Alert: true), new CowCardMetric("Conductivité", "6,8 mS/cm")]));

        Assert.Single(cut.FindAll(".cow-card__metric--alert"));
    }
}
