namespace TourneeVeto.E2E;

/// <summary>
/// Base des parcours : Chrome installé (pas de téléchargement de navigateurs), un nouveau contexte par test
/// (donc une IndexedDB vide), date figée au 8 octobre 2026 avec l'horloge de Playwright.
/// </summary>
public abstract class E2ETest : IAsyncLifetime
{
    public const string HomeTitle = "Accueil — TournéeVéto";

    public const string SkipReason = "BASE_URL non définie : lancer l'application publiée et définir BASE_URL (ex. http://localhost:5180/).";

    /// <summary>Tailles d'écran : tablette portrait puis poste.</summary>
    public static TheoryData<int, int> Viewports => new() { { 768, 1024 }, { 1280, 800 } };

    public static string? BaseUrl => Environment.GetEnvironmentVariable("BASE_URL");

    private IPlaywright? playwright;
    private IBrowser? browser;

    protected IBrowserContext Context { get; private set; } = null!;

    protected IPage Page { get; private set; } = null!;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (browser is not null)
        {
            await browser.CloseAsync();
        }

        playwright?.Dispose();
    }

    /// <summary>Ouvre la page d'accueil à la taille demandée et attend l'accueil.</summary>
    protected async Task StartAsync(int width, int height)
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Channel = "chrome", Headless = true });
        Context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = "fr-CA",
            TimezoneId = "America/Toronto",
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        // 12 h à Toronto le 8 octobre 2026 : « aujourd'hui » vaut cette date.
        await Context.Clock.SetFixedTimeAsync(new DateTime(2026, 10, 8, 16, 0, 0, DateTimeKind.Utc));
        Page = await Context.NewPageAsync();
        await OpenHomeAsync();
    }

    /// <summary>Navigue depuis la racine (le serveur statique ne sert pas les liens profonds) et attend le titre.</summary>
    protected async Task OpenHomeAsync()
    {
        await Page.GotoAsync(BaseUrl!);
        await Assertions.Expect(Page).ToHaveTitleAsync(HomeTitle);
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Dre Camille Exemple");
        await Assertions.Expect(Page.Locator("main").GetByText(
            new System.Text.RegularExpressions.Regex("^Jeudi 8 octobre 2026$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).First).ToBeVisibleAsync();
    }

    /// <summary>Lien de la navigation principale : libellé court des onglets (&lt; 1024 px) ou long de la barre latérale (poste).</summary>
    protected ILocator NavLink(string shortLabel, string longLabel) =>
        Page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation principale" })
            .GetByRole(AriaRole.Link, new() { NameRegex = new System.Text.RegularExpressions.Regex($"^({System.Text.RegularExpressions.Regex.Escape(shortLabel)}|{System.Text.RegularExpressions.Regex.Escape(longLabel)})$") });
}
