using System.Globalization;

namespace PortalProveedores.Web;

public sealed record AlertaFactura(string Numero, string Vence, int Dias, string Saldo);

// Cálculos del dashboard. La API devuelve todo como string ya formateado en es-AR
// (fecha dd/MM/yyyy, importe "$ 1.234,50"), así que acá se vuelve a parsear.
public static class Resumen
{
    private static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("es-AR");

    public static DateOnly? Fecha(string? s) =>
        DateOnly.TryParseExact(s, "dd/MM/yyyy", Ar, DateTimeStyles.None, out var d) ? d : null;

    // La API cuenta "hoy" en Buenos Aires; el servidor del portal puede estar en otra zona.
    // Linux usa el id IANA y Windows (IIS) el suyo propio.
    public static DateOnly Hoy()
    {
        foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
        {
            try { return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(id))); }
            catch (TimeZoneNotFoundException) { }
        }
        return DateOnly.FromDateTime(DateTime.Now);
    }

    private static string F(DateOnly? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    // Tramos de la dona: 0 vencidas, 1 vencen en 7 días, 2 más adelante. El 3 (sin vencimiento)
    // no se puede filtrar: la API deja afuera las filas sin fecha cuando hay desde/hasta.
    public static string? QueryVencimiento(int tramo, DateOnly hoy) => tramo switch
    {
        0 => $"hasta={F(hoy.AddDays(-1))}",
        1 => $"desde={F(hoy)}&hasta={F(hoy.AddDays(7))}",
        2 => $"desde={F(hoy.AddDays(8))}",
        _ => null,
    };

    // "2026-06" -> desde=2026-06-01&hasta=2026-06-30
    public static string? QueryMes(string? mes) =>
        DateOnly.TryParseExact(mes + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? $"desde={F(d)}&hasta={F(d.AddMonths(1).AddDays(-1))}" : null;

    public static DateOnly? FechaIso(string? s) =>
        DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    public static string Moneda(decimal d) => d.ToString("C2", Ar);

    // Negativo = vencida. null = factura sin fecha de vencimiento.
    public static int? Dias(string? vencimiento, DateOnly hoy) =>
        Fecha(vencimiento) is { } f ? f.DayNumber - hoy.DayNumber : null;

    public static List<AlertaFactura> Alertas(IEnumerable<FacturaPendiente> facturas, DateOnly hoy) =>
        facturas
            .Select(f => (f, d: Dias(f.FechaVencimiento, hoy)))
            .Where(x => x.d is <= 7)
            .OrderBy(x => x.d)
            .Select(x => new AlertaFactura(
                x.f.NumeroFactura ?? string.Empty, x.f.FechaVencimiento ?? string.Empty,
                x.d!.Value, x.f.SaldoConvertido ?? string.Empty))
            .ToList();

    public static (string Clase, string Texto) Estado(int? dias) => dias switch
    {
        null => ("", ""),
        < 0 => ("danger", $"Vencida hace {-dias} d"),
        0 => ("warn", "Vence hoy"),
        <= 7 => ("warn", $"Vence en {dias} d"),
        _ => ("ok", $"Vence en {dias} d"),
    };

    // "2026-06" -> "jun 26". Si no parsea, devuelve el texto tal cual.
    public static string Mes(string? s) =>
        DateOnly.TryParseExact(s + "-01", "yyyy-MM-dd", Ar, DateTimeStyles.None, out var d)
            ? d.ToString("MMM yy", Ar) : s ?? string.Empty;
}
