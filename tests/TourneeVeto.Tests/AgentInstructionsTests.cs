using System.Text.RegularExpressions;

namespace TourneeVeto.Tests;

/// <summary>
/// Les règles par type de fichier existent en deux exemplaires : .github/instructions (Copilot, applyTo)
/// et .claude/rules (Claude Code, paths). Un import @ chargerait la règle partout : le contenu est donc recopié,
/// et ce test vérifie que les deux exemplaires restent identiques.
/// </summary>
public partial class AgentInstructionsTests
{
    [Fact]
    public void Regles_Claude_et_instructions_Copilot_sont_identiques()
    {
        var root = SolutionRoot.Find();
        var copilotDirectory = Path.Combine(root, ".github", "instructions");
        var claudeDirectory = Path.Combine(root, ".claude", "rules");
        var differences = new List<string>();

        var copilotNames = Directory.EnumerateFiles(copilotDirectory, "*.instructions.md")
            .Select(file => Path.GetFileName(file).Replace(".instructions.md", string.Empty, StringComparison.Ordinal))
            .ToHashSet();
        var claudeNames = Directory.EnumerateFiles(claudeDirectory, "*.md")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .ToHashSet();

        differences.AddRange(copilotNames.Except(claudeNames).Select(name => $"{name} : absent de .claude/rules"));
        differences.AddRange(claudeNames.Except(copilotNames).Select(name => $"{name} : absent de .github/instructions"));

        foreach (var name in copilotNames.Intersect(claudeNames).Order())
        {
            var (copilotFront, copilotBody) = Split(File.ReadAllText(Path.Combine(copilotDirectory, $"{name}.instructions.md")));
            var (claudeFront, claudeBody) = Split(File.ReadAllText(Path.Combine(claudeDirectory, $"{name}.md")));

            var applyTo = ApplyToPattern().Match(copilotFront).Groups[1].Value
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var paths = PathItemPattern().Matches(claudeFront).Select(match => match.Groups[1].Value).ToArray();

            if (!applyTo.SequenceEqual(paths))
            {
                differences.Add($"{name} : applyTo [{string.Join(", ", applyTo)}] ≠ paths [{string.Join(", ", paths)}]");
            }

            if (copilotBody != HtmlCommentPattern().Replace(claudeBody, string.Empty).Trim())
            {
                differences.Add($"{name} : contenu différent");
            }
        }

        // En cas d'échec, xUnit affiche la liste des écarts.
        Assert.Empty(differences);
    }

    private static (string FrontMatter, string Body) Split(string markdown)
    {
        var match = FrontMatterPattern().Match(markdown.ReplaceLineEndings("\n"));
        Assert.True(match.Success, "En-tête YAML (---) manquant.");
        return (match.Groups[1].Value, match.Groups[2].Value.Trim());
    }

    [GeneratedRegex(@"\A---\n(.*?)\n---\n(.*)\z", RegexOptions.Singleline)]
    private static partial Regex FrontMatterPattern();

    [GeneratedRegex("applyTo:\\s*\"([^\"]*)\"")]
    private static partial Regex ApplyToPattern();

    [GeneratedRegex("^\\s*-\\s*\"([^\"]*)\"\\s*$", RegexOptions.Multiline)]
    private static partial Regex PathItemPattern();

    [GeneratedRegex("<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex HtmlCommentPattern();
}
