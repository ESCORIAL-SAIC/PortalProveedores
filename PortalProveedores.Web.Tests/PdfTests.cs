using PortalProveedores.Web;

namespace PortalProveedores.Web.Tests;

public sealed class PdfTests : IDisposable
{
    private readonly string dir = Directory.CreateTempSubdirectory("pdf-tests-").FullName;

    public void Dispose() => Directory.Delete(dir, recursive: true);

    private void Crear(params string[] nombres)
    {
        foreach (var n in nombres) File.WriteAllText(Path.Combine(dir, n), "x");
    }

    // Número de documento y proveedor vienen de afuera y se concatenan con la raíz del share:
    // "../.." leería fuera de la carpeta del proveedor.
    [Theory]
    [InlineData("..")]
    [InlineData("../..")]
    [InlineData("a/b")]
    [InlineData(@"a\b")]
    [InlineData("a b")]
    [InlineData("")]
    [InlineData(null)]
    public void NombreValido_rechaza_separadores_y_vacios(string? nombre) =>
        Assert.False(Pdf.NombreValido(nombre));

    [Fact]
    public void NombreValido_rechaza_mas_de_64_caracteres() =>
        Assert.False(Pdf.NombreValido(new string('x', 65)));

    [Theory]
    [InlineData("OP-1234")]
    [InlineData("12345")]
    [InlineData("orden_99")]
    public void NombreValido_acepta_letras_digitos_guion_y_guion_bajo(string nombre) =>
        Assert.True(Pdf.NombreValido(nombre));

    [Fact]
    public void NombreValido_acepta_exactamente_64_caracteres() =>
        Assert.True(Pdf.NombreValido(new string('x', 64)));

    [Fact]
    public void Buscar_en_carpeta_inexistente_devuelve_vacio() =>
        Assert.Empty(Pdf.Buscar(Path.Combine(dir, "no-existe-" + Guid.NewGuid()), "1"));

    [Fact]
    public void Buscar_filtra_por_numero()
    {
        Crear("Pago-1234-a.pdf", "Pago-1234-b.pdf", "Pago-9999.pdf");

        Assert.Equal(2, Pdf.Buscar(dir, "1234").Length);
        Assert.Single(Pdf.Buscar(dir, "9999"));
        Assert.Empty(Pdf.Buscar(dir, "0000"));
    }

    // Un Contains pelado hacía que "OP-12" se llevara los PDF de "OP-123" en el mismo ZIP.
    [Fact]
    public void Buscar_no_arrastra_un_numero_mas_largo_que_lo_contiene()
    {
        Crear("Pago-OP-12.pdf", "Pago-OP-123.pdf");

        Assert.Equal(["Pago-OP-12.pdf"], Pdf.Buscar(dir, "OP-12").Select(Path.GetFileName));
        Assert.Equal(["Pago-OP-123.pdf"], Pdf.Buscar(dir, "OP-123").Select(Path.GetFileName));
    }

    [Fact]
    public void Buscar_acepta_el_numero_al_inicio_y_al_final_del_nombre()
    {
        Crear("7777-orden.pdf", "orden-8888");

        Assert.Single(Pdf.Buscar(dir, "7777"));
        Assert.Single(Pdf.Buscar(dir, "8888"));
    }

    [Fact]
    public void Buscar_ignora_mayusculas()
    {
        Crear("pago-abc1.pdf");

        Assert.Single(Pdf.Buscar(dir, "ABC1"));
    }

    [Fact]
    public void Buscar_con_numero_vacio_no_devuelve_nada()
    {
        Crear("pago-1.pdf");

        Assert.Empty(Pdf.Buscar(dir, ""));
    }
}
