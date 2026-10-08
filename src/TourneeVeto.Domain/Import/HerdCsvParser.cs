using System.Globalization;
using System.Text;
using TourneeVeto.Domain.Herd;

namespace TourneeVeto.Domain.Import;

/// <summary>
/// Analyse le CSV de contrôle laitier d'un élevage (format de l'issue #49) : en-tête obligatoire, colonnes dans
/// n'importe quel ordre, séparateur « ; » ou « , », dates lues en fr-CA quelle que soit la culture de l'appareil.
/// </summary>
public static class HerdCsvParser
{
    public static IReadOnlyList<string> Columns { get; } =
        ["numero", "nom", "date_naissance", "lactation", "date_velage", "date_ia", "statut", "ccs_milliers"];

    private const string HeaderColumn = "en-tête";

    private static readonly CultureInfo FrenchCanada = CultureInfo.GetCultureInfo("fr-CA");
    private static readonly string[] DateFormats = ["yyyy-MM-dd", "dd/MM/yyyy"];

    /// <summary>Analyse le fichier ; les vaches valides sont rattachées à l'élevage <paramref name="farmId"/>.</summary>
    public static HerdImportResult Parse(TextReader reader, string farmId)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentException.ThrowIfNullOrWhiteSpace(farmId);

        var cows = new List<Cow>();
        var errors = new List<HerdImportError>();

        var headerLine = reader.ReadLine()?.TrimStart('﻿');
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            errors.Add(new HerdImportError(1, HeaderColumn, "fichier vide"));
            return new HerdImportResult(cows, errors);
        }

        var separator = headerLine.Contains(';') ? ';' : ',';
        var header = SplitLine(headerLine, separator).Select(name => name.Trim().ToLowerInvariant()).ToList();
        errors.AddRange(Columns.Where(column => !header.Contains(column))
            .Select(column => new HerdImportError(1, HeaderColumn, $"colonne {column} manquante")));
        if (errors.Count > 0)
        {
            return new HerdImportResult(cows, errors);
        }

        var position = Columns.ToDictionary(column => column, column => header.IndexOf(column));
        var firstLineById = new Dictionary<string, int>(StringComparer.Ordinal);
        var lineNumber = 1;

        for (var line = reader.ReadLine(); line is not null; line = reader.ReadLine())
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitLine(line, separator);
            if (fields.Count != header.Count)
            {
                errors.Add(new HerdImportError(lineNumber, null, $"{fields.Count} colonnes au lieu de {header.Count}"));
                continue;
            }

            var row = new Row(lineNumber, column => fields[position[column]].Trim());
            var cow = row.ToCow(farmId);
            if (row.Errors.Count > 0 || cow is null)
            {
                errors.AddRange(row.Errors);
                continue;
            }

            if (firstLineById.TryGetValue(cow.Id, out var firstLine))
            {
                errors.Add(new HerdImportError(lineNumber, "numero", $"numéro {cow.Id} déjà présent ligne {firstLine}"));
                continue;
            }

            firstLineById[cow.Id] = lineNumber;
            cows.Add(cow);
        }

        return new HerdImportResult(cows, errors);
    }

    // Découpe une ligne en respectant les champs entre guillemets ("" pour un guillemet dans un champ).
    private static List<string> SplitLine(string line, char separator)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (inQuotes)
            {
                if (character != '"')
                {
                    current.Append(character);
                }
                else if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = false;
                }
            }
            else if (character == '"')
            {
                inQuotes = true;
            }
            else if (character == separator)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    /// <summary>Une ligne de données ; chaque lecture de champ ajoute son erreur éventuelle.</summary>
    private sealed class Row(int lineNumber, Func<string, string> valueOf)
    {
        public List<HerdImportError> Errors { get; } = [];

        public Cow? ToCow(string farmId)
        {
            var id = Required("numero");
            var name = Required("nom");
            var bornOn = Date("date_naissance", required: true);
            var lactation = Number("lactation", required: true);
            var lastCalving = Date("date_velage", required: false);
            var lastInsemination = Date("date_ia", required: false);
            var status = Status();
            var scc = Number("ccs_milliers", required: false);

            return Errors.Count > 0 || id is null || name is null || bornOn is null || lactation is null || status is null
                ? null
                : new Cow(id, farmId, name, bornOn.Value, lactation.Value, lastCalving, lastInsemination, status.Value, scc);
        }

        private string? Required(string column)
        {
            var value = valueOf(column);
            if (value.Length > 0)
            {
                return value;
            }

            Fail(column, "valeur obligatoire");
            return null;
        }

        private DateOnly? Date(string column, bool required)
        {
            var value = required ? Required(column) : valueOf(column);
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (DateOnly.TryParseExact(value, DateFormats, FrenchCanada, DateTimeStyles.None, out var date))
            {
                return date;
            }

            Fail(column, "date invalide");
            return null;
        }

        private int? Number(string column, bool required)
        {
            var value = required ? Required(column) : valueOf(column);
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            // Entier positif ou nul uniquement : ni signe, ni décimale, ni séparateur de milliers.
            if (int.TryParse(value, NumberStyles.None, FrenchCanada, out var number))
            {
                return number;
            }

            Fail(column, "nombre invalide");
            return null;
        }

        private ReproStatus? Status()
        {
            var raw = valueOf("statut");
            switch (WithoutAccents(raw).ToLowerInvariant())
            {
                case "":
                case "vide":
                    return ReproStatus.Open;
                case "inseminee":
                    return ReproStatus.Bred;
                case "gestante":
                    return ReproStatus.Pregnant;
                case "tarie":
                    return ReproStatus.Dry;
                default:
                    Fail("statut", $"statut inconnu « {raw} »");
                    return null;
            }
        }

        private void Fail(string column, string message) => Errors.Add(new HerdImportError(lineNumber, column, message));

        private static string WithoutAccents(string value) =>
            string.Concat(value.Normalize(NormalizationForm.FormD)
                .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark));
    }
}
