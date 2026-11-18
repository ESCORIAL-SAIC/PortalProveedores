using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace PortalProveedores.Web.Components.Pages;

// Reemplaza los tres Page_Init que leían el static Config.Proveedor. Ahora el proveedor
// sale del claim de la cookie, así que cada usuario ve lo suyo.
public abstract class PaginaProveedor : ComponentBase
{
    [CascadingParameter] private Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject] protected ApiClient Api { get; set; } = default!;

    // Filtros de fecha: los llena el usuario o un link del Panel (?desde=yyyy-MM-dd&hasta=...).
    [SupplyParameterFromQuery(Name = "desde")] private string? DesdeQuery { get; set; }
    [SupplyParameterFromQuery(Name = "hasta")] private string? HastaQuery { get; set; }
    private string? queryVista;
    protected DateOnly? desde, hasta;
    protected int pagina = 1;

    // El Panel hereda pero no filtra por fecha: sin esto recargaría (y redibujaría) al navegar a otra vista con query.
    protected virtual bool LeeFiltroDeUrl => true;

    protected string Proveedor { get; private set; } = string.Empty;
    protected bool Cargando { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        Proveedor = (await AuthState).User.FindFirst("Proveedor")?.Value ?? string.Empty;
        AplicarQuery();
        await Recargar();
    }

    // La instancia se reutiliza al navegar a la misma página con otro query (ej. desde el menú
    // lateral estando filtrado): sin esto el filtro viejo se quedaría.
    protected override async Task OnParametersSetAsync()
    {
        if (!LeeFiltroDeUrl || queryVista is null || $"{DesdeQuery}|{HastaQuery}" == queryVista) return;
        AplicarQuery();
        await Recargar();
    }

    private void AplicarQuery()
    {
        queryVista = $"{DesdeQuery}|{HastaQuery}";
        (desde, hasta, pagina) = (Resumen.FechaIso(DesdeQuery), Resumen.FechaIso(HastaQuery), 1);
    }

    protected abstract Task Cargar();

    protected async Task Recargar()
    {
        Cargando = true;
        try { await Cargar(); }
        finally { Cargando = false; }
    }
}
