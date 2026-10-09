namespace TourneeVeto.E2E;

/// <summary>Les trois parcours clés, sur tablette (768×1024) et sur poste (1280×800). Données fictives uniquement.</summary>
public class ParcoursTests : E2ETest
{
    private const string Quarantine = "Les animaux achetés sont-ils isolés au moins 21 jours avant d'entrer dans le troupeau ?";

    private static string FixturePath() => Path.Combine(FindRoot(), "tests", "TourneeVeto.Tests", "Fixtures", "troupeau-fictif.csv");

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TourneeVeto.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("TourneeVeto.slnx introuvable au-dessus du dossier de test.");
    }

    [TheoryIfBaseUrl]
    [MemberData(nameof(Viewports))]
    public async Task Importer_le_CSV_fictif_puis_voir_les_actions_du_jour(int width, int height)
    {
        await StartAsync(width, height);

        await NavLink("Tournée", "Tournée du jour").ClickAsync();
        // Poste : le détail de la visite sélectionnée propose « Démarrer la consultation » ; tablette : « Démarrer la visite ».
        await Page.GetByRole(AriaRole.Link, new() { Name = width >= 1024 ? "Démarrer la consultation" : "Démarrer la visite" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Importer le troupeau (CSV)" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Importer le troupeau" })).ToBeVisibleAsync();

        await Page.GetByLabel("Fichier CSV").SetInputFilesAsync(FixturePath());
        await Assertions.Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "60 vaches prêtes à importer." })).ToBeVisibleAsync();
        await Page.GetByLabel("Remplacer").CheckAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Confirmer l'import de 60 vaches" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Import enregistré" })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Retour à la grille de régie" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Grille de régie" })).ToBeVisibleAsync();

        // 5006 Bouton : inséminée le 6 septembre 2026, donc à 32 jours le 8 octobre, dans la fenêtre du DG.
        await Assertions.Expect(Page.GetByRole(AriaRole.Article, new() { Name = "Bouton" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "DG (" }).ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Article, new() { Name = "Bouton" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Article, new() { Name = "Abeille" })).ToHaveCountAsync(0);
    }

    [TheoryIfBaseUrl]
    [MemberData(nameof(Viewports))]
    public async Task Remplir_le_bilan_de_biosecurite_puis_voir_le_score_et_les_pratiques_prioritaires(int width, int height)
    {
        await StartAsync(width, height);
        await OpenBiosecurityAsync();

        await AnswerAsync(Quarantine, "Non");
        await AnswerAsync("Le statut sanitaire des animaux est-il vérifié avant l'achat ?", "Oui");
        await AnswerAsync("Les animaux qui reviennent d'une exposition ou d'une pension sont-ils isolés ?", "Partiel");

        // Poids 3 + 2 + 2 : Non (0) + Oui (2) + Partiel (1) = 3/7, soit 43 %, avec un point critique à Non : risque élevé.
        await Assertions.Expect(Page.Locator(".section-tab").First).ToContainTextAsync("43 % · Point de vigilance");
        var priorities = Page.GetByRole(AriaRole.Region, new() { Name = "Pratiques prioritaires" });
        await Assertions.Expect(priorities.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        await Assertions.Expect(priorities.GetByRole(AriaRole.Listitem).First).ToContainTextAsync("Les animaux achetés sont-ils isolés");
        await Assertions.Expect(priorities.GetByRole(AriaRole.Listitem).First).ToContainTextAsync("Point critique");
    }

    [TheoryIfBaseUrl]
    [MemberData(nameof(Viewports))]
    public async Task Passer_hors_ligne_puis_rouvrir_la_visite_sans_perte_de_la_saisie(int width, int height)
    {
        await StartAsync(width, height);
        await Assertions.Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "En ligne · prêt hors ligne" })).ToBeVisibleAsync();

        await OpenBiosecurityAsync();
        await AnswerAsync(Quarantine, "Partiel");
        await Assertions.Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Enregistré à" })).ToBeVisibleAsync();

        await Context.SetOfflineAsync(true);
        await OpenHomeAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Hors ligne · prêt" })).ToBeVisibleAsync();
        await OpenBiosecurityAsync();

        await Assertions.Expect(AnswerRadio(Quarantine, "Partiel")).ToBeCheckedAsync();
    }

    private async Task OpenBiosecurityAsync()
    {
        await NavLink("Biosécurité", "Bilan biosécurité").ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Ferme" }).First.ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Bilan biosécurité", Level = 1 })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Rubriques du bilan" })).ToBeVisibleAsync();
    }

    private ILocator AnswerRadio(string question, string answer) =>
        Page.GetByRole(AriaRole.Group, new() { Name = question }).GetByRole(AriaRole.Radio, new() { Name = answer, Exact = true });

    private Task AnswerAsync(string question, string answer) => AnswerRadio(question, answer).CheckAsync();
}
