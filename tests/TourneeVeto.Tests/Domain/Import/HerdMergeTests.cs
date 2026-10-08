using TourneeVeto.Domain;
using TourneeVeto.Domain.Herd;
using TourneeVeto.Domain.Import;

namespace TourneeVeto.Tests.Domain.Import;

/// <summary>Story #49 : « fusionner » met à jour une vache existante sans doublon ; « remplacer » ne garde que le fichier.</summary>
public class HerdMergeTests
{
    private static readonly Cow Template = DemoData.Generate(new DateOnly(2026, 10, 8), seed: 42).Cows[0] with { FarmId = "F001" };

    private static Cow Cow(string id, int scc = 100) => Template with { Id = id, Name = $"Vache {id}", LastSccThousands = scc };

    [Fact]
    public void Fusionner_met_a_jour_les_vaches_existantes_sans_doublon()
    {
        Cow[] existing = [Cow("A"), Cow("B")];
        Cow[] imported = [Cow("A", scc: 450), Cow("C")];

        var result = HerdMerge.Merge(existing, imported, ImportMode.Merge);

        Assert.Equal(["A", "B", "C"], result.Herd.Select(cow => cow.Id));
        Assert.Equal(450, result.Herd.Single(cow => cow.Id == "A").LastSccThousands);
        Assert.Equal((1, 1, 0, 0), (result.Added, result.Updated, result.Unchanged, result.Removed));
    }

    [Fact]
    public void Fusionner_une_vache_identique_ne_compte_pas_comme_une_mise_a_jour()
    {
        var result = HerdMerge.Merge([Cow("A")], [Cow("A")], ImportMode.Merge);

        Assert.Equal((0, 0, 1, 0), (result.Added, result.Updated, result.Unchanged, result.Removed));
    }

    [Fact]
    public void Remplacer_ne_garde_que_les_vaches_du_fichier()
    {
        Cow[] existing = [Cow("A"), Cow("B")];
        Cow[] imported = [Cow("A", scc: 450), Cow("C")];

        var result = HerdMerge.Merge(existing, imported, ImportMode.Replace);

        Assert.Equal(["A", "C"], result.Herd.Select(cow => cow.Id));
        Assert.Equal((1, 1, 0, 1), (result.Added, result.Updated, result.Unchanged, result.Removed));
    }

    [Fact]
    public void Troupeau_resultant_est_trie_par_numero()
    {
        var result = HerdMerge.Merge([Cow("5003"), Cow("5001")], [Cow("5002")], ImportMode.Merge);

        Assert.Equal(["5001", "5002", "5003"], result.Herd.Select(cow => cow.Id));
    }

    [Fact]
    public void Vaches_de_fermes_differentes_sont_refusees()
    {
        Assert.Throws<ArgumentException>(() => HerdMerge.Merge([Cow("A")], [Cow("B") with { FarmId = "F002" }], ImportMode.Merge));
    }
}
