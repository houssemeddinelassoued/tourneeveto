using System.Text.RegularExpressions;

namespace TourneeVeto.Tests.Ui.Data;

/// <summary>Garde-fous sur le schéma IndexedDB de visitStore.js (la vraie base n'est jamais ouverte dans les tests).</summary>
public partial class VisitStoreSchemaTests
{
    private static string Script => File.ReadAllText(Path.Combine(SolutionRoot.Find(), "src", "TourneeVeto.Ui", "wwwroot", "js", "visitStore.js")).ReplaceLineEndings("\n");

    [Fact]
    public void La_version_de_la_base_est_4()
    {
        Assert.Equal("4", VersionPattern().Match(Script).Groups[1].Value);
    }

    [Fact]
    public void La_migration_vers_la_version_4_ne_fait_que_creer_le_store_des_recommandations()
    {
        var script = Script;
        var start = script.IndexOf("if (oldVersion < 4)", StringComparison.Ordinal);
        Assert.True(start >= 0, "étape de migration < 4 absente");
        var step = script[start..script.IndexOf("\n    }\n", start, StringComparison.Ordinal)];

        Assert.Contains("createObjectStore(\"recommendations\", { keyPath: \"visitId\" })", step);
        Assert.DoesNotContain("deleteObjectStore", step);
        Assert.DoesNotContain(".clear(", step);
    }

    [Fact]
    public void Les_etapes_precedentes_sont_conservees()
    {
        var script = Script;

        foreach (var version in new[] { 1, 2, 3 })
        {
            Assert.Contains($"if (oldVersion < {version})", script);
        }
    }

    [Fact]
    public void Supprimer_une_visite_supprime_aussi_ses_recommandations()
    {
        var script = Script;
        var start = script.IndexOf("export function deleteVisit", StringComparison.Ordinal);
        var body = script[start..script.IndexOf("\n}\n", start, StringComparison.Ordinal)];

        Assert.Contains("\"recommendations\"", body);
    }

    [GeneratedRegex(@"const DB_VERSION = (\d+);")]
    private static partial Regex VersionPattern();
}
