using TourneeVeto.Domain.Visits;

namespace TourneeVeto.Tests.Domain.Visits;

/// <summary>Limites du POC : nombre maximal de recommandations et caractères de contrôle retirés.</summary>
public class VisitRecommendationsLimitsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 14, 30, 0, TimeSpan.Zero);

    private static VisitRecommendations Filled(int count)
    {
        var recommendations = VisitRecommendations.Empty(Guid.NewGuid(), Now);
        for (var n = 1; n <= count; n++)
        {
            Assert.True(recommendations.TryAdd($"R{n}", new Guid(n, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), Now, out recommendations));
        }

        return recommendations;
    }

    [Fact]
    public void Le_nombre_maximal_de_recommandations_est_20()
    {
        Assert.Equal(20, VisitRecommendations.MaxItems);
    }

    [Fact]
    public void Ajout_au_dela_du_nombre_maximal_est_refuse_sans_rien_changer()
    {
        var full = Filled(VisitRecommendations.MaxItems);

        var added = full.TryAdd("De trop", Guid.NewGuid(), Now, out var result);

        Assert.False(added);
        Assert.Same(full, result);
    }

    [Fact]
    public void Modification_reste_possible_quand_la_liste_est_pleine()
    {
        var full = Filled(VisitRecommendations.MaxItems);

        Assert.True(full.TryUpdate(full.Items[0].Id, "Modifiée", Now, out _));
    }

    [Fact]
    public void Les_caracteres_de_controle_sont_retires_sauf_retour_a_la_ligne_et_tabulation()
    {
        var ok = VisitRecommendations.TryNormalize("A\u0000B\u0007C\u001bD\u007fE\u0085F\nG\tH\r\nI", out var normalized);

        Assert.True(ok);
        Assert.Equal("ABCDEF\nG\tH\r\nI", normalized);
    }

    [Fact]
    public void Un_texte_fait_uniquement_de_caracteres_de_controle_est_refuse()
    {
        Assert.False(VisitRecommendations.TryNormalize("\u0000\u0007 \u001b", out _));
    }

    [Theory]
    [InlineData("A‮B")]
    [InlineData("A​B‏C⁦D⁩E‪F")]
    public void Les_caracteres_de_format_unicode_sont_retires(string text)
    {
        Assert.True(VisitRecommendations.TryNormalize(text, out var normalized));
        Assert.DoesNotContain(normalized, c => char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format);
        Assert.StartsWith("A", normalized);
    }

    [Fact]
    public void Un_texte_trop_long_est_signale_comme_tel()
    {
        Assert.True(VisitRecommendations.IsTooLong(new string('a', VisitRecommendations.MaxTextLength + 1)));
        Assert.False(VisitRecommendations.IsTooLong(new string('a', VisitRecommendations.MaxTextLength)));
        Assert.False(VisitRecommendations.IsTooLong("  "));
    }

    [Fact]
    public void Assainissement_retire_les_textes_invalides_renormalise_et_borne_le_nombre()
    {
        var items = new List<Recommendation>
        {
            new(Guid.NewGuid(), "  ok‮  "),
            new(Guid.NewGuid(), "   "),
            new(Guid.NewGuid(), new string('x', VisitRecommendations.MaxTextLength + 1)),
            new(Guid.NewGuid(), null!),
        };
        items.AddRange(Enumerable.Range(1, 30).Select(n => new Recommendation(Guid.NewGuid(), $"R{n}")));

        var clean = VisitRecommendations.Sanitize(new VisitRecommendations(Guid.NewGuid(), items, Now));

        Assert.Equal(VisitRecommendations.MaxItems, clean.Items.Count);
        Assert.Equal("ok", clean.Items[0].Text);
        Assert.Equal("R1", clean.Items[1].Text);
    }

    [Fact]
    public void Assainissement_d_une_liste_absente_donne_une_liste_vide()
    {
        var clean = VisitRecommendations.Sanitize(new VisitRecommendations(Guid.NewGuid(), null!, Now));

        Assert.Empty(clean.Items);
    }

    [Fact]
    public void Les_caracteres_de_format_hors_plan_de_base_sont_retires()
    {
        Assert.True(VisitRecommendations.TryNormalize("A\U000E0041B\U000E0001C", out var normalized));
        Assert.Equal("ABC", normalized);
    }

    [Fact]
    public void Les_emoji_hors_plan_de_base_sont_conserves()
    {
        Assert.True(VisitRecommendations.TryNormalize("Vache \U0001F404", out var normalized));
        Assert.Equal("Vache \U0001F404", normalized);
    }

    [Fact]
    public void Assainissement_ecarte_les_identifiants_vides_et_en_double()
    {
        var id = Guid.NewGuid();
        var items = new List<Recommendation> { new(Guid.Empty, "vide"), new(id, "premier"), new(id, "doublon"), new(Guid.NewGuid(), "autre") };

        var clean = VisitRecommendations.Sanitize(new VisitRecommendations(Guid.NewGuid(), items, Now));

        Assert.Equal(["premier", "autre"], clean.Items.Select(item => item.Text));
    }
}
