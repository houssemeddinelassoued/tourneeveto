namespace TourneeVeto.E2E;

/// <summary>Test de parcours ignoré quand BASE_URL n'est pas définie, pour que « dotnet test » reste vert sans serveur.</summary>
public sealed class FactIfBaseUrlAttribute : FactAttribute
{
    public FactIfBaseUrlAttribute()
    {
        if (string.IsNullOrWhiteSpace(E2ETest.BaseUrl))
        {
            Skip = E2ETest.SkipReason;
        }
    }
}

/// <summary>Variante Theory de <see cref="FactIfBaseUrlAttribute"/> (une exécution par taille d'écran).</summary>
public sealed class TheoryIfBaseUrlAttribute : TheoryAttribute
{
    public TheoryIfBaseUrlAttribute()
    {
        if (string.IsNullOrWhiteSpace(E2ETest.BaseUrl))
        {
            Skip = E2ETest.SkipReason;
        }
    }
}
