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

    // Asserts en vez de un proyecto de tests: corre con `dotnet run -- --self-check`.
    public static void SelfCheck()
    {
        // Path traversal y caracteres de separación rechazados.
        foreach (var malo in new[] { "..", "../..", "a/b", @"a\b", "a b", "", null, new string('x', 65) })
            Assert(!NombreValido(malo), $"NombreValido debería rechazar: {malo ?? "(null)"}");

        foreach (var bueno in new[] { "OP-1234", "12345", "orden_99", new string('x', 64) })
            Assert(NombreValido(bueno), $"NombreValido debería aceptar: {bueno}");

        // Carpeta inexistente devuelve vacío, no excepción.
        Assert(Buscar(Path.Combine(Path.GetTempPath(), "no-existe-" + Guid.NewGuid()), "1").Length == 0,
            "Buscar en carpeta inexistente debería devolver vacío");

        // Filtrado por número sobre archivos reales.
        var dir = Directory.CreateTempSubdirectory("pdf-selfcheck-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(dir, "Pago-1234-a.pdf"), "x");
            File.WriteAllText(Path.Combine(dir, "Pago-1234-b.pdf"), "x");
            File.WriteAllText(Path.Combine(dir, "Pago-9999.pdf"), "x");
            Assert(Buscar(dir, "1234").Length == 2, "Buscar debería encontrar 2 archivos con 1234");
            Assert(Buscar(dir, "9999").Length == 1, "Buscar debería encontrar 1 archivo con 9999");
            Assert(Buscar(dir, "0000").Length == 0, "Buscar no debería encontrar 0000");

            // Un número no debe arrastrar los PDF de otro más largo que lo contiene.
            File.WriteAllText(Path.Combine(dir, "Pago-OP-12.pdf"), "x");
            File.WriteAllText(Path.Combine(dir, "Pago-OP-123.pdf"), "x");
            Assert(Buscar(dir, "OP-12").Length == 1, "Buscar OP-12 no debería traer OP-123");
            Assert(Buscar(dir, "OP-123").Length == 1, "Buscar OP-123 debería traer solo OP-123");

            // El número al principio y al final del nombre también cuenta.
            File.WriteAllText(Path.Combine(dir, "7777-orden.pdf"), "x");
            File.WriteAllText(Path.Combine(dir, "orden-8888"), "x");
            Assert(Buscar(dir, "7777").Length == 1, "Buscar debería aceptar el número al inicio");
            Assert(Buscar(dir, "8888").Length == 1, "Buscar debería aceptar el número al final");
        }
        finally { Directory.Delete(dir, recursive: true); }

        Console.WriteLine("self-check OK");
    }

    private static void Assert(bool cond, string mensaje)
    {
        if (!cond) throw new Exception("self-check FALLÓ: " + mensaje);
    }
}
