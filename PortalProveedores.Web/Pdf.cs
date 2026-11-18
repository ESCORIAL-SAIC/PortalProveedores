namespace PortalProveedores.Web;

public static class Pdf
{
    // Número de documento y proveedor vienen de afuera y se concatenan con la raíz
    // del share: sin esta validación, "../.." leería fuera de la carpeta del proveedor.
    public static bool NombreValido(string? s) =>
        !string.IsNullOrEmpty(s) && s.Length <= 64 &&
        s.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public static string[] Buscar(string carpeta, string numero) =>
        Directory.Exists(carpeta)
            ? Directory.GetFiles(carpeta)
                .Where(f => ContieneNumero(Path.GetFileName(f), numero))
                .ToArray()
            : [];

    // Un Contains pelado hacía que "OP-12" se llevara los PDF de "OP-123" en el mismo
    // ZIP. El número tiene que aparecer delimitado, no pegado a otro alfanumérico.
    private static bool ContieneNumero(string nombre, string numero)
    {
        if (string.IsNullOrEmpty(numero))
            return false;

        for (var i = nombre.IndexOf(numero, StringComparison.OrdinalIgnoreCase);
             i >= 0 && i < nombre.Length;
             i = nombre.IndexOf(numero, i + 1, StringComparison.OrdinalIgnoreCase))
        {
            var fin = i + numero.Length;
            var abreBien = i == 0 || !char.IsAsciiLetterOrDigit(nombre[i - 1]);
            var cierraBien = fin == nombre.Length || !char.IsAsciiLetterOrDigit(nombre[fin]);
            if (abreBien && cierraBien)
                return true;
        }
        return false;
    }
}
