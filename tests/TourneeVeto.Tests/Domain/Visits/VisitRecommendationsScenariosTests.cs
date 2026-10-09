using Microsoft.Extensions.Time.Testing;
using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Tests.Domain.Visits;

/// <summary>Scénarios Gherkin des stories 9.1 et 9.2 (bornes et erreurs comprises).</summary>
public class VisitRecommendationsScenariosTests
{
    private static readonly Guid VisitId = Guid.Parse("8f0c4a52-0c7e-4b8e-9d6a-3c1f2e5b7a90");
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 8, 14, 30, 0, TimeSpan.Zero));

    private static Guid IdOf(int n) => new(n, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    private VisitRecommendations Two()
    {
        var r = VisitRecommendations.Empty(VisitId, _time.GetUtcNow());
        r.TryAdd("Première", IdOf(1), _time.GetUtcNow(), out r);
        r.TryAdd("Deuxième", IdOf(2), _time.GetUtcNow(), out r);
        return r;
    }

    [Fact]
    public void Ajout_d_une_3e_recommandation_les_3_sont_numerotees_dans_l_ordre_de_saisie()
    {
        _time.Advance(TimeSpan.FromMinutes(1));

        Assert.True(Two().TryAdd("Troisième", IdOf(3), _time.GetUtcNow(), out var result));

        Assert.Equal([(1, "Première"), (2, "Deuxième"), (3, "Troisième")],
            result.Numbered().Select(e => (e.Number, e.Recommendation.Text)));
        Assert.Equal([IdOf(1), IdOf(2), IdOf(3)], result.Items.Select(i => i.Id));
        Assert.Equal(_time.GetUtcNow(), result.UpdatedAt);
    }

    [Fact]
    public void Texte_avec_retour_a_la_ligne_et_accents_reapparait_a_l_identique()
    {
        const string text = "Tarir 4521 et 4533 cette semaine\nDeuxième ligne é à ç";

        VisitRecommendations.Empty(VisitId, _time.GetUtcNow()).TryAdd(text, IdOf(1), _time.GetUtcNow(), out var result);

        Assert.Equal(text, Assert.Single(result.Items).Text);
    }

    [Fact]
    public void Suppression_d_un_id_connu_laisse_les_autres_intactes_et_dans_l_ordre()
    {
        var result = Two().Remove(IdOf(1), _time.GetUtcNow());

        Assert.Equal([new Recommendation(IdOf(2), "Deuxième")], result.Items);
        Assert.Equal([1], result.Numbered().Select(e => e.Number));
    }

    [Fact]
    public void Suppression_de_la_derniere_recommandation_donne_une_liste_vide()
    {
        var result = Two().Remove(IdOf(1), _time.GetUtcNow()).Remove(IdOf(2), _time.GetUtcNow());

        Assert.Empty(result.Items);
        Assert.Empty(result.Numbered());
    }

    [Fact]
    public void Suppression_d_un_id_inconnu_ne_change_ni_la_liste_ni_la_date()
    {
        var start = Two();
        _time.Advance(TimeSpan.FromHours(1));

        var result = start.Remove(IdOf(99), _time.GetUtcNow());

        Assert.Same(start, result);
        Assert.NotEqual(_time.GetUtcNow(), result.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("\n\t \r\n")]
    public void Ajout_vide_ou_espaces_seuls_sur_une_liste_existante_ne_cree_rien(string? text)
    {
        var start = Two();

        Assert.False(start.TryAdd(text, IdOf(3), _time.GetUtcNow(), out var result));

        Assert.Same(start, result);
        Assert.Equal(2, result.Items.Count);
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void Ajout_limite_de_1000_caracteres(int length, bool accepted)
    {
        var start = Two();

        Assert.Equal(accepted, start.TryAdd(new string('é', length), IdOf(3), _time.GetUtcNow(), out var result));

        Assert.Equal(accepted ? 3 : 2, result.Items.Count);
    }

    [Fact]
    public void La_limite_s_applique_apres_rognage()
    {
        var text = "  " + new string('a', 1000) + "\n ";

        Assert.True(VisitRecommendations.TryNormalize(text, out var normalized));
        Assert.Equal(1000, normalized.Length);
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void Modification_limite_de_1000_caracteres(int length, bool accepted)
    {
        var start = Two();

        Assert.Equal(accepted, start.TryUpdate(IdOf(2), new string('b', length), _time.GetUtcNow(), out var result));

        if (!accepted)
        {
            Assert.Same(start, result);
        }
        else
        {
            Assert.Equal(length, result.Items[1].Text.Length);
        }
    }

    [Fact]
    public void Modification_garde_la_place_et_les_identifiants_des_autres()
    {
        var start = Two();
        start.TryAdd("Troisième", IdOf(3), _time.GetUtcNow(), out start);

        Assert.True(start.TryUpdate(IdOf(1), "Première\nmodifiée é", _time.GetUtcNow(), out var result));

        Assert.Equal([IdOf(1), IdOf(2), IdOf(3)], result.Items.Select(i => i.Id));
        Assert.Equal(["Première\nmodifiée é", "Deuxième", "Troisième"], result.Items.Select(i => i.Text));
    }

    [Fact]
    public void Les_listes_renvoyees_ne_modifient_jamais_la_liste_d_origine()
    {
        var start = Two();
        var snapshot = start.Items.ToArray();

        start.TryUpdate(IdOf(1), "X", _time.GetUtcNow(), out _);
        start.TryAdd("Y", IdOf(3), _time.GetUtcNow(), out _);
        start.Remove(IdOf(2), _time.GetUtcNow());

        Assert.Equal(snapshot, start.Items);
    }
}
