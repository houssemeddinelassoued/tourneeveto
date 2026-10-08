using TourneeVeto.Domain.Import;

namespace TourneeVeto.Tests.Domain.Import;

/// <summary>Fichiers d'exemple fictifs de tests/TourneeVeto.Tests/Fixtures (copiés dans le dossier de sortie des tests).</summary>
internal static class ImportFixtures
{
    public const string FarmId = "F001";

    public static byte[] Bytes(string fileName) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName));

    public static HerdImportResult Parse(string fileName) =>
        HerdCsvParser.Parse(new StringReader(HerdCsvDecoder.Decode(Bytes(fileName))), FarmId);

    public static HerdImportResult ParseText(string csv) => HerdCsvParser.Parse(new StringReader(csv), FarmId);
}
