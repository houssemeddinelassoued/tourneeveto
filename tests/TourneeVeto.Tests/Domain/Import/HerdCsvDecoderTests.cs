using System.Text;
using TourneeVeto.Domain.Import;
using static TourneeVeto.Tests.Domain.Import.ImportFixtures;

namespace TourneeVeto.Tests.Domain.Import;

/// <summary>Story #49 : fichier en UTF-8 (avec ou sans BOM), repli sur Windows-1252 pour les exports Excel.</summary>
public class HerdCsvDecoderTests
{
    [Fact]
    public void UTF8_sans_BOM_conserve_les_accents()
    {
        var text = HerdCsvDecoder.Decode(Encoding.UTF8.GetBytes("nom\nCâline\nÉtoile\n"));

        Assert.Equal("nom\nCâline\nÉtoile\n", text);
    }

    [Fact]
    public void BOM_UTF8_est_retire()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("numero;nom")];

        Assert.Equal("numero;nom", HerdCsvDecoder.Decode(bytes));
    }

    [Fact]
    public void Fichier_Windows_1252_est_relu_avec_ses_accents()
    {
        var text = HerdCsvDecoder.Decode(Bytes("troupeau-fictif-1252.csv"));

        Assert.Contains("6001;Câline;", text);
        Assert.Contains("6002;Cœur de Lion;", text);   // « œ » n'existe pas en Latin-1 : vrai Windows-1252
        Assert.Contains("inséminée", text);
    }

    [Fact]
    public void Fichier_Windows_1252_donne_les_bonnes_vaches()
    {
        var result = Parse("troupeau-fictif-1252.csv");

        Assert.Empty(result.Errors);
        Assert.Equal(["Câline", "Cœur de Lion", "Étoile", "Pâquerette"], result.Cows.Select(cow => cow.Name));
    }

    [Fact]
    public void Limite_de_taille_est_de_2_Mo()
    {
        Assert.Equal(2 * 1024 * 1024, HerdCsvDecoder.MaxFileBytes);
    }
}
