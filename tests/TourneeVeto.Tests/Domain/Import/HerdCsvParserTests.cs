using System.Globalization;
using TourneeVeto.Domain.Herd;
using static TourneeVeto.Tests.Domain.Import.ImportFixtures;

namespace TourneeVeto.Tests.Domain.Import;

/// <summary>Story #49 : analyse du CSV de contrôle laitier (format décidé dans l'issue), ligne en erreur sans bloquer les autres.</summary>
public class HerdCsvParserTests
{
    private const string Header = "numero;nom;date_naissance;lactation;date_velage;date_ia;statut;ccs_milliers";

    [Fact]
    public void Fichier_valide_de_60_lignes_donne_60_vaches()
    {
        var result = Parse("troupeau-fictif.csv");

        Assert.Empty(result.Errors);
        Assert.Equal(60, result.Cows.Count);
        Assert.All(result.Cows, cow => Assert.Equal(FarmId, cow.FarmId));
        var first = result.Cows[0];
        Assert.Equal(
            new Cow("5001", FarmId, "Abeille", new DateOnly(2024, 3, 3), 1, new DateOnly(2026, 3, 29), null, ReproStatus.Open, 371),
            first);
    }

    [Fact]
    public void Date_de_velage_invalide_ligne_12_est_signalee_et_les_autres_lignes_restent_importables()
    {
        var result = Parse("troupeau-corrompu.csv");

        var error = Assert.Single(result.Errors);
        Assert.Equal("ligne 12, date_velage : date invalide", error.ToString());
        Assert.Equal(59, result.Cows.Count);
        Assert.DoesNotContain(result.Cows, cow => cow.Id == "5011");
    }

    [Fact]
    public void Colonnes_dans_un_autre_ordre_et_separateur_virgule()
    {
        var result = ParseText("nom,numero,statut,lactation,date_naissance,date_velage,date_ia,ccs_milliers\nBella,4812,gestante,3,2021-02-14,2026-01-05,2026-03-20,120\n");

        var cow = Assert.Single(result.Cows);
        Assert.Equal(("4812", "Bella", ReproStatus.Pregnant, 3, 120), (cow.Id, cow.Name, cow.Status, cow.Lactation, cow.LastSccThousands));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Dates_lues_en_fr_CA_quelle_que_soit_la_culture_de_l_appareil()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            var result = ParseText($"{Header}\n4812;Bella;14/02/2021;3;08/10/2026;;vide;\n");

            var cow = Assert.Single(result.Cows);
            Assert.Equal(new DateOnly(2021, 2, 14), cow.BornOn);
            Assert.Equal(new DateOnly(2026, 10, 8), cow.LastCalving);   // 8 octobre, et non 10 août
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Champs_facultatifs_vides_pour_une_genisse()
    {
        var result = ParseText($"{Header}\n7001;Étoile;2025-01-10;0;;;;\n");

        var heifer = Assert.Single(result.Cows);
        Assert.True(heifer.IsHeifer);
        Assert.Equal((null, null, ReproStatus.Open, null), (heifer.LastCalving, heifer.LastInsemination, heifer.Status, heifer.LastSccThousands));
    }

    [Theory]
    [InlineData("vide", ReproStatus.Open)]
    [InlineData("inséminée", ReproStatus.Bred)]
    [InlineData("inseminee", ReproStatus.Bred)]
    [InlineData("Gestante", ReproStatus.Pregnant)]
    [InlineData(" tarie ", ReproStatus.Dry)]
    public void Statut_avec_ou_sans_accents_ni_majuscules(string status, ReproStatus expected)
    {
        var result = ParseText($"{Header}\n4812;Bella;2021-02-14;3;2026-01-05;2026-03-20;{status};120\n");

        Assert.Equal(expected, Assert.Single(result.Cows).Status);
    }

    [Theory]
    [InlineData("4812;Bella;2021-02-14;3;2026-01-05;;malade;120", "ligne 2, statut : statut inconnu « malade »")]
    [InlineData("4812;Bella;2021-02-14;trois;2026-01-05;;vide;120", "ligne 2, lactation : nombre invalide")]
    [InlineData("4812;Bella;2021-02-14;-1;2026-01-05;;vide;120", "ligne 2, lactation : nombre invalide")]
    [InlineData("4812;Bella;2021-02-14;3;2026-01-05;;vide;1,5", "ligne 2, ccs_milliers : nombre invalide")]
    [InlineData("4812;;2021-02-14;3;2026-01-05;;vide;120", "ligne 2, nom : valeur obligatoire")]
    [InlineData(";Bella;2021-02-14;3;2026-01-05;;vide;120", "ligne 2, numero : valeur obligatoire")]
    [InlineData("4812;Bella;;3;2026-01-05;;vide;120", "ligne 2, date_naissance : valeur obligatoire")]
    [InlineData("4812;Bella;2021-02-14;3;2026-01-05;20/13/2026;vide;120", "ligne 2, date_ia : date invalide")]
    [InlineData("4812;Bella;2021-02-14;3", "ligne 2 : 4 colonnes au lieu de 8")]
    public void Valeur_invalide_signale_la_ligne_et_la_colonne(string line, string expected)
    {
        var result = ParseText($"{Header}\n{line}\n");

        Assert.Empty(result.Cows);
        Assert.Equal(expected, Assert.Single(result.Errors).ToString());
    }

    [Fact]
    public void Numero_en_double_est_signale_sur_la_seconde_ligne()
    {
        var result = ParseText($"{Header}\n4812;Bella;2021-02-14;3;;;vide;\n4812;Perle;2022-02-14;2;;;vide;\n");

        Assert.Equal("Bella", Assert.Single(result.Cows).Name);
        Assert.Equal("ligne 3, numero : numéro 4812 déjà présent ligne 2", Assert.Single(result.Errors).ToString());
    }

    [Fact]
    public void Colonne_obligatoire_absente_de_l_en_tete_bloque_tout_l_import()
    {
        var result = ParseText("numero;nom;date_naissance;lactation;date_ia;statut;ccs_milliers\n4812;Bella;2021-02-14;3;;vide;\n");

        Assert.Empty(result.Cows);
        Assert.Equal("ligne 1, en-tête : colonne date_velage manquante", Assert.Single(result.Errors).ToString());
    }

    [Fact]
    public void Fichier_vide_est_signale()
    {
        var result = ParseText(string.Empty);

        Assert.Empty(result.Cows);
        Assert.Equal("ligne 1, en-tête : fichier vide", Assert.Single(result.Errors).ToString());
    }

    [Fact]
    public void Lignes_vides_ignorees_et_champs_entre_guillemets_acceptes()
    {
        var result = ParseText($"{Header}\r\n\r\n4812;\"Bella; la grande\";2021-02-14;3;;;vide;\r\n\r\n");

        Assert.Equal("Bella; la grande", Assert.Single(result.Cows).Name);
        Assert.Empty(result.Errors);
    }
}
