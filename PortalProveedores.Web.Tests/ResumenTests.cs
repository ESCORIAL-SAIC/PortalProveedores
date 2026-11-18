using PortalProveedores.Web;

namespace PortalProveedores.Web.Tests;

public sealed class ResumenTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 16);

    private static FacturaPendiente Factura(string vencimiento) =>
        new() { NumeroFactura = vencimiento, FechaVencimiento = vencimiento };

    [Fact]
    public void Fecha_parsea_dd_MM_yyyy() =>
        Assert.Equal(new DateOnly(2026, 3, 3), Resumen.Fecha("03/03/2026"));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("2026-03-03")]
    public void Fecha_devuelve_null_si_no_es_dd_MM_yyyy(string? texto) =>
        Assert.Null(Resumen.Fecha(texto));

    [Fact]
    public void FechaIso_parsea_yyyy_MM_dd_y_rechaza_el_formato_de_la_api()
    {
        Assert.Equal(new DateOnly(2026, 11, 1), Resumen.FechaIso("2026-11-01"));
        Assert.Null(Resumen.FechaIso("01/11/2026"));
    }

    [Fact]
    public void Dias_es_negativo_si_esta_vencida_y_null_sin_vencimiento()
    {
        Assert.Equal(-6, Resumen.Dias("10/11/2026", Hoy));
        Assert.Equal(0, Resumen.Dias("16/11/2026", Hoy));
        Assert.Null(Resumen.Dias("", Hoy));
    }

    // Vencidas, vencen hoy y dentro de 7 días; quedan afuera las de más adelante y las sin fecha.
    [Fact]
    public void Alertas_incluye_vencidas_y_proximas_ordenadas_por_dias()
    {
        var facturas = new[]
        {
            Factura("10/11/2026"), Factura("16/11/2026"), Factura("20/11/2026"),
            Factura("30/11/2026"), Factura(""),
        };

        Assert.Equal([-6, 0, 4], Resumen.Alertas(facturas, Hoy).Select(a => a.Dias));
    }

    [Fact]
    public void Alertas_toma_el_limite_de_7_dias_inclusive() =>
        Assert.Single(Resumen.Alertas([Factura("23/11/2026"), Factura("24/11/2026")], Hoy));

    [Fact]
    public void Estado_describe_vencidas_proximas_y_sin_fecha()
    {
        Assert.Equal(("danger", "Vencida hace 6 d"), Resumen.Estado(-6));
        Assert.Equal(("warn", "Vence hoy"), Resumen.Estado(0));
        Assert.Equal(("warn", "Vence en 7 d"), Resumen.Estado(7));
        Assert.Equal(("ok", "Vence en 8 d"), Resumen.Estado(8));
        Assert.Equal(("", ""), Resumen.Estado(null));
    }

    [Fact]
    public void Mes_formatea_abreviado_y_devuelve_el_texto_si_no_parsea()
    {
        Assert.Equal("jun 26", Resumen.Mes("2026-06"));
        Assert.Equal("xx", Resumen.Mes("xx"));
        Assert.Equal("", Resumen.Mes(null));
    }

    [Fact]
    public void QueryVencimiento_arma_el_rango_de_cada_tramo()
    {
        Assert.Equal("hasta=2026-11-15", Resumen.QueryVencimiento(0, Hoy));
        Assert.Equal("desde=2026-11-16&hasta=2026-11-23", Resumen.QueryVencimiento(1, Hoy));
        Assert.Equal("desde=2026-11-24", Resumen.QueryVencimiento(2, Hoy));
    }

    // La API deja afuera las filas sin fecha cuando hay filtro, así que ese tramo no navega.
    [Fact]
    public void QueryVencimiento_devuelve_null_para_sin_vencimiento() =>
        Assert.Null(Resumen.QueryVencimiento(3, Hoy));

    [Theory]
    [InlineData("2026-02", "desde=2026-02-01&hasta=2026-02-28")]
    [InlineData("2024-02", "desde=2024-02-01&hasta=2024-02-29")]
    [InlineData("2026-12", "desde=2026-12-01&hasta=2026-12-31")]
    public void QueryMes_cubre_el_mes_completo_incluido_bisiesto(string mes, string esperado) =>
        Assert.Equal(esperado, Resumen.QueryMes(mes));

    [Fact]
    public void QueryMes_devuelve_null_si_el_mes_no_parsea() =>
        Assert.Null(Resumen.QueryMes("xx"));

    [Fact]
    public void Moneda_usa_formato_es_AR() =>
        Assert.Equal(1234.5m.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("es-AR")), Resumen.Moneda(1234.5m));
}
