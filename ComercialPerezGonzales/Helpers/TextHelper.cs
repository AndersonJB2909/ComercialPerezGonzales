using System.Globalization;
using System.Text;

namespace ComercialPerezGonzales.Helpers;

public static class TextHelper
{
    public static string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizado)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    public static bool ContieneSinAcento(string texto, string busqueda)
        => Normalizar(texto).Contains(Normalizar(busqueda));
}
