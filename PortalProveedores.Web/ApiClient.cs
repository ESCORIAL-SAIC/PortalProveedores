using System.Net.Http.Json;
using System.Text.Json;

namespace PortalProveedores.Web;

// DTOs: nombres de propiedad copiados tal cual del proyecto WebForms para no tocar
// el contrato JSON de la API. Todo string, igual que antes.
public sealed record LoginResult(string? Proveedor);

public sealed class OrdenCompra
{
    public string? Id { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? Transaccion { get; set; }
    public string? NombreProveedor { get; set; }
    public string? FechaTransaccion { get; set; }
}

public sealed class ItemOrdenCompra
{
    public string? Id { get; set; }
    public string? CodigoProducto { get; set; }
    public string? DescripcionProducto { get; set; }
    public string? CantidadPendiente { get; set; }
    public string? FechaEstimada { get; set; }
}

public sealed class OrdenPago
{
    public string? NumeroPago { get; set; }
    public string? Fecha { get; set; }
    public string? Proveedor { get; set; }
    public string? Cuit { get; set; }
    public string? NombreDocumento { get; set; }
    public string? Factura { get; set; }
    public string? Importe { get; set; }
    public string? Total { get; set; }
    public string? Moneda { get; set; }
    public string? Cotizacion { get; set; }
}

public sealed class FacturaPendiente
{
    public string? FechaFactura { get; set; }
    public string? NumeroFactura { get; set; }
    public string? TipoTransaccion { get; set; }
    public string? Proveedor { get; set; }
    public string? Moneda { get; set; }
    public string? Total { get; set; }
    public string? Saldo { get; set; }
    public string? FechaVencimiento { get; set; }
    public string? SaldoConvertido { get; set; }
    public string? Cotizacion { get; set; }
    public string? FechaActualizacion { get; set; }
}

public sealed class ApiClient(HttpClient http, ILogger<ApiClient> log)
{
    // PropertyNamingPolicy en null a propósito: el código WebForms serializaba con
    // Newtonsoft, que manda PascalCase ("User"/"Password"). JsonSerializerDefaults.Web
    // camelCasearía los nombres y la API devuelve 400. Al leer se acepta cualquier caso.
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
    };

    public async Task<LoginResult?> Login(string user, string password)
    {
        try
        {
            var res = await http.PostAsJsonAsync("api/login",
                new { User = user, Password = password }, Json);
            if (!res.IsSuccessStatusCode)
                return null;
            return await res.Content.ReadFromJsonAsync<LoginResult>(Json);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            log.LogWarning(ex, "Login falló");
            return null;
        }
    }

    public Task<List<OrdenCompra>> OrdenesCompra(string proveedor, string? buscar = null) =>
        GetList<OrdenCompra>(string.IsNullOrWhiteSpace(buscar)
            ? $"api/ordenescompra/{Esc(proveedor)}"
            : $"api/ordenescompra/{Esc(proveedor)}?buscar={Esc(buscar)}");

    public Task<List<ItemOrdenCompra>> ItemsOrdenCompra(string idTransaccion) =>
        GetList<ItemOrdenCompra>(
            $"api/ordenescompra/{Esc(idTransaccion)}/items");

    public Task<List<OrdenPago>> OrdenesPago(string proveedor, string? buscar = null) =>
        GetList<OrdenPago>(string.IsNullOrWhiteSpace(buscar)
            ? $"api/ordenespago/{Esc(proveedor)}"
            : $"api/ordenespago/{Esc(proveedor)}?buscar={Esc(buscar)}");

    public Task<List<FacturaPendiente>> FacturasPendientes(string proveedor) =>
        GetList<FacturaPendiente>($"api/facturaspendientes/{Esc(proveedor)}");

    // Lista vacía en vez de null cuando falla: el código WebForms deserializaba
    // string.Empty y devolvía null, que después explotaba al hacer el bind.
    private async Task<List<T>> GetList<T>(string url)
    {
        try
        {
            var res = await http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                log.LogWarning("GET {Url} devolvió {Status}", url, (int)res.StatusCode);
                return [];
            }
            return await res.Content.ReadFromJsonAsync<List<T>>(Json) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            log.LogWarning(ex, "GET {Url} falló", url);
            return [];
        }
    }

    // El código viejo interpolaba directo: un valor de búsqueda con / o # rompía la ruta.
    private static string Esc(string s) => Uri.EscapeDataString(s);
}
