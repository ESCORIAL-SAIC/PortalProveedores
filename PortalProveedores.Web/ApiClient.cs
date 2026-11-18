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

public sealed record Pagina<T>(List<T> Items, int Total);

// Parámetros opcionales de los listados: solo viajan en la URL los que vienen.
public sealed record Consulta(
    string? Buscar = null, int? Pagina = null, int? Tamano = null,
    string? Desde = null, string? Hasta = null, string? Ordenar = null, string? Dir = null);

// camelCase en el JSON; ApiClient.Json lee sin distinguir mayúsculas.
public sealed record MesPagos(string Mes, long Cantidad);

public sealed record ResumenApi(
    long OcPendientes, long FacturasAbiertas, decimal SaldoTotal, long FacturasVencidas,
    long FacturasPorVencer7, long FacturasSinVencimiento, List<MesPagos> PagosUltimos6Meses);

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

    public Task<Pagina<OrdenCompra>> OrdenesCompra(string proveedor, Consulta? q = null) =>
        GetPagina<OrdenCompra>($"api/ordenescompra/{Esc(proveedor)}", q);

    public Task<List<ItemOrdenCompra>> ItemsOrdenCompra(string idTransaccion) =>
        GetList<ItemOrdenCompra>(
            $"api/ordenescompra/{Esc(idTransaccion)}/items");

    public Task<Pagina<OrdenPago>> OrdenesPago(string proveedor, Consulta? q = null) =>
        GetPagina<OrdenPago>($"api/ordenespago/{Esc(proveedor)}", q);

    public Task<Pagina<FacturaPendiente>> FacturasPendientes(string proveedor, Consulta? q = null) =>
        GetPagina<FacturaPendiente>($"api/facturaspendientes/{Esc(proveedor)}", q);

    // null si falla: el Panel cae a ceros en vez de romperse.
    public async Task<ResumenApi?> Resumen(string proveedor)
    {
        try
        {
            var res = await http.GetAsync($"api/resumen/{Esc(proveedor)}");
            if (!res.IsSuccessStatusCode)
            {
                log.LogWarning("GET resumen devolvió {Status}", (int)res.StatusCode);
                return null;
            }
            return await res.Content.ReadFromJsonAsync<ResumenApi>(Json);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            log.LogWarning(ex, "GET resumen falló");
            return null;
        }
    }

    private static string Url(string baseUrl, Consulta? q)
    {
        if (q is null) return baseUrl;
        var p = new List<string>();
        void Add(string k, object? v)
        {
            if (v is not null && !(v is string s && string.IsNullOrWhiteSpace(s)))
                p.Add($"{k}={Esc(Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)!)}");
        }
        Add("buscar", q.Buscar); Add("pagina", q.Pagina); Add("tamano", q.Tamano);
        Add("desde", q.Desde); Add("hasta", q.Hasta); Add("ordenar", q.Ordenar); Add("dir", q.Dir);
        return p.Count == 0 ? baseUrl : $"{baseUrl}?{string.Join('&', p)}";
    }

    private async Task<Pagina<T>> GetPagina<T>(string baseUrl, Consulta? q)
    {
        var url = Url(baseUrl, q);
        try
        {
            var res = await http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                log.LogWarning("GET {Url} devolvió {Status}", url, (int)res.StatusCode);
                return new([], 0);
            }
            var items = await res.Content.ReadFromJsonAsync<List<T>>(Json) ?? [];
            var total = res.Headers.TryGetValues("X-Total-Count", out var v)
                && int.TryParse(v.FirstOrDefault(), out var t) ? t : items.Count;
            return new(items, total);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            log.LogWarning(ex, "GET {Url} falló", url);
            return new([], 0);
        }
    }

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
