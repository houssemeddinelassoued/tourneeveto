using TourneeVeto.Domain.Herd;

namespace TourneeVeto.Tests;

/// <summary>Règles de dépendance de l'ADR 0002 (docs/adr/0002-structure-solution.md).</summary>
public class ArchitectureTests
{
    private static readonly string[] ForbiddenDomainDependencies = ["Microsoft.AspNetCore", "Microsoft.JSInterop"];

    [Fact]
    public void Domain_ne_depend_pas_de_l_interface()
    {
        var forbidden = typeof(Cow).Assembly.GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .Where(name => ForbiddenDomainDependencies.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Hote_Web_ne_contient_aucune_page_routable()
    {
        var webProject = Path.Combine(SolutionRoot.Find(), "src", "TourneeVeto.Web");

        var routablePages = Directory.EnumerateFiles(webProject, "*.razor", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(webProject, file))
            .Where(file => File.ReadLines(file).Any(line => line.TrimStart().StartsWith("@page", StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(webProject, file))
            .ToList();

        // En cas d'échec, xUnit affiche la liste des fichiers fautifs.
        Assert.Empty(routablePages);
    }

    private static bool IsBuildOutput(string projectDirectory, string file)
    {
        var firstSegment = Path.GetRelativePath(projectDirectory, file).Split(Path.DirectorySeparatorChar)[0];
        return firstSegment is "bin" or "obj";
    }
}
