using TourneeVeto.Ui.Components;

namespace TourneeVeto.Tests.Ui;

/// <summary>Lecture de l'écran et de l'élevage courants dans l'adresse.</summary>
public class FarmRouteTests
{
    [Theory]
    [InlineData("", null, null)]
    [InlineData("tournee", null, null)]
    [InlineData("regie", "regie", null)]
    [InlineData("regie/F002", "regie", "F002")]
    [InlineData("regie/F002?q=bella", "regie", "F002")]
    [InlineData("biosecurite/F003", "biosecurite", "F003")]
    [InlineData("rapport/F001", "rapport", "F001")]
    [InlineData("fermes/F001/import", "regie", "F001")]
    public void Lit_l_ecran_et_l_elevage(string path, string? section, string? farmId) =>
        Assert.Equal((section, farmId), FarmRoute.Parse(path));
}
