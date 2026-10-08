using System.Text;

namespace TourneeVeto.Domain.Import;

/// <summary>Décode le fichier importé : UTF-8 (avec ou sans BOM), sinon Windows-1252 (exports Excel en français).</summary>
public static class HerdCsvDecoder
{
    /// <summary>Taille maximale acceptée par l'import (2 Mo), contrôlée par l'interface avant toute lecture.</summary>
    public const int MaxFileBytes = 2 * 1024 * 1024;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    // Windows-1252 n'est pas intégré à .NET (Latin-1 ne connaît ni « œ », ni « € », ni « ’ ») : fournisseur de pages de codes.
    private static readonly Lazy<Encoding> Windows1252 = new(() =>
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252);
    });

    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> utf8Bom = [0xEF, 0xBB, 0xBF];
        if (bytes.StartsWith(utf8Bom))
        {
            return StrictUtf8.GetString(bytes[utf8Bom.Length..]);
        }

        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Windows1252.Value.GetString(bytes);
        }
    }
}
