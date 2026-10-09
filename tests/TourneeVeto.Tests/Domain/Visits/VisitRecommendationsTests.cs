using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Tests.Domain.Visits;

public class VisitRecommendationsTests
{
    private static readonly Guid VisitId = Guid.Parse("8f0c4a52-0c7e-4b8e-9d6a-3c1f2e5b7a90");
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 14, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddMinutes(5);

    private static Guid IdOf(int n) => new(n, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    [Fact]
    public void Une_visite_sans_recommandation_est_vide()
    {
        var recommendations = VisitRecommendations.Empty(VisitId, Now);

        Assert.Equal(VisitId, recommendations.VisitId);
        Assert.Empty(recommendations.Items);
        Assert.Equal(Now, recommendations.UpdatedAt);
    }

    [Fact]
    public void Ajout_rogne_les_espaces_aux_extremites_et_date_la_modification()
    {
        var added = VisitRecommendations.Empty(VisitId, Now).TryAdd("  Tarir 4521  ", IdOf(1), Later, out var result);

        Assert.True(added);
        Assert.Equal([new Recommendation(IdOf(1), "Tarir 4521")], result.Items);
        Assert.Equal(Later, result.UpdatedAt);
    }

    [Fact]
    public void Ajout_conserve_accents_et_retours_a_la_ligne_internes()
    {
        const string text = "Tarir 4521 et 4533 cette semaine\nDeuxième ligne : vêlage près de l'été\r\nTroisième";

        VisitRecommendations.Empty(VisitId, Now).TryAdd(text, IdOf(1), Now, out var result);

        Assert.Equal(text, Assert.Single(result.Items).Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t\r\n ")]
    public void Ajout_d_un_texte_vide_ou_d_espaces_ne_cree_rien(string? text)
    {
        var start = VisitRecommendations.Empty(VisitId, Now);

        var added = start.TryAdd(text, IdOf(1), Later, out var result);

        Assert.False(added);
        Assert.Same(start, result);
    }

    [Fact]
    public void Ajout_au_dela_de_la_longueur_maximale_est_refuse()
    {
        var start = VisitRecommendations.Empty(VisitId, Now);

        Assert.True(start.TryAdd(new string('a', VisitRecommendations.MaxTextLength), IdOf(1), Now, out _));
        Assert.False(start.TryAdd(new string('a', VisitRecommendations.MaxTextLength + 1), IdOf(1), Now, out _));
    }

    [Fact]
    public void Les_recommandations_gardent_l_ordre_de_saisie_et_sont_numerotees_de_1_a_n()
    {
        var recommendations = Three();

        Assert.Equal(["A", "B", "C"], recommendations.Items.Select(item => item.Text));
        Assert.Equal([1, 2, 3], recommendations.Numbered().Select(entry => entry.Number));
        Assert.Equal(["A", "B", "C"], recommendations.Numbered().Select(entry => entry.Recommendation.Text));
    }

    [Fact]
    public void Modification_remplace_le_texte_en_gardant_la_place()
    {
        var updated = Three().TryUpdate(IdOf(2), "  B modifié\nsuite ", Later, out var result);

        Assert.True(updated);
        Assert.Equal(["A", "B modifié\nsuite", "C"], result.Items.Select(item => item.Text));
        Assert.Equal(IdOf(2), result.Items[1].Id);
        Assert.Equal(Later, result.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Modification_vers_un_texte_vide_est_refusee(string text)
    {
        var recommendations = Three();

        var updated = recommendations.TryUpdate(IdOf(2), text, Later, out var result);

        Assert.False(updated);
        Assert.Same(recommendations, result);
    }

    [Fact]
    public void Modification_d_une_recommandation_inconnue_est_refusee()
    {
        var recommendations = Three();

        Assert.False(recommendations.TryUpdate(IdOf(99), "X", Later, out var result));
        Assert.Same(recommendations, result);
    }

    [Fact]
    public void Suppression_retire_la_recommandation_et_renumerote()
    {
        var result = Three().Remove(IdOf(2), Later);

        Assert.Equal(["A", "C"], result.Items.Select(item => item.Text));
        Assert.Equal([1, 2], result.Numbered().Select(entry => entry.Number));
        Assert.Equal(Later, result.UpdatedAt);
    }

    [Fact]
    public void Suppression_d_une_recommandation_inconnue_ne_change_rien()
    {
        var recommendations = Three();

        Assert.Same(recommendations, recommendations.Remove(IdOf(99), Later));
    }

    [Fact]
    public void Les_operations_ne_modifient_pas_l_original()
    {
        var recommendations = Three();

        recommendations.Remove(IdOf(1), Later);
        recommendations.TryAdd("D", IdOf(4), Later, out _);

        Assert.Equal(3, recommendations.Items.Count);
        Assert.Equal(Now, recommendations.UpdatedAt);
    }

    private static VisitRecommendations Three()
    {
        var recommendations = VisitRecommendations.Empty(VisitId, Now);
        recommendations.TryAdd("A", IdOf(1), Now, out recommendations);
        recommendations.TryAdd("B", IdOf(2), Now, out recommendations);
        recommendations.TryAdd("C", IdOf(3), Now, out recommendations);
        return recommendations;
    }
}
