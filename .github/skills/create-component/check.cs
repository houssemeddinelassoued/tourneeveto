// Vérifie un composant ou une page Razor de TournéeVéto créé avec le skill create-component.
// Usage (depuis la racine du dépôt) : dotnet run .github/skills/create-component/check.cs -- <NomEnPascalCase>
// Code de sortie : 0 si tout est conforme, 1 si des écarts sont trouvés, 2 si l'appel est invalide.

using System.Text.RegularExpressions;

if (args.Length != 1 || !Regex.IsMatch(args[0], "^[A-Z][A-Za-z0-9]*$"))
{
    Console.Error.WriteLine("Usage : dotnet run .github/skills/create-component/check.cs -- <NomEnPascalCase>");
    return 2;
}

var name = args[0];
var root = FindRepositoryRoot();
var ui = Path.Combine(root, "src", "TourneeVeto.Ui");
var componentPath = Path.Combine(ui, "Components", $"{name}.razor");
var pagePath = Path.Combine(ui, "Pages", $"{name}.razor");
var isPage = File.Exists(pagePath);
var razorPath = isPage ? pagePath : componentPath;
var cssPath = $"{razorPath}.css";
var testPath = Path.Combine(root, "tests", "TourneeVeto.Tests", "Ui", $"{name}Tests.cs");
var tokensPath = Path.Combine(ui, "wwwroot", "tokens.css");
var problems = new List<string>();

if (File.Exists(componentPath) && File.Exists(pagePath))
{
    problems.Add($"{name}.razor existe à la fois dans Components/ et dans Pages/.");
}

foreach (var (path, label) in new[] { (razorPath, "Composant"), (cssPath, "CSS isolé"), (testPath, "Test bUnit") })
{
    if (!File.Exists(path))
    {
        problems.Add($"{label} manquant : {Relative(path)}");
    }
    else if (File.ReadAllText(path).Contains("__NOM__", StringComparison.Ordinal))
    {
        problems.Add($"Marqueur __NOM__ non remplacé : {Relative(path)}");
    }
}

if (File.Exists(razorPath))
{
    CheckRazor(File.ReadAllText(razorPath));
}

if (File.Exists(cssPath))
{
    CheckCss(File.ReadAllText(cssPath));
}

if (File.Exists(testPath))
{
    CheckTest(File.ReadAllText(testPath));
}

if (problems.Count == 0)
{
    Console.WriteLine($"OK : {name} ({(isPage ? "page" : "composant")}) respecte les règles du skill create-component.");
    Console.WriteLine($"Étape suivante : dotnet test --filter {name}");
    return 0;
}

Console.WriteLine($"{problems.Count} écart(s) pour {name} :");
problems.ForEach(problem => Console.WriteLine($"  - {problem}"));
return 1;

void CheckRazor(string razor)
{
    var hasPageDirective = Regex.IsMatch(razor, @"^\s*@page\b", RegexOptions.Multiline);
    if (isPage && !hasPageDirective)
    {
        problems.Add("Une page de Pages/ doit déclarer @page.");
    }

    if (!isPage && hasPageDirective)
    {
        problems.Add("@page dans Components/ : déplacer le fichier dans Pages/.");
    }

    if (razor.Contains("MarkupString", StringComparison.Ordinal))
    {
        problems.Add("MarkupString interdit (razor.instructions.md).");
    }

    if (Regex.IsMatch(razor, @"DateTime\.(Now|Today|UtcNow)"))
    {
        problems.Add("DateTime.Now/Today interdit : recevoir la date du domaine ou d'un TimeProvider injecté.");
    }

    if (Regex.IsMatch(razor, @"\sstyle\s*=|<style\b", RegexOptions.IgnoreCase))
    {
        problems.Add("Style en ligne : mettre les styles dans le .razor.css isolé.");
    }

    if (Regex.IsMatch(razor, @"@inject\s+(HttpClient|IJSRuntime)\b"))
    {
        problems.Add("HttpClient ou IJSRuntime injecté dans le composant : passer par un service (ex. IVisitRepository).");
    }
}

void CheckCss(string rawCss)
{
    var css = Regex.Replace(rawCss, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
    var definedTokens = File.Exists(tokensPath)
        ? CustomPropertyDefinitions(File.ReadAllText(tokensPath))
        : [];
    var localProperties = CustomPropertyDefinitions(css);

    foreach (Match usage in Regex.Matches(css, @"var\(\s*(--[\w-]+)"))
    {
        var token = usage.Groups[1].Value;
        if (!definedTokens.Contains(token) && !localProperties.Contains(token))
        {
            problems.Add($"Variable inconnue {token} : absente de tokens.css.");
        }
    }

    foreach (Match declaration in Regex.Matches(css, @"(?<property>[a-z-]+)\s*:\s*(?<value>[^;{}]+);"))
    {
        var property = declaration.Groups["property"].Value;
        var value = declaration.Groups["value"].Value.Trim();
        if (property.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var isColor = Regex.IsMatch(property, "^(color|background(-color)?|border(-[a-z]+)*-color|outline-color|fill|stroke|caret-color|accent-color)$");
        if (Regex.IsMatch(value, @"#[0-9a-fA-F]{3,8}\b|\b(rgba?|hsla?|oklch|lab)\("))
        {
            problems.Add($"Couleur en dur ({property}: {value}) : utiliser une variable --color-*.");
        }
        else if (isColor && !value.Contains("var(--", StringComparison.Ordinal)
            && !Regex.IsMatch(value, "^(none|transparent|currentColor|inherit|initial|unset)$", RegexOptions.IgnoreCase))
        {
            problems.Add($"Couleur hors jetons ({property}: {value}) : utiliser une variable --color-*.");
        }

        var isSpacingOrRadius = Regex.IsMatch(property, "^(margin|padding|gap|row-gap|column-gap|border-radius)(-[a-z]+)*$");
        var withoutTokens = Regex.Replace(value, @"var\([^)]*\)", string.Empty);
        if (isSpacingOrRadius && Regex.IsMatch(withoutTokens, @"(?<![\w-])\d*\.?\d+(px|rem|em|%)"))
        {
            problems.Add($"Espacement ou rayon en dur ({property}: {value}) : utiliser --space-* ou --radius-*.");
        }
    }
}

void CheckTest(string test)
{
    if (!test.Contains("BunitContext", StringComparison.Ordinal))
    {
        problems.Add("Le test doit hériter de BunitContext (tests.instructions.md).");
    }

    if (!test.Contains($"Render<{name}>", StringComparison.Ordinal))
    {
        problems.Add($"Le test ne rend pas le composant (Render<{name}> introuvable).");
    }

    if (!Regex.IsMatch(test, @"namespace\s+TourneeVeto\.Tests\.Ui\b"))
    {
        problems.Add("Espace de noms attendu pour le test : TourneeVeto.Tests.Ui.");
    }

    if (Regex.IsMatch(test, @"DateTime\.(Now|Today|UtcNow)"))
    {
        problems.Add("DateTime.Now/Today interdit dans un test : figer la date (FakeTimeProvider ou DateOnly fixe).");
    }
}

static HashSet<string> CustomPropertyDefinitions(string css) =>
    Regex.Matches(css, @"(--[\w-]+)\s*:").Select(match => match.Groups[1].Value).ToHashSet();

static string FindRepositoryRoot()
{
    for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory is not null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "TourneeVeto.slnx")))
        {
            return directory.FullName;
        }
    }

    throw new InvalidOperationException("TourneeVeto.slnx introuvable : lancer le script depuis le dépôt TournéeVéto.");
}

string Relative(string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
