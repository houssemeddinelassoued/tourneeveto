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

    [Fact]
    public void Skills_Claude_et_Copilot_ont_le_meme_nom_et_la_meme_description()
    {
        var root = SolutionRoot.Find();
        var copilot = SkillFrontMatters(Path.Combine(root, ".github", "skills"));
        var claude = SkillFrontMatters(Path.Combine(root, ".claude", "skills"));

        var differences = copilot.Keys.Except(claude.Keys).Select(name => $"{name} : absent de .claude/skills")
            .Concat(claude.Keys.Except(copilot.Keys).Select(name => $"{name} : absent de .github/skills"))
            .Concat(copilot.Keys.Intersect(claude.Keys).Where(name => copilot[name] != claude[name]).Select(name => $"{name} : nom ou description différents"))
            .Concat(copilot.Concat(claude).Where(skill => !skill.Value.Contains($"name: {skill.Key}\n", StringComparison.Ordinal))
                .Select(skill => $"{skill.Key} : le champ name doit être le nom du dossier"))
            .ToList();

        Assert.Empty(differences);
    }

    [Fact]
    public void Fichiers_cites_par_les_skills_Copilot_existent()
    {
        var skillsDirectory = Path.Combine(SolutionRoot.Find(), ".github", "skills");
        var missing = new List<string>();

        foreach (var skill in Directory.EnumerateDirectories(skillsDirectory))
        {
            var instructions = File.ReadAllText(Path.Combine(skill, "SKILL.md"));
            var referenced = SkillFileReferencePattern().Matches(instructions).Select(match => Path.Combine(skill, match.Groups[1].Value, match.Groups[2].Value))
                .Concat(ScriptReferencePattern().Matches(instructions).Select(match => Path.Combine(SolutionRoot.Find(), match.Groups[1].Value)));

            missing.AddRange(referenced.Where(path => !File.Exists(path)).Select(path => Path.GetRelativePath(skillsDirectory, path)));
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void Agents_Claude_et_Copilot_ont_le_meme_nom_la_meme_description_et_les_memes_instructions()
    {
        var root = SolutionRoot.Find();
        var copilot = Directory.EnumerateFiles(Path.Combine(root, ".github", "agents"), "*.agent.md")
            .ToDictionary(file => Path.GetFileName(file).Replace(".agent.md", string.Empty, StringComparison.Ordinal), File.ReadAllText);
        var claude = Directory.EnumerateFiles(Path.Combine(root, ".claude", "agents"), "*.md")
            .ToDictionary(file => Path.GetFileNameWithoutExtension(file), File.ReadAllText);

        var differences = copilot.Keys.Except(claude.Keys).Select(name => $"{name} : absent de .claude/agents")
            .Concat(claude.Keys.Except(copilot.Keys).Select(name => $"{name} : absent de .github/agents"))
            .ToList();
        foreach (var name in copilot.Keys.Intersect(claude.Keys).Order())
        {
            var (copilotFront, copilotBody) = Split(copilot[name]);
            var (claudeFront, claudeBody) = Split(claude[name]);
            string Field(string front, string field) => Regex.Match(front, $"^{field}:(.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();

            if (Field(copilotFront, "name") != name || Field(claudeFront, "name") != name)
            {
                differences.Add($"{name} : le champ name doit être le nom du fichier");
            }

            if (Field(copilotFront, "description") != Field(claudeFront, "description"))
            {
                differences.Add($"{name} : description différente");
            }

            if (copilotBody != claudeBody)
            {
                differences.Add($"{name} : instructions différentes");
            }
        }

        Assert.Empty(differences);
    }

    private static Dictionary<string, string> SkillFrontMatters(string skillsDirectory) =>
        Directory.EnumerateDirectories(skillsDirectory).ToDictionary(
            directory => Path.GetFileName(directory),
            directory => Split(File.ReadAllText(Path.Combine(directory, "SKILL.md"))).FrontMatter + "\n");

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

    [GeneratedRegex(@"(template|exemples)/([\w.-]+\.\w+)")]
    private static partial Regex SkillFileReferencePattern();

    [GeneratedRegex(@"dotnet run (\S+\.cs)")]
    private static partial Regex ScriptReferencePattern();
}
